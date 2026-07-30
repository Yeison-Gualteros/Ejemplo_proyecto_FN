using AutoMapper;
using Contracts;
using Entities.Exceptions;
using Entities.Models;
using Service.Contracts;
using Shared.DataTransferObjects;
using Microsoft.AspNetCore.Identity;

namespace Service
{
    public class Trazabilidad_DocumentoService : ITrazabilidad_DocumentoService
    {
        private readonly IRepositoryManager _repository;
        private readonly ILoggerManager _logger;
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        public Trazabilidad_DocumentoService(IRepositoryManager repository, ILoggerManager logger, IMapper mapper, UserManager<User> userManager)
        {
            _repository = repository;
            _logger = logger;
            _mapper = mapper;
            _userManager = userManager;

        }

        public async Task<IEnumerable<Trazabilidad_Documento>> GetTrazabilidadesPorDocumento(int documentoId)
        {
            // ✅ Usar el método del repositorio, NO FindByCondition directamente
            return await _repository.trazabilidad_Documento
                .GetTrazabilidadesPorDocumentoAsync(documentoId, false);
        }

        public async Task CreateTrazabilidadDocumento(
            int Id_Documento,
            string accion,
            string? comentario,
            string userId,
            string rol,
            string? rutaAnterior,
            string? rutaNueva,
            int? estadoAnterior,
            int? estadoNuevo,
            string? versionAnterior,
            string? versionNueva
        )
        {
            try
            {
                var usuario = await _repository.User.GetByIdAsync(userId);

                if (usuario == null)
                    throw new Exception("Usuario no encontrado");

                var trazabilidad = new Trazabilidad_Documento
                {
                    Id_Documento = Id_Documento,
                    Accion = accion,
                    Comentario = comentario,
                    Id_Usuario = userId,
                    Rol = rol,

                    RutaAnterior = rutaAnterior,
                    RutaNueva = rutaNueva,

                    EstadoAnterior = estadoAnterior,   
                    EstadoNuevo = estadoNuevo,         
                    VersionAnterior = versionAnterior,
                    VersionNueva = versionNueva,      

                    Fecha_Cambio = DateTime.Now
                };

                _repository.trazabilidad_Documento.CreateTrazabilidadDocumento(trazabilidad);

                await _repository.SaveAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("ERROR TRAZABILIDAD → " + (ex.InnerException?.Message ?? ex.Message));
            }
        }

        public async Task<IEnumerable<Trazabilidad_DocumentoDTO>> GetAllTrazabilidad_DocumentoAsync(int idDocumento, string userId, bool trackChanges)
        {
            var documento = await _repository.Documento.GetDocumento(idDocumento, userId, trackChanges);

            if (documento is null)
                throw new DocumentoNotFoundException(idDocumento);

            var trazabilidadesDb = await _repository.trazabilidad_Documento.GetAllByDocumentoAsync(idDocumento, userId, trackChanges);

            return trazabilidadesDb.Select(t => new Trazabilidad_DocumentoDTO
            {
                Id_Documento = t.Id_Documento,
                Accion = t.Accion,
                Comentario = t.Comentario,
                Id_Usuario = t.Id_Usuario,
                Fecha_Cambio = t.Fecha_Cambio,

                NombreUsuario = t.User.Nombre,
                ApellidoUsuario = t.User.Apellido,
                Rol = t.Rol,

                RutaAnterior = t.RutaAnterior,
                RutaNueva = t.RutaNueva,

                EstadoAnterior = t.EstadoAnterior,
                EstadoNuevo = t.EstadoNuevo,

                VersionAnterior = t.VersionAnterior,
                VersionNueva = t.VersionNueva
            });
        }
    }
}
