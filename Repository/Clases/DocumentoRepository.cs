using Contracts.Interface;
using Entities.Enums;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Repository.Extensions;
using Shared.DataTransferObjects;
using Shared.DataTransferObjects.Documentos;
using Shared.RequestFeatures;
using System.Text.Json;

namespace Repository.Clases
{
    public class DocumentoRepository : RepositoryBase<Documento>, IDocumentoRepository
    {
        public DocumentoRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
        public async Task<PagedList<Documento>> GetAllDocumentos(DocumentoParameters documentoParameters, bool trackChanges)
        {
            var query = FindAll(trackChanges)
               .Include(d => d.TipoDocumento)
               .Include(d => d.Versiones)
               .Include(d => d.Aprobador)
               .Where(d => d.Versiones.Any(v =>
                   v.EsActual &&
                   v.Estado != (int)EstadoDocumento.Eliminado
               ));

            if (documentoParameters.MinFecha.HasValue)
            {
                query = query.Where(d => d.Fecha_Creacion >= documentoParameters.MinFecha.Value);
            }

            if (documentoParameters.MaxFecha.HasValue)
            {
                query = query.Where(d => d.Fecha_Creacion <= documentoParameters.MaxFecha.Value);
            }

            query = query.Search(documentoParameters.Busqueda);

            query = query.Sort(documentoParameters.Orden, documentoParameters.Direccion);

            var documentos = await query
                .Include(d => d.User)
                .Include(d => d.Aprobador)
                .Include(d => d.Versiones)
                .ToListAsync();

            foreach (var doc in documentos)
            {
                doc.VersionActual = doc.Versiones
                    .Where(v => v.EsActual)
                    .OrderByDescending(v => v.Fecha_Creacion)
                    .FirstOrDefault();
            }

            return PagedList<Documento>
                .ToPageList(documentos, documentoParameters.PageNumber, documentoParameters.PageSize);
        }

        public async Task<IEnumerable<DocumentoVersion>> GetByDocumentoId(int documentoId)
        {
            return await RepositoryContext.DocumentoVersion
                .Include(v => v.Documento)
                .Where(v => v.Id_Documento == documentoId)
                .OrderBy(v => v.Fecha_Creacion)
                .ToListAsync();
        }

        public async Task<Documento> GetDocumento(int Id_Documento, string userId, bool trackChanges)
        {
            var doc = await FindAll(trackChanges)
                .Include(d => d.TipoDocumento)
                .Include(d => d.Proceso)
                .Include(d => d.Versiones)
                .Include(d => d.User)
                .Include(d => d.Aprobador)
                .Where(d => d.Id_Documento == Id_Documento &&
                    d.Versiones.Any(v =>
                        v.EsActual &&
                        v.Estado != (int)EstadoDocumento.Eliminado))
                .SingleOrDefaultAsync();

            

            if (doc != null)
            {
                doc.VersionActual = doc.Versiones
                    .Where(v => v.EsActual)
                    .OrderByDescending(v => v.Fecha_Creacion)
                    .FirstOrDefault();
            }

            return doc;
        }

        public async Task<IEnumerable<Documento>> GetDocumentoByIds(IEnumerable<int> Id_Documentos, bool trackChanges)
        {
            var docs = await FindAll(trackChanges)
                .Include(d => d.Versiones)
                .Where(d => Id_Documentos.Contains(d.Id_Documento))
                .ToListAsync();

            foreach (var doc in docs)
            {
                doc.VersionActual = doc.Versiones
                    .Where(v => v.EsActual)
                    .OrderByDescending(v => v.Fecha_Creacion)
                    .FirstOrDefault();
                }

            return docs;
        }

        public async Task<PagedList<DocumentoDTO>> GetDocumentosFiltrados(DocumentoParameters parameters, string userId, bool verTodos, bool puedeAprobar, 
            bool puedeArchivar, 
            List<int> nivelesAcceso,
            bool trackChanges)
        {
            var query = FindAll(trackChanges)
                .AsNoTracking()
                .Include(d => d.User)        
                .Include(d => d.Aprobador)   
                .Include(d => d.Versiones)
                .AsQueryable();

            query = query.Where(d => !d.Versiones.Any(v =>
                v.EsActual && v.Estado == (int)EstadoDocumento.Eliminado));

            var usuario = await RepositoryContext.Users
                .Include(u => u.Proceso)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var procesoUsuario = await RepositoryContext.Users
                .Where(u => u.Id == userId)
                .Select(u => u.IdProceso)
                .FirstOrDefaultAsync();

            // FILTROS
            if (!verTodos)
            {
                if (puedeAprobar)
                {
                    // APROBADOR: Ve documentos de su proceso donde es aprobador
                    query = query.Where(d =>
                        d.AprobadorId == userId &&
                        d.IdProceso == procesoUsuario &&
                        d.Versiones.Any(v => v.EsActual &&
                            (v.Estado == (int)EstadoDocumento.EnRevision ||
                             v.Estado == (int)EstadoDocumento.Rechazado))
                    );
                }
                else if (puedeArchivar)
                {
                    // ARCHIVADOR: Ve documentos de su proceso según nivel de acceso
                    query = query.Where(d =>
                        d.IdProceso == procesoUsuario &&
                        nivelesAcceso.Contains((int)d.NivelAcceso)
                    );
                }
                else
                {
                    //ve todos los documentos de su proceso:
                    query = query.Where(d =>
                        d.IdProceso == procesoUsuario  // Solo filtrar por proceso, no por creador
                    );
                }
            }

            if (!string.IsNullOrWhiteSpace(parameters.Busqueda))
            {
                var busqueda = parameters.Busqueda.Trim();

                query = query.Where(d =>
                    d.Nombre.Contains(busqueda) ||
                    d.Descripcion.Contains(busqueda)
                );
            }

            if (parameters.MinFecha.HasValue)
            {
                query = query.Where(d =>
                    d.Fecha_Creacion >= parameters.MinFecha.Value
                );
            }

            if (parameters.MaxFecha.HasValue)
            {
                query = query.Where(d =>
                    d.Fecha_Creacion <= parameters.MaxFecha.Value
                );
            }

            query = query.OrderByDescending(d => d.Fecha_Creacion);
 
            // PROYECCIÓN 
            var queryDto = query.Select(d => new DocumentoDTO
            {
                Id_Documento = d.Id_Documento,
                Nombre = d.Nombre,
                Descripcion = d.Descripcion,
                UsuarioSubio = d.User.Nombre,
                ApellidoUsuario = d.User.Apellido,
                AprobadorNombre = d.Aprobador.Nombre,
                AprobadorApellido = d.Aprobador.Apellido,
                Tipo = d.IdTipoDocumento,
                Fecha_Creacion = d.Fecha_Creacion,
                Estado = d.Versiones
                    .Where(v => v.EsActual)
                    .Select(v => v.Estado)
                    .FirstOrDefault(),
                Etiquetado = d.Etiquetado,
                VersionActual = d.Versiones
                    .Where(v => v.EsActual)
                    .OrderByDescending(v => v.Fecha_Creacion)
                    .Select(v => new DocumentoVersionDTO
                    {
                        Id_Version = v.Id_Version,
                        Ruta = v.RutaPdf,
                    })
                    .FirstOrDefault(),

                ContenidoJson = d.ContenidoJson
            });

            return await PagedList<DocumentoDTO>.ToPageListAsync(queryDto, parameters.PageNumber, parameters.PageSize);
        }

        public async Task<Documento> GetByIdAsync(int id, bool trackChanges)
        {
            var doc = await FindByCondition(d => d.Id_Documento == id, trackChanges)
                .Include(d => d.TipoDocumento)
                .Include(d => d.Proceso)
                .Include(d => d.Versiones)
                .Include(d => d.User)
                .Include(d => d.Aprobador)
                .FirstOrDefaultAsync();

            if (doc != null)
            {
                doc.VersionActual = doc.Versiones
                    .Where(v => v.EsActual)
                    .OrderByDescending(v => v.Fecha_Creacion)
                    .FirstOrDefault();
            }

            return doc;
        }

        public async Task<DocumentoVersion> GetVersionById(int documentoId, int versionId)
        {
            return await RepositoryContext.DocumentoVersion
                .FirstOrDefaultAsync(v =>
                    v.Id_Documento == documentoId &&
                    v.Id_Version == versionId
                );
        }

        public void CreateDocumento(Documento documento)=>
            Create(documento);

        public void DeleteDocumento(Documento documento) =>
            Delete(documento);

        public async Task<int> GetConsecutivo()
        {
            return await FindAll(false)
                .MaxAsync(d => (int?)d.ConsecutivoNumero) ?? 0;
        }
        public async Task<PagedList<ListadoDocumentoDto>> GetListadoMaestro(DocumentoParameters parameters, List<int> nivelesAcceso, bool esConsultor,
            string userId, bool esGestorDocumental, bool trackChanges)
        {
            var query = RepositoryContext.Documentos
                .AsNoTracking()
                .Include(d => d.TipoDocumento)
                .Include(d => d.User)
                .Include(d => d.Aprobador)
                .Include(d => d.Proceso)
                .Include(d => d.Versiones)
                .Where(d => d.Versiones.Any(v => v.EsActual))
                .AsQueryable();

            
            query = query.Where(d => d.Versiones.Any(v => v.EsActual && v.Estado == (int)EstadoDocumento.Vigente));

            // FILTRO POR ESTADO ELIMINADO (excluir eliminados)
            query = query.Where(d => !d.Versiones.Any(v => v.EsActual && v.Estado == (int)EstadoDocumento.Eliminado));

            // FILTRO POR NIVELES DE ACCESO
            if (!esGestorDocumental)
            {
                if (nivelesAcceso != null && nivelesAcceso.Any())
                {
                    query = query.Where(d => nivelesAcceso.Contains((int)d.NivelAcceso));
                }
                else
                {
                    query = query.Where(d => d.NivelAcceso == NivelAccesoDocumento.Publico);
                }
            }

            // FILTRO POR BÚSQUEDA
            if (!string.IsNullOrWhiteSpace(parameters.Busqueda))
            {
                var busqueda = parameters.Busqueda.Trim();
                query = query.Where(d =>
                    d.Nombre.Contains(busqueda) ||
                    d.Descripcion.Contains(busqueda) ||
                    d.Consecutivo.Contains(busqueda) ||
                    (d.TipoDocumento != null && d.TipoDocumento.Nombre.Contains(busqueda)) ||
                    (d.Proceso != null && d.Proceso.Nombre.Contains(busqueda))
                );
            }

            // FILTRO POR TIPO DE DOCUMENTO
            if (!string.IsNullOrWhiteSpace(parameters.TipoDocumento))
            {
                query = query.Where(d => d.TipoDocumento != null && d.TipoDocumento.Nombre == parameters.TipoDocumento);
            }

            // FILTRO POR PROCESO 
            if (!string.IsNullOrWhiteSpace(parameters.Proceso))
            {
                query = query.Where(d => d.Proceso != null && d.Proceso.Nombre == parameters.Proceso);
            }

            // FILTRO POR ETIQUETADO
            if (!string.IsNullOrWhiteSpace(parameters.Etiquetado))
            {
                query = query.Where(d => d.Etiquetado == parameters.Etiquetado);
            }

            // FILTRO POR RANGO DE FECHAS
            if (parameters.MinFecha.HasValue)
                query = query.Where(d => d.Fecha_Creacion >= parameters.MinFecha.Value);

            if (parameters.MaxFecha.HasValue)
                query = query.Where(d => d.Fecha_Creacion <= parameters.MaxFecha.Value);

            // ORDENAMIENTO
            query = parameters.Orden?.ToLower() switch
            {
                "nombre" => parameters.Direccion == "asc"
                    ? query.OrderBy(d => d.Nombre)
                    : query.OrderByDescending(d => d.Nombre),
                "fechacreacion" => parameters.Direccion == "asc"
                    ? query.OrderBy(d => d.Fecha_Creacion)
                    : query.OrderByDescending(d => d.Fecha_Creacion),
                _ => query.OrderByDescending(d => d.Fecha_Creacion)
            };

            var procesoUsuario = await RepositoryContext.Users
                .Where(u => u.Id == userId)
                .Select(u => u.IdProceso)
                .FirstOrDefaultAsync();

            // PROYECCIÓN 
            var resultado = query.Select(d => new ListadoDocumentoDto
            {
                Id = d.Id_Documento,
                Consecutivo = d.Consecutivo,
                Nombre = d.Nombre,
                Descripcion = d.Descripcion,
                TipoDocumento = d.TipoDocumento != null ? d.TipoDocumento.Nombre : "",
                Proceso = d.Proceso != null ? d.Proceso.Nombre : "",
                FechaCreacion = d.Fecha_Creacion,
                FechaModificacion = d.Fecha_Modificacion,
                FechaRevision = d.Fecha_Revision,
                RutaArchivo = d.Versiones
                    .Where(v => v.EsActual)
                    .Select(v => v.RutaPdf)
                    .FirstOrDefault(),
                VersionActual = d.Versiones
                    .Where(v => v.EsActual)
                    .Select(v => new VersionActualDto
                    {
                        Id_Version = v.Id_Version,
                        RutaPdf = v.RutaPdf,
                        NumeroVersion = v.NumeroVersion
                    })
                    .FirstOrDefault(),
                Usuario = d.User != null ? d.User.Nombre : "",
                UsuarioSubio = d.User != null ? d.User.Nombre : "",
                ApellidoUsuario = d.User != null ? d.User.Apellido : "",
                AprobadorNombre = d.Aprobador != null ? d.Aprobador.Nombre : "",
                AprobadorApellido = d.Aprobador != null ? d.Aprobador.Apellido : "",
                AprobadorId = d.AprobadorId,
                Etiquetado = d.Etiquetado,
                Estado = d.Versiones
                    .Where(v => v.EsActual)
                    .Select(v => v.Estado)
                    .FirstOrDefault(),
                NivelAcceso = (int)d.NivelAcceso,
                IdTipoDocumento = d.IdTipoDocumento,
                IdProceso = d.IdProceso,
                FechaAprobacion = d.Fecha_Aprobacion,
                ContenidoJson = d.ContenidoJson,

                IdCreador = d.Id_Usuario,  // ID del creador del documento
                EsCreador = d.Id_Usuario == userId,  // ¿El usuario actual es el creador?
                PerteneceAlProceso = d.IdProceso == procesoUsuario  // ¿El usuario pertenece al mismo proceso?
            });

            return await PagedList<ListadoDocumentoDto>
                .ToPageListAsync(resultado, parameters.PageNumber, parameters.PageSize);
        }

        public async Task<Documento?> GetUltimoConsecutivoPorPrefijo(string idTipoDocumento, string idProceso)
        {
            return await FindByCondition(d =>
                d.IdTipoDocumento == idTipoDocumento &&
                d.IdProceso == idProceso,
                false)
                .OrderByDescending(d => d.ConsecutivoNumero)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Documento>> GetActivosConConsecutivoMayor(string idTipoDocumento, string idProceso, int consecutivoMinimo, bool trackChanges)
        {
            return await FindByCondition(d =>
                d.IdTipoDocumento == idTipoDocumento &&
                d.IdProceso == idProceso &&
                d.ConsecutivoNumero > consecutivoMinimo && // Solo mayores
                d.Versiones.Any(v => v.EsActual && v.Estado != (int)EstadoDocumento.Eliminado),
                trackChanges)
                .OrderBy(d => d.ConsecutivoNumero) // Ya están ordenados
                .ToListAsync();
        }

        public async Task<List<Documento>> GetDocumentosNuncaRevisadosAsync(DateTime fechaLimite, bool trackChanges)
        {
            return await FindByCondition(d =>
                // Tiene versión actual VIGENTE
                d.Versiones.Any(v => v.EsActual && v.Estado == (int)EstadoDocumento.Vigente) &&
                // NUNCA ha sido revisado
                d.Fecha_Revision == null &&
                // Creado hace más de 2 años (fechaLimite = hoy - 2 años)
                d.Fecha_Creacion <= fechaLimite,
                trackChanges)
                .Include(d => d.Proceso)
                .Include(d => d.TipoDocumento)
                .Include(d => d.Versiones)
                .Include(d => d.User)
                .OrderBy(d => d.Fecha_Creacion)
                .ToListAsync();
        }

        // Documentos VIGENTES última revisión fue hace más de 2 años
        public async Task<List<Documento>> GetDocumentosRevisionVencidaAsync(DateTime fechaLimite, bool trackChanges)
        {
            return await FindByCondition(d =>
                // Tiene versión actual VIGENTE
                d.Versiones.Any(v => v.EsActual && v.Estado == (int)EstadoDocumento.Vigente) &&
                // Fue revisado pero hace más de 2 años
                d.Fecha_Revision != null &&
                d.Fecha_Revision <= fechaLimite,
                trackChanges)
                .Include(d => d.Proceso)
                .Include(d => d.TipoDocumento)
                .Include(d => d.Versiones)
                .Include(d => d.User)
                .OrderBy(d => d.Fecha_Revision)
                .ToListAsync();
        }

        // Documentos APROBADOS (no vigentes) que también necesitan revisión
        public async Task<List<Documento>> GetDocumentosAprobadosSinRevisionAsync(DateTime fechaLimite, bool trackChanges)
        {
            return await FindByCondition(d =>
                // Tiene versión actual APROBADA
                d.Versiones.Any(v => v.EsActual && v.Estado == (int)EstadoDocumento.Aprobado) &&
                // Nunca revisado O revisión vencida
                (d.Fecha_Revision == null || d.Fecha_Revision <= fechaLimite),
                trackChanges)
                .Include(d => d.Proceso)
                .Include(d => d.TipoDocumento)
                .Include(d => d.Versiones)
                .Include(d => d.User)
                .OrderBy(d => d.Fecha_Aprobacion)
                .ToListAsync();
        }
    }
}
