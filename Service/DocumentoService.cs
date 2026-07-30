using AutoMapper;
using Contracts;
using Entities.Enums;
using Entities.Exceptions;
using Entities.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Service.Ayudante;
using Service.Contracts;
using Shared.DataTransferObjects.Documentos;
using Shared.RequestFeatures;
using System.Diagnostics;
using System.Text.Json;
using Service.PlantillasEmail;

namespace Service
{
    public class DocumentoService : IDocumentoService
    {
        private readonly IRepositoryManager _repository;
        private readonly ILoggerManager _logger;
        private readonly IMapper _mapper;
        private readonly IPermisosService _permisosService;
        private readonly ITrazabilidad_DocumentoService _trazabilidad;
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;

        public DocumentoService(IRepositoryManager repository, ILoggerManager logger, IMapper mapper,
            IPermisosService permisosService, ITrazabilidad_DocumentoService trazabilidad, UserManager<User> userManager, IEmailService emailService)
        {
            _repository = repository;
            _logger = logger;
            _mapper = mapper;
            _permisosService = permisosService;
            _trazabilidad = trazabilidad;
            _userManager = userManager;
            _emailService = emailService;
        }

        public async Task<(IEnumerable<DocumentoDTO> documentos, MetaData metaData)> GetAllDocumentos(
            DocumentoParameters documentoParameters, string userId, bool trackChanges)
        {
            try
            {
                if (!documentoParameters.ValidFechaRango)
                    throw new MaxFechaRangoBadRequestException();

                var permisos = await _permisosService.ObtenerPermisosUsuario(userId);
                var puedeVerTodos = permisos.Contains("DOCUMENTOS_VER_TODOS");
                var puedeAprobar = permisos.Contains("DOCUMENTOS_APROBAR");
                var puedeArchivar = permisos.Contains("DOCUMENTOS_ARCHIVAR");
                var puedeVerInternos = permisos.Contains("DOCUMENTOS_VER_INTERNOS");
                var puedeVerRestrigidos = permisos.Contains("DOCUMENTOS_VER_RESTRINGIDOS");
                var puedeVerCofidenciales = permisos.Contains("DOCUMENTOS_VER_CONFIDENCIALES");

                _logger.LogInfo($"🔍||||||||| Usuario: {userId}");
                _logger.LogInfo($"🔍|||||||||| Permisos: {string.Join(", ", permisos)}");
                _logger.LogInfo($"🔍||||||| verTodos: {puedeVerTodos}");
                _logger.LogInfo($"🔍||||||||| puedeAprobar: {puedeAprobar}");
                _logger.LogInfo($"🔍|||||||| puedeArchivar: {puedeArchivar}");

                var nivelAcceso = new List<int> { (int)NivelAccesoDocumento.Publico };
                if (puedeVerInternos) nivelAcceso.Add((int)NivelAccesoDocumento.UsoInterno);
                if (puedeVerRestrigidos) nivelAcceso.Add((int)NivelAccesoDocumento.Restringido);
                if (puedeVerCofidenciales) nivelAcceso.Add((int)NivelAccesoDocumento.Confidencial);
                if (puedeVerTodos)
                    nivelAcceso = Enum.GetValues(typeof(NivelAccesoDocumento)).Cast<int>().ToList();

                var documentosWithMetaData = await _repository.Documento.GetDocumentosFiltrados(
                    documentoParameters, userId, puedeVerTodos, puedeAprobar, puedeArchivar, nivelAcceso, trackChanges);

                var documentosDto = documentosWithMetaData.Select(doc =>
                {
                    doc.EsAprobador = doc.AprobadorId == userId;
                    return doc;
                });

                return (documentos: documentosDto, metaData: documentosWithMetaData.MetaData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR EN GetAllDocumentos: {ex}");
                throw;
            }
        }

        public async Task<DocumentoDTO> GetDocumento(int Id_Documento, string userId, bool trackChanges)
        {
            var doc = await _repository.Documento.GetDocumento(Id_Documento, userId, trackChanges);
            if (doc is null) throw new DocumentoNotFoundException(Id_Documento);

            var permisos = await _permisosService.ObtenerPermisosUsuario(userId);
            var puedeVerTodos = permisos.Contains("DOCUMENTOS_VER_LISTADO");
            var puedeVerInternos = permisos.Contains("DOCUMENTOS_VER_INTERNOS");
            var puedeVerRestrigidos = permisos.Contains("DOCUMENTOS_VER_RESTRINGIDOS");
            var puedeVerCofidenciales = permisos.Contains("DOCUMENTOS_VER_CONFIDENCIALES");

            _logger.LogInfo($"🔍||||||||| Usuario: {userId}");
            _logger.LogInfo($"🔍|||||||||| Permisos: {string.Join(", ", permisos)}");
            _logger.LogInfo($"🔍||||||| verTodos: {puedeVerTodos}");
            

            var nivelesAcceso = new List<int> { (int)NivelAccesoDocumento.Publico };
            if (puedeVerInternos) nivelesAcceso.Add((int)NivelAccesoDocumento.UsoInterno);
            if (puedeVerRestrigidos) nivelesAcceso.Add((int)NivelAccesoDocumento.Restringido);
            if (puedeVerCofidenciales) nivelesAcceso.Add((int)NivelAccesoDocumento.Confidencial);
            if (puedeVerTodos) nivelesAcceso = Enum.GetValues(typeof(NivelAccesoDocumento)).Cast<int>().ToList();

            var esCreador = doc.Id_Usuario == userId;
            var esAprobador = doc.AprobadorId == userId;
            var enRevision = doc.VersionActual?.Estado == (int)EstadoDocumento.EnRevision;

            if (!nivelesAcceso.Contains((int)doc.NivelAcceso) && !esCreador && !(esAprobador && enRevision))
                throw new UnauthorizedAccessException("No tienes acceso a este documento");

            var dto = _mapper.Map<DocumentoDTO>(doc);
            dto.EsAprobador = doc.AprobadorId == userId;
            return dto;
        }

        public async Task<IEnumerable<DocumentoDTO>> GetDocumentoByIds(IEnumerable<int> idDocumento, bool trackChanges)
        {
            if (idDocumento is null) throw new IdParametersBadRequestException();
            var documentoEntities = await _repository.Documento.GetDocumentoByIds(idDocumento, trackChanges);
            if (idDocumento.Count() != documentoEntities.Count()) throw new CollectionByIdsBadRequestException();
            return _mapper.Map<IEnumerable<DocumentoDTO>>(documentoEntities);
        }

        // UTILIDADES DE ARCHIVOS
        public async Task<string> GuardarArchivoAsync(IFormFile? archivo)
        {
            if (archivo == null || archivo.Length == 0)
                throw new Exception("El archivo es inválido o no fue enviado");

            var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            if (!Directory.Exists(root)) Directory.CreateDirectory(root);

            var folderPath = Path.Combine(root, "archivos");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(archivo.FileName)}";
            var filePath = Path.Combine(folderPath, fileName);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                    await archivo.CopyToAsync(stream);
            }
            catch (Exception ex)
            {
                throw new Exception("Error guardando archivo: " + ex.Message);
            }
            return "/archivos/" + fileName;
        }

        private async Task<string> GuardarArchivoBytesAsync(byte[] archivoBytes, string extension)
        {
            var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var carpetaPath = Path.Combine(root, "archivos");
            if (!Directory.Exists(carpetaPath)) Directory.CreateDirectory(carpetaPath);

            var archivoNombre = $"{Guid.NewGuid()}{extension}";
            var archivoPath = Path.Combine(carpetaPath, archivoNombre);
            await File.WriteAllBytesAsync(archivoPath, archivoBytes);
            return "/archivos/" + archivoNombre;
        }

        // CREAR DOCUMENTO 
        public async Task<DocumentoDTO> CreateDocumentoAsync(DocumentoForCreationDto dto, IFormFile? archivo)
        {
            // Validaciones
            var usuario = await _userManager.FindByIdAsync(dto.Id_Usuario);
            if (usuario == null) throw new Exception("Usuario no encontrado");

            if (usuario.IdProceso != dto.IdProceso)
                throw new UnauthorizedAccessException("No puedes crear documentos en un proceso al que no perteneces");

            if (string.IsNullOrEmpty(dto.Id_Usuario)) throw new Exception("Usuario inválido");
            if (string.IsNullOrEmpty(dto.AprobadorId)) throw new Exception("Aprobador requerido");

            var tipoDocumento = await _repository.TipoDocumento.GetByIdAsync(dto.IdTipoDocumento, false);
            if (tipoDocumento == null) throw new Exception("Tipo de documento no encontrado");

            var proceso = await _repository.Proceso.GetByIdAsync(dto.IdProceso, false);
            if (proceso == null) throw new Exception("Proceso no encontrado");

            //CREAR DOCUMENTO
            var documento = _mapper.Map<Documento>(dto);
            documento.Fecha_Creacion = DateTime.UtcNow;
            documento.Fecha_Modificacion = DateTime.UtcNow;
            documento.IdTipoDocumento = dto.IdTipoDocumento;
            documento.IdProceso = dto.IdProceso;
            documento.AprobadorId = dto.AprobadorId;
            documento.Id_Usuario = dto.Id_Usuario;

            if (!Enum.IsDefined(typeof(NivelAccesoDocumento), dto.NivelAcceso) || dto.NivelAcceso == 0)
                documento.NivelAcceso = NivelAccesoDocumento.Publico;
            else
                documento.NivelAcceso = (NivelAccesoDocumento)dto.NivelAcceso;

            
            // Dentro de CreateDocumentoAsync, después de mapear el documento:
            documento.ContenidoJson = await ProcesarImagenesEnContenido(dto.ContenidoData);

            documento.Consecutivo = await GenerarConsecutivo(documento);
            _repository.Documento.CreateDocumento(documento);
            await _repository.SaveAsync();

            //GENERAR PDF
            string rutaWord, rutaPdf;
            float tamañoKB;

            if (archivo != null && archivo.Length > 0)
            {
                rutaWord = await GuardarArchivoAsync(archivo);
                var ext = Path.GetExtension(archivo.FileName).ToLower();
                if (ext == ".pdf")
                {
                    rutaPdf = rutaWord;
                    tamañoKB = archivo.Length / 1024f;
                }
                else
                {
                    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    Directory.CreateDirectory(tempDir);
                    var tempFile = Path.Combine(tempDir, $"input{ext}");
                    try
                    {
                        using (var fs = new FileStream(tempFile, FileMode.Create))
                            await archivo.CopyToAsync(fs);
                        var pdfBytes = WordTemplateHelper.ConvertirDocxAPdf(tempFile, tempDir);
                        rutaPdf = await GuardarArchivoBytesAsync(pdfBytes, ".pdf");
                        tamañoKB = pdfBytes.Length / 1024f;
                    }
                    finally { try { Directory.Delete(tempDir, true); } catch { } }
                }
            }
            else
            {
                // Generar desde plantilla con contenido flexible
                var creador = await _userManager.FindByIdAsync(dto.Id_Usuario);

                var metadatos = new Dictionary<string, string>
                {
                    { "{Nombre}", documento.Nombre ?? "" },
                    { "{Consecutivo}", documento.Consecutivo ?? "" },
                    { "{Version}", "1.0" },
                    { "{Fecha_Creacion}", DateTime.UtcNow.ToString("dd/MM/yyyy") },
                    { "{Fecha_Modificacion}", DateTime.UtcNow.ToString("dd/MM/yyyy") },
                    { "{Proceso}", proceso?.Nombre ?? "" },
                    { "{Descripcion}", documento.Descripcion ?? "" },
                    { "{Etiquetado}", documento.Etiquetado ?? "" },
                    { "{Creador}", $"{creador?.Nombre ?? ""} {creador?.Apellido ?? ""}".Trim() },
                    { "{Aprobador}", "" },
                    { "{Fecha_Aprobacion}", "" }
                };

                var controlCambios = new List<ControlCambioDto>
                {
                    new()
                    {
                        Version = "1.0",
                        Fecha = DateTime.UtcNow.ToString("dd/MM/yyyy"),
                        Usuario = $"{creador?.Nombre ?? ""} {creador?.Apellido ?? ""}".Trim(),
                        Descripcion = "Versión creada"
                    }
                };

                var contenidoJsonParaPdf = await PrepararImagenesParaPdf(documento.ContenidoJson);
                var contenido = DeserializarContenido(contenidoJsonParaPdf) ?? new ContenidoDocumentoDto();

                var firmas = new List<FirmaAprobadorDto>();
                var plantillaPath = SeleccionarPlantilla(tipoDocumento?.Nombre ?? "");

                var pdfBytes = WordTemplateHelper.GenerarDocumentoFlexible(
                    plantillaPath, metadatos, contenido, controlCambios, firmas);

                rutaPdf = await GuardarArchivoBytesAsync(pdfBytes, ".pdf");
                rutaWord = await GuardarArchivoBytesAsync(pdfBytes, ".docx");
                tamañoKB = pdfBytes.Length / 1024f;
            }

            // CREAR VERSIÓN
            var version = new DocumentoVersion
            {
                Id_Documento = documento.Id_Documento,
                NumeroVersion = "1.0",
                Estado = (int)EstadoDocumento.Borrador,
                Fecha_Creacion = DateTime.UtcNow,
                Id_Usuario = dto.Id_Usuario,
                AprobadorId = dto.AprobadorId,
                EsActual = true,
                RutaPdf = rutaPdf,
                RutaWord = rutaWord,
                Tamaño_KB = tamañoKB
            };
            _repository.DocumentoVersion.Create(version);
            await _repository.SaveAsync();

            // TRAZABILIDAD
            await _trazabilidad.CreateTrazabilidadDocumento(
                documento.Id_Documento, "Documento creado", null,
                documento.Id_Usuario, null, rutaWord, rutaPdf,
                (int)EstadoDocumento.Borrador, (int)EstadoDocumento.Borrador, "-", "1.0");

            documento.VersionActualId = version.Id_Version;
            await _repository.SaveAsync();

            //await NotificarSolicitudCreacion(documento);

            return _mapper.Map<DocumentoDTO>(documento);
        }

        private async Task<string> ProcesarImagenesEnContenido(string contenidoJson)
        {
            if (string.IsNullOrEmpty(contenidoJson))
                return contenidoJson;

            try
            {
                var contenido = JsonSerializer.Deserialize<ContenidoDocumentoDto>(contenidoJson);
                if (contenido?.Bloques == null)
                    return contenidoJson;

                int imagenesProcesadas = 0;
                int errores = 0;

                for (int i = 0; i < contenido.Bloques.Count; i++)
                {
                    var bloque = contenido.Bloques[ i ];

                    if (bloque.Tipo == "imagen" && !string.IsNullOrEmpty(bloque.UrlImagen))
                    {
                        try
                        {
                            // Si la imagen es base64, guardarla como archivo
                            if (bloque.UrlImagen.StartsWith("data:image"))
                            {
                                

                                // Validar formato base64
                                var partes = bloque.UrlImagen.Split(',');
                                if (partes.Length < 2)
                                {
                                    
                                    errores++;
                                    continue;
                                }

                                var base64Data = partes[ 1 ];

                                // Validar que no esté vacío
                                if (string.IsNullOrEmpty(base64Data))
                                {
                                    
                                    errores++;
                                    continue;
                                }

                                var imageBytes = Convert.FromBase64String(base64Data);

                                // Determinar extensión
                                string extension = "png"; // por defecto
                                var mimePart = partes[ 0 ];
                                if (mimePart.Contains("jpeg") || mimePart.Contains("jpg"))
                                    extension = ".jpg";
                                else if (mimePart.Contains("png"))
                                    extension = ".png";
                                else
                                    extension = $".{mimePart.Split('/')[ 1 ].Split(';')[ 0 ]}";

                                // Guardar imagen
                                var imagePath = await GuardarArchivoBytesAsync(imageBytes, extension);
                                bloque.UrlImagen = imagePath;
                                imagenesProcesadas++;
                                
                            }
                            
                        }
                        catch (FormatException fex)
                        {
                            Console.WriteLine($" Error de formato en imagen {i + 1}: {fex.Message}");
                            errores++;
                            // Mantener la URL original si hay error
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($" Error procesando imagen {i + 1}: {ex.Message}");
                            Console.WriteLine($"   Stack: {ex.StackTrace}");
                            errores++;
                            // Mantener la URL original si hay error
                        }
                    }
                }

                

                return JsonSerializer.Serialize(contenido);
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error general en ProcesarImagenesEnContenido: {ex.Message}");
                Console.WriteLine($" Stack: {ex.StackTrace}");
                return contenidoJson; // Devolver el original si hay error
            }
        }

        private async Task<string> PrepararImagenesParaPdf(string contenidoJson)
        {
            if (string.IsNullOrEmpty(contenidoJson))
                return contenidoJson;

            try
            {
                var contenido = JsonSerializer.Deserialize<ContenidoDocumentoDto>(contenidoJson);
                if (contenido?.Bloques == null)
                {
                    return contenidoJson;
                }

                int imagenesPreparadas = 0;
                int errores = 0;

                for (int i = 0; i < contenido.Bloques.Count; i++)
                {
                    var bloque = contenido.Bloques[ i ];

                    if (bloque.Tipo == "imagen" && !string.IsNullOrEmpty(bloque.UrlImagen))
                    {
                        try
                        {
                            // Si la imagen es una ruta local del servidor, convertirla a base64
                            if (bloque.UrlImagen.StartsWith("/archivos/"))
                            {

                                var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                                    bloque.UrlImagen.TrimStart('/'));

                                if (File.Exists(physicalPath))
                                {
                                    var imageBytes = await File.ReadAllBytesAsync(physicalPath);
                                    var extension = Path.GetExtension(physicalPath).TrimStart('.');
                                    var base64 = Convert.ToBase64String(imageBytes);
                                    bloque.UrlImagen = $"data:image/{extension};base64,{base64}";
                                    imagenesPreparadas++;
                                    
                                }
                                else
                                {
                                    
                                    errores++;
                                }
                            }
                            else if (bloque.UrlImagen.StartsWith("data:image"))
                            {
                                
                                imagenesPreparadas++;
                            }
                            else if (bloque.UrlImagen.StartsWith("http"))
                            {
                                
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error preparando imagen {i + 1}: {ex.Message}");
                            errores++;
                            
                        }
                    }
                }

                return JsonSerializer.Serialize(contenido);
            }
            catch (JsonException jex)
            {
                Console.WriteLine($"Error deserializando JSON en PrepararImagenesParaPdf: {jex.Message}");
                return contenidoJson;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializando: {ex.Message}");
                return contenidoJson;
            }
        }

        // CREAR NUEVA VERSIÓN 
        public async Task CrearNuevaVersion(DocumentoForCreationDto dto, int idDocumento, string userId, IFormFile? archivo)
        {
            var documento = await _repository.Documento.GetDocumento(idDocumento, null, true);
            if (documento is null) throw new DocumentoNotFoundException(idDocumento);

            var versionActual = await _repository.DocumentoVersion.GetActual(idDocumento);
            if (versionActual is null) throw new DocumentoNotFoundException(idDocumento);

            var estadoActual = (EstadoDocumento)versionActual.Estado;
            if (estadoActual != EstadoDocumento.Vigente && estadoActual != EstadoDocumento.Aprobado)
                throw new InvalidOperationException(
                    $"Solo se puede crear una nueva versión desde documentos Vigentes o Aprobados. " +
                    $"Estado actual: {estadoActual}");

            var usuario = await _userManager.FindByIdAsync(userId);
            if (usuario == null) throw new Exception("Usuario no encontrado");
            if (usuario.IdProceso != documento.IdProceso)
                throw new UnauthorizedAccessException("No puedes crear versiones de documentos de otros procesos");

            var permisos = await _permisosService.ObtenerPermisosUsuario(userId);
            if (!permisos.Contains("DOCUMENTOS_NUEVA_VERSION"))
                throw new UnauthorizedAccessException("No tienes permiso para crear nuevas versiones");

            var tipoDocumento = await _repository.TipoDocumento.GetByIdAsync(dto.IdTipoDocumento, false);
            if (tipoDocumento == null) throw new Exception("Tipo de documento no encontrado");

            string descripcionCambio = dto.Comentario ?? "Nueva versión creada";
            string nuevaVersionTexto = GenerarSiguienteVersion(versionActual.NumeroVersion ?? "1.0", true);

            // 1. Desactivar versión actual
            versionActual.EsActual = false;
            await _repository.SaveAsync();

            // 2. Actualizar documento
            documento.Nombre = dto.Nombre;
            documento.Descripcion = dto.Descripcion;
            documento.AprobadorId = dto.AprobadorId;
            documento.IdTipoDocumento = dto.IdTipoDocumento;
            documento.Etiquetado = dto.Etiquetado;
            documento.IdProceso = dto.IdProceso;
            documento.Fecha_Modificacion = DateTime.UtcNow;
            documento.Id_Usuario = userId;

            if (!string.IsNullOrEmpty(dto.ContenidoData))
                documento.ContenidoJson = await ProcesarImagenesEnContenido(dto.ContenidoData);

            await _repository.SaveAsync();

            // 3. Construir control de cambios manualmente
            var usuarioActual = await _userManager.FindByIdAsync(userId);
            var nombreUsuario = $"{usuarioActual?.Nombre ?? "Sistema"} {usuarioActual?.Apellido ?? ""}".Trim();

            var versionesExistentes = await _repository.DocumentoVersion.GetByDocumentoId(idDocumento, false);
            var controlCambios = new List<ControlCambioDto>();

            foreach (var ver in versionesExistentes.OrderBy(v => v.Fecha_Creacion))
            {
                var user = await _userManager.FindByIdAsync(ver.Id_Usuario);
                string desc = (ver.NumeroVersion == "1.0" || ver == versionesExistentes.OrderBy(v => v.Fecha_Creacion).First())
                    ? "Versión original creada"
                    : ObtenerDescripcionCambio(ver);

                controlCambios.Add(new ControlCambioDto
                {
                    Version = ver.NumeroVersion ?? "1.0",
                    Fecha = ver.Fecha_Creacion.ToString("dd/MM/yyyy"),
                    Usuario = $"{user?.Nombre ?? "Sistema"} {user?.Apellido ?? ""}".Trim(),
                    Descripcion = desc
                });
            }

            // Agregar la nueva versión
            controlCambios.Add(new ControlCambioDto
            {
                Version = nuevaVersionTexto,
                Fecha = DateTime.UtcNow.ToString("dd/MM/yyyy"),
                Usuario = nombreUsuario,
                Descripcion = descripcionCambio
            });

            // 4. Generar PDF
            string rutaNueva, rutaWord;
            float tamañoKB;
            var proceso = await _repository.Proceso.GetByIdAsync(documento.IdProceso, false);

            if (archivo != null && archivo.Length > 0)
            {
                rutaNueva = await GuardarArchivoAsync(archivo);
                rutaWord = rutaNueva;
                tamañoKB = archivo.Length / 1024f;
            }
            else
            {
                var metadatos = ConstruirMetadatos(documento, nuevaVersionTexto);
                var contenidoJsonParaPdf = await PrepararImagenesParaPdf(documento.ContenidoJson);
                var contenido = DeserializarContenido(contenidoJsonParaPdf) ?? new ContenidoDocumentoDto();
                var firmas = DeserializarFirmas(documento.FirmasAprobacionJson);
                var plantillaPath = SeleccionarPlantilla(tipoDocumento?.Nombre ?? "");

                var pdfBytes = WordTemplateHelper.GenerarDocumentoFlexible(
                    plantillaPath, metadatos, contenido, controlCambios, firmas);

                rutaWord = await GuardarArchivoBytesAsync(pdfBytes, ".docx");
                rutaNueva = await GuardarArchivoBytesAsync(pdfBytes, ".pdf");
                tamañoKB = pdfBytes.Length / 1024f;
            }

            // 5. Crear nueva versión
            var nuevaVersion = new DocumentoVersion
            {
                Id_Documento = idDocumento,
                NumeroVersion = nuevaVersionTexto,
                Estado = (int)EstadoDocumento.Borrador,
                Fecha_Creacion = DateTime.UtcNow,
                Id_Usuario = userId,
                AprobadorId = dto.AprobadorId,
                EsActual = true,
                Tipo = documento.Tipo,
                RutaPdf = rutaNueva,
                RutaWord = rutaWord,
                Tamaño_KB = tamañoKB
            };

            _repository.DocumentoVersion.Create(nuevaVersion);
            await _repository.SaveAsync();

            documento.VersionActualId = nuevaVersion.Id_Version;
            await _repository.SaveAsync();

            // 6. Trazabilidad
            var rol = await ObtenerRolUsuario(userId);
            string descripcionTrazabilidad = string.IsNullOrEmpty(descripcionCambio)
                ? "Nueva versión creada"
                : $"NUEVA VERSIÓN: {descripcionCambio}";

            await _trazabilidad.CreateTrazabilidadDocumento(
                idDocumento, "NUEVA VERSIÓN", descripcionTrazabilidad,
                userId, rol,
                ConstruirUrlDescargaVersion(idDocumento, versionActual.Id_Version),
                ConstruirUrlDescargaVersion(idDocumento, nuevaVersion.Id_Version),
                (int)estadoActual, (int)EstadoDocumento.Borrador,
                versionActual.NumeroVersion, nuevaVersionTexto);

            await _repository.SaveAsync();
        }

        // CAMBIAR ESTADO 
        public async Task CambiarEstadoDocumento(int idDocumento, int nuevoEstado, string idUsuario,
            DocumentoForUpdateDto dto, string comentario)
        {
            var versionActual = await _repository.DocumentoVersion.GetActual(idDocumento);
            if (versionActual is null) throw new DocumentoNotFoundException(idDocumento);

            var estadoActual = (EstadoDocumento)versionActual.Estado;
            var estadoNuevo = (EstadoDocumento)nuevoEstado;

            

            // ✅ Validar si realmente hay cambio de estado
            if (estadoActual == estadoNuevo)
            {
                _logger.LogInfo($" El documento {idDocumento} ya está en estado {estadoActual}. No se realiza cambio.");
                return; // Salir temprano si no hay cambio
            }

            if (estadoNuevo == EstadoDocumento.Aprobado)
            {
                _logger.LogInfo($" Documento {idDocumento}: Aprobado → Vigente (automático)");
                estadoNuevo = EstadoDocumento.Vigente;
                nuevoEstado = (int)EstadoDocumento.Vigente;
            }

            if (!DocumentoWorkflowPolicy.TryGetPermiso(estadoActual, estadoNuevo, out var permiso))
                throw new InvalidOperationException($"Transición inválida {estadoActual} → {estadoNuevo}");

            var tienePermiso = await _permisosService.UsuarioTienePermiso(idUsuario, permiso);
            if (!tienePermiso) throw new UnauthorizedAccessException("No tienes permiso para esta acción");

            var doc = await _repository.Documento.GetDocumento(idDocumento, idUsuario, trackChanges: true);
            if (doc is null) throw new Exception("Documento no encontrado");

            var tipoDocumento = await _repository.TipoDocumento.GetByIdAsync(doc.IdTipoDocumento, false);
            if (tipoDocumento == null) throw new Exception("Tipo de documento no encontrado");

            if (estadoNuevo == EstadoDocumento.Aprobado || estadoNuevo == EstadoDocumento.Rechazado)
                if (doc.AprobadorId != idUsuario)
                    throw new UnauthorizedAccessException("Solo el aprobador asignado puede aprobar o rechazar");

            if (estadoNuevo == EstadoDocumento.Aprobado && versionActual.Id_Usuario == idUsuario)
                throw new InvalidOperationException("No puedes aprobar tu propio documento");

            string? rutaNueva = null, rutaWord = null;
            float tamañoKB = 0;

            // AL APROBAR: REGISTRAR FIRMA Y REGENERAR PDF
            if (estadoNuevo == EstadoDocumento.Vigente)
            {
                var fechaAprobacion = DateTime.Now;
                doc.Fecha_Aprobacion = fechaAprobacion;
                doc.Fecha_Modificacion = DateTime.Now;
                doc.Fecha_Revision = fechaAprobacion;

                var aprobador = await _userManager.FindByIdAsync(idUsuario);
                if (aprobador != null)
                {
                    doc.FirmasAprobacionJson = AgregarFirmaAprobador(
                        doc.FirmasAprobacionJson, aprobador.Nombre ?? "", aprobador.Apellido ?? "");
                }

                var proceso = await _repository.Proceso.GetByIdAsync(doc.IdProceso, false);
                var metadatos = ConstruirMetadatos(doc, versionActual.NumeroVersion ?? "1.0");
                metadatos[ "{Fecha_Aprobacion}" ] = fechaAprobacion.ToString("dd/MM/yyyy");
                metadatos[ "{Aprobador}" ] = $"{aprobador?.Nombre ?? ""} {aprobador?.Apellido ?? ""}".Trim();

                var contenidoJsonParaPdf = await PrepararImagenesParaPdf(doc.ContenidoJson);
                var contenido = DeserializarContenido(contenidoJsonParaPdf) ?? new ContenidoDocumentoDto();
                var controlCambios = await ConstruirControlCambios(idDocumento);
                var firmas = DeserializarFirmas(doc.FirmasAprobacionJson);
                var plantillaPath = SeleccionarPlantilla(tipoDocumento?.Nombre ?? "");

                var pdfBytes = WordTemplateHelper.GenerarDocumentoFlexible(
                    plantillaPath, metadatos, contenido, controlCambios, firmas);

                rutaNueva = await GuardarArchivoBytesAsync(pdfBytes, ".pdf");
                rutaWord = await GuardarArchivoBytesAsync(pdfBytes, ".docx");
                tamañoKB = pdfBytes.Length / 1024f;

                versionActual.RutaPdf = rutaNueva;
                versionActual.RutaWord = rutaWord;
                versionActual.Tamaño_KB = tamañoKB;
            }

            versionActual.Estado = (int)estadoNuevo;

            await _repository.SaveAsync();

            if ((int)estadoActual == (int)estadoNuevo) return;
            if (estadoNuevo == EstadoDocumento.EnRevision)
            {
                var aprobador = await _userManager.FindByIdAsync(doc.AprobadorId);
                var creador = await _userManager.FindByIdAsync(doc.Id_Usuario);
                var url = $"https://localhost:7168/documentos/{idDocumento}/preview";

                var cuerpo = PlantillasEmail.PlantillasEmail.DocumentoEnviadoRevision(  // Quitar el 's' de Plantillas
                    doc.Nombre, doc.Consecutivo,
                    $"{creador.Nombre} {creador.Apellido}",
                    $"{aprobador.Nombre} {aprobador.Apellido}",
                    url
                );

                await _emailService.EnviarNotificacionAsync(
                    aprobador.Email,
                    $"Documento para revisión y aprobación: {doc.Nombre}",
                    cuerpo

                );
            }
            else if (estadoNuevo == EstadoDocumento.Aprobado)
            {
                var creador = await _userManager.FindByIdAsync(doc.Id_Usuario);
                var aprobador = await _userManager.FindByIdAsync(idUsuario);
                var url = $"https://localhost:7168/documentos/{idDocumento}/preview";

                var cuerpo = PlantillasEmail.PlantillasEmail.DocumentoAprobado(
                    doc.Nombre, doc.Consecutivo,
                    $"{creador.Nombre} {creador.Apellido}",
                    $"{aprobador?.Nombre} {aprobador?.Apellido}", url
                );

                await _emailService.EnviarNotificacionAsync(
                    creador.Email,
                    $"Documento aprobado: {doc.Nombre}",
                    cuerpo
                );
            }
            else if (estadoNuevo == EstadoDocumento.Rechazado)
            {
                var creador = await _userManager.FindByIdAsync(doc.Id_Usuario);
                var aprobador = await _userManager.FindByIdAsync(idUsuario);
                var url = $"https://localhost:7168/documentos/{idDocumento}/preview";

                var cuerpo = PlantillasEmail.PlantillasEmail.DocumentoRechazado(
                    doc.Nombre, doc.Consecutivo,
                    $"{creador.Nombre} {creador.Apellido}",
                    $"{aprobador.Nombre} {aprobador.Apellido}",
                    comentario, url
                );

                await _emailService.EnviarNotificacionAsync(
                    creador.Email,
                    $"Documento rechazado: {doc.Nombre}",
                    cuerpo
                );
            }

            var rol = await ObtenerRolUsuario(idUsuario);
            string accion = estadoNuevo switch
            {
                EstadoDocumento.EnRevision => "Documento enviado a revisión",
                EstadoDocumento.Aprobado => "Documento aprobado",
                EstadoDocumento.Rechazado => "Documento rechazado",
                EstadoDocumento.Vigente => "Documento vigente",
                EstadoDocumento.Archivado => "Documento archivado",
                _ => $"Cambio estado {estadoActual} → {estadoNuevo}"
            };

            await _trazabilidad.CreateTrazabilidadDocumento(
                idDocumento, accion, comentario, idUsuario, rol, null, null,
                (int)estadoActual, (int)estadoNuevo, null, null);

            await _repository.SaveAsync();
        }

        // UPDATE DOCUMENTO 
        public async Task UpdateDocumento(int Id_Documento, string userId, DocumentoForUpdateDto dto,
            IFormFile? archivo, string? comentario)
        {
            var doc = await _repository.Documento.GetDocumento(Id_Documento, userId, trackChanges: true);
            if (doc is null) throw new Exception("Documento no encontrado");

            var usuario = await _userManager.FindByIdAsync(userId);
            if (usuario?.IdProceso != doc.IdProceso)
                throw new UnauthorizedAccessException("No puedes editar documentos de otros procesos");

            var versionActual = await _repository.DocumentoVersion.GetActual(Id_Documento);
            if (versionActual == null) throw new Exception("No existe versión actual");

            var tipoDocumento = await _repository.TipoDocumento.GetByIdAsync(dto.idTipoDocumento, false);
            if (tipoDocumento == null) throw new Exception("Tipo de documento no encontrado");

            var proceso = await _repository.Proceso.GetByIdAsync(dto.IdProceso, false);
            if (proceso == null) throw new Exception("Proceso no encontrado");

            var estadoActual = (EstadoDocumento)versionActual.Estado;
            string rutaAntigua = versionActual.RutaPdf;

            // GUARDAR valores antiguos ANTES de actualizar
            var nombreAntiguo = doc.Nombre;
            var descripcionAntigua = doc.Descripcion;
            var aprobadorAntiguo = doc.AprobadorId;
            var etiquetadoAntiguo = doc.Etiquetado;
            var procesoAntiguo = doc.IdProceso;
            var tipoAntiguo = doc.IdTipoDocumento;
            var contenidoAntiguo = doc.ContenidoJson;
            var consecutivoAntiguo = doc.ConsecutivoNumero;

            // Si cambio el tipo de documento
            bool tipoDocumentoCambio = tipoAntiguo != dto.idTipoDocumento;

            // Actualizar campos
            doc.Nombre = dto.Nombre;
            doc.Descripcion = dto.Descripcion;
            doc.AprobadorId = dto.AprobadorId;
            doc.Tipo = dto.Tipo;
            doc.Etiquetado = dto.Etiquetado;
            doc.IdProceso = dto.IdProceso;
            doc.IdTipoDocumento = dto.idTipoDocumento;
            doc.Fecha_Modificacion = DateTime.Now;

            if (tipoDocumentoCambio)
            {
                await NormalizarConsecutivos(tipoAntiguo, doc.IdProceso, consecutivoAntiguo);
                doc.Consecutivo = await GenerarConsecutivo(doc);
            }

            // En UpdateDocumento, cuando actualizas el contenido:
            if (!string.IsNullOrEmpty(dto.ContenidoData))
                doc.ContenidoJson = await ProcesarImagenesEnContenido(dto.ContenidoData);

            await _repository.SaveAsync();

            // DETECTAR CAMBIOS (comparando con valores antiguos)
            var cambios = new List<string>();
            if (nombreAntiguo != dto.Nombre) cambios.Add("Nombre");
            if (descripcionAntigua != dto.Descripcion) cambios.Add("Descripción");
            if (aprobadorAntiguo != dto.AprobadorId) cambios.Add("Aprobador");
            if (etiquetadoAntiguo != dto.Etiquetado) cambios.Add("Etiquetado");
            if (procesoAntiguo != dto.IdProceso) cambios.Add("Proceso");
            if (tipoDocumentoCambio)
            {
                cambios.Add($"Tipo de documento (consecutivo: {consecutivoAntiguo} -> {doc.Consecutivo}");
            }
            if (contenidoAntiguo != dto.ContenidoData) cambios.Add("Contenido");

            bool hayCambios = cambios.Any();
            string descripcionCambios = hayCambios ? $"Campos editados: {string.Join(", ", cambios)}" : "";

            if (estadoActual == EstadoDocumento.Borrador)
            {
                // En estado Borrador, SIEMPRE regenerar el PDF si hay cambios o archivo nuevo
                if (hayCambios || (archivo != null && archivo.Length > 0))
                {
                    string rutaNueva, rutaWord;
                    float tamañoKB;

                    if (archivo != null && archivo.Length > 0)
                    {
                        rutaNueva = await GuardarArchivoAsync(archivo);
                        rutaWord = rutaNueva;
                        tamañoKB = archivo.Length / 1024f;
                    }
                    else
                    {
                        var tipoDocActual = await _repository.TipoDocumento.GetByIdAsync(dto.idTipoDocumento, false);
                        // Regenerar PDF con los cambios
                        var metadatos = ConstruirMetadatos(doc, versionActual.NumeroVersion ?? "1.0");
                        var contenidoJsonParaPdf = await PrepararImagenesParaPdf(doc.ContenidoJson);
                        var contenido = DeserializarContenido(contenidoJsonParaPdf) ?? new ContenidoDocumentoDto();
                        var controlCambiosList = await ConstruirControlCambios(Id_Documento);
                        var firmas = DeserializarFirmas(doc.FirmasAprobacionJson);
                        var plantillaPath = SeleccionarPlantilla(tipoDocumento?.Nombre ?? "");

                        var pdfBytes = WordTemplateHelper.GenerarDocumentoFlexible(
                            plantillaPath, metadatos, contenido, controlCambiosList, firmas);

                        rutaNueva = await GuardarArchivoBytesAsync(pdfBytes, ".pdf");
                        rutaWord = await GuardarArchivoBytesAsync(pdfBytes, ".docx");
                        tamañoKB = pdfBytes.Length / 1024f;
                    }

                    // Actualizar la versión actual con los nuevos archivos
                    versionActual.RutaPdf = rutaNueva;
                    versionActual.RutaWord = rutaWord;
                    versionActual.Tamaño_KB = tamañoKB;
                    versionActual.Fecha_Creacion = DateTime.UtcNow;
                    await _repository.SaveAsync();

                    // Trazabilidad
                    var rol = await ObtenerRolUsuario(userId);
                    await _trazabilidad.CreateTrazabilidadDocumento(
                        Id_Documento, "DOCUMENTO EDITADO", descripcionCambios, userId, rol,
                        rutaAntigua, rutaNueva, (int)estadoActual, (int)estadoActual,
                        versionActual.NumeroVersion, versionActual.NumeroVersion);
                }
            }
            else if (estadoActual == EstadoDocumento.Rechazado)
            {
                if (hayCambios || (archivo != null && archivo.Length > 0))
                {
                    // Solo regenerar PDF, misma versión
                    string rutaNueva, rutaWord;
                    float tamañoKB;

                    if (archivo != null && archivo.Length > 0)
                    {
                        rutaNueva = await GuardarArchivoAsync(archivo);
                        rutaWord = rutaNueva;
                        tamañoKB = archivo.Length / 1024f;
                    }
                    else
                    {
                        var tipoDocRechazado = await _repository.TipoDocumento.GetByIdAsync(dto.idTipoDocumento, false);
                        var metadatos = ConstruirMetadatos(doc, versionActual.NumeroVersion ?? "1.0");
                        var contenido = DeserializarContenido(doc.ContenidoJson) ?? new ContenidoDocumentoDto();
                        var controlCambiosList = await ConstruirControlCambios(Id_Documento);
                        var firmas = DeserializarFirmas(doc.FirmasAprobacionJson);
                        var plantillaPath = SeleccionarPlantilla(tipoDocRechazado?.Nombre ?? "");

                        var pdfBytes = WordTemplateHelper.GenerarDocumentoFlexible(
                            plantillaPath, metadatos, contenido, controlCambiosList, firmas);

                        rutaNueva = await GuardarArchivoBytesAsync(pdfBytes, ".pdf");
                        rutaWord = await GuardarArchivoBytesAsync(pdfBytes, ".docx");
                        tamañoKB = pdfBytes.Length / 1024f;
                    }

                    // ✅ Actualizar la misma versión (NO crear nueva)
                    versionActual.RutaPdf = rutaNueva;
                    versionActual.RutaWord = rutaWord;
                    versionActual.Tamaño_KB = tamañoKB;
                    versionActual.Fecha_Creacion = DateTime.UtcNow;
                    await _repository.SaveAsync();

                    var rol = await ObtenerRolUsuario(userId);
                    await _trazabilidad.CreateTrazabilidadDocumento(
                        Id_Documento, "DOCUMENTO CORREGIDO (POST-RECHAZO)",
                        descripcionCambios, userId, rol,
                        rutaAntigua, rutaNueva,
                        (int)estadoActual, (int)estadoActual,
                        versionActual.NumeroVersion, versionActual.NumeroVersion);
                }
            }

            await _repository.SaveAsync();
        }

        // ELIMINAR, RECHAZAR, DESCARGAR, ETC.
        public async Task DeleteDocumento(int idDocumento, string userId)
        {
            var documento = await _repository.Documento.GetDocumento(idDocumento, null, true);
            if (documento == null) throw new DocumentoNotFoundException(idDocumento);

            var versionActual = await _repository.DocumentoVersion.GetActual(idDocumento);
            if (versionActual == null) throw new Exception("No existe versión actual");

            var estadoActual = (EstadoDocumento)versionActual.Estado;

            if (estadoActual != EstadoDocumento.Borrador)
                throw new InvalidOperationException(
                    $"Solo se pueden eliminar documentos en estado 'Borrador'. " +
                    $"Estado actual: {estadoActual}");

            var esCreador = documento.Id_Usuario == userId;

            if (!esCreador)
            {
                var permisos = await _permisosService.ObtenerPermisosUsuario(userId);
                var puedeEliminar = permisos.Contains("DOCUMENTOS_ELIMINAR");

                if (!puedeEliminar)
                    throw new UnauthorizedAccessException("No tienes permiso para eliminar este documento");
            }

            if (versionActual.Estado == (int)EstadoDocumento.Eliminado)
                throw new InvalidOperationException("El documento ya está eliminado");

            // ✅ Guardar información antes de eliminar
            var consecutivoEliminado = documento.ConsecutivoNumero;
            var idTipoDocumento = documento.IdTipoDocumento;
            var idProceso = documento.IdProceso;
            var consecutivoTexto = documento.Consecutivo;

            // Eliminación lógica
            versionActual.Estado = (int)EstadoDocumento.Eliminado;
            documento.Fecha_Modificacion = DateTime.UtcNow;

            // ✅ NO asignar NULL - usar un valor que indique eliminado
            // Si la columna no permite NULL, asignamos un valor especial
            documento.Consecutivo = $"(ELIMINADO)_{consecutivoTexto}";
            documento.ConsecutivoNumero = 0; // o -1 para indicar eliminado

            await _repository.SaveAsync();

            // ✅ Normalizar consecutivos - los documentos con número mayor bajan en 1
            await NormalizarConsecutivos(idTipoDocumento, idProceso, consecutivoEliminado);

            // Trazabilidad
            var rol = await ObtenerRolUsuario(userId);
            await _trazabilidad.CreateTrazabilidadDocumento(
                idDocumento, "Documento eliminado",
                $"Eliminado por: {(esCreador ? "Creador" : "Administrador")}. Consecutivo liberado: {consecutivoTexto}",
                userId, rol, null, null,
                (int)estadoActual, (int)EstadoDocumento.Eliminado,
                versionActual.NumeroVersion, versionActual.NumeroVersion);

            await _repository.SaveAsync();
        }

        private async Task NormalizarConsecutivos(string idTipoDocumento, string idProceso, int consecutivoEliminado)
        {
            try
            {
                // Solo actualizar documentos con consecutivo MAYOR al eliminado/liberado
                var docsAModificar = await _repository.Documento
                    .GetActivosConConsecutivoMayor(idTipoDocumento, idProceso, consecutivoEliminado, true);

                if (!docsAModificar.Any()) return;

                var tipoDocumento = await _repository.TipoDocumento.GetByIdAsync(idTipoDocumento, false);
                var proceso = await _repository.Proceso.GetByIdAsync(idProceso, false);

                var prefijoTipo = string.IsNullOrWhiteSpace(tipoDocumento?.Prefijo) ? "DOC" : tipoDocumento.Prefijo;
                var prefijoProceso = string.IsNullOrWhiteSpace(proceso?.Prefijo) ? "PROC" : proceso.Prefijo;

                foreach (var doc in docsAModificar)
                {
                    doc.ConsecutivoNumero -= 1; // Restar 1 para llenar el hueco
                    doc.Consecutivo = $"{prefijoTipo}{prefijoProceso}{doc.ConsecutivoNumero:D3}";
                }

                await _repository.SaveAsync();
                Console.WriteLine($"{docsAModificar.Count} consecutivos normalizados después de liberar posición {consecutivoEliminado}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error normalizando consecutivos: {ex.Message}");
            }
        }

        public async Task RechazarDocumento(int id, string comentario, string userID)
        {
            var versionActual = await _repository.DocumentoVersion.GetActual(id);
            if (versionActual is null) throw new DocumentoNotFoundException(id);

            var documento = await _repository.Documento.GetDocumento(id, null, true);
            if (documento.AprobadorId != userID)
                throw new UnauthorizedAccessException("Solo el aprobador asignado puede rechazar");
            if (versionActual.Estado != (int)EstadoDocumento.EnRevision)
                throw new InvalidOperationException("Solo se pueden rechazar documentos en revisión");

            versionActual.Estado = (int)EstadoDocumento.Rechazado;

            var rol = await ObtenerRolUsuario(userID);
            await _trazabilidad.CreateTrazabilidadDocumento(
                id, "Documento rechazado", comentario, userID, rol, null, null,
                (int)EstadoDocumento.EnRevision, (int)EstadoDocumento.Rechazado, null, null);

            await _repository.SaveAsync();
            try
            {
                var creador = await _userManager.FindByIdAsync(documento.Id_Usuario);
                var aprobador = await _userManager.FindByIdAsync(userID);
                var url = $"https://localhost:7168/documentos/{id}/preview";

                if (creador != null && !string.IsNullOrEmpty(creador.Email))
                {
                    var cuerpo = PlantillasEmail.PlantillasEmail.DocumentoRechazado(
                        documento.Nombre,
                        documento.Consecutivo,
                        $"{creador.Nombre} {creador.Apellido}",
                        $"{aprobador?.Nombre} {aprobador?.Apellido}",
                        comentario ?? "Sin comentarios",
                        url
                    );

                    await _emailService.EnviarNotificacionAsync(
                        creador.Email,
                        $"Documento rechazado: {documento.Nombre}",
                        cuerpo
                    );

                    _logger.LogInfo($" Correo de rechazo enviado a: {creador.Email}");
                }
                else
                {
                    _logger.LogWarn($" No se pudo enviar correo de rechazo: creador no encontrado o sin email");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error enviando correo de rechazo: {ex.Message}");
                // No lanzar excepción para no interrumpir el flujo
            }
        }

        public async Task<Documento> GetDocumentoParaDescargar(int id, string userId)
        {
            var doc = await _repository.Documento.GetByIdAsync(id, false);
            if (doc is null) throw new DocumentoNotFoundException(id);

            var permisos = await _permisosService.ObtenerPermisosUsuario(userId);
            var puedeDescargar = permisos.Contains("DOCUMENTOS_DESCARGAR");
            if (!puedeDescargar) throw new UnauthorizedAccessException("No tienes permiso para descargar");

            var estado = doc.VersionActual?.Estado;
            var esDescargable = estado == (int)EstadoDocumento.Aprobado
                || estado == (int)EstadoDocumento.Vigente
                || estado == (int)EstadoDocumento.Rechazado
                || estado == (int)EstadoDocumento.Borrador
                || estado == (int)EstadoDocumento.EnRevision;

            if (!esDescargable) throw new InvalidOperationException("El documento no está en un estado descargable");

            return doc;
        }

        public async Task<DocumentoVersion> GetVersionParaDescargar(int documentoId, int versionId, string userId)
        {
            var version = await _repository.Documento.GetVersionById(documentoId, versionId);
            if (version != null) version.Fecha_Revision = DateTime.Now;
            return version;
        }

        public async Task<byte[]> GenerarPdf(int id)
        {
            var documento = await _repository.Documento.GetByIdAsync(id, false);
            if (documento == null) throw new Exception("Documento no encontrado");

            var metadatos = ConstruirMetadatos(documento, documento.VersionActual?.NumeroVersion ?? "1.0");
            var contenido = DeserializarContenido(documento.ContenidoJson) ?? new ContenidoDocumentoDto();
            var controlCambios = await ConstruirControlCambios(id);
            var firmas = DeserializarFirmas(documento.FirmasAprobacionJson);
            var plantillaPath = SeleccionarPlantilla(documento.Tipo ?? "");

            return WordTemplateHelper.GenerarDocumentoFlexible(
                plantillaPath, metadatos, contenido, controlCambios, firmas);
        }

        public async Task<PagedList<ListadoDocumentoDto>> GetListadoMaestro(
            DocumentoParameters parametros, List<string> permisos, bool esConsultor,
            string userId, bool puedeArchivar, bool trackChanges)
        {
            var nivelesAcceso = new List<int> { (int)NivelAccesoDocumento.Publico };
            if (permisos.Contains("DOCUMENTOS_VER_INTERNOS")) nivelesAcceso.Add((int)NivelAccesoDocumento.UsoInterno);
            if (permisos.Contains("DOCUMENTOS_VER_RESTRINGIDOS")) nivelesAcceso.Add((int)NivelAccesoDocumento.Restringido);
            if (permisos.Contains("DOCUMENTOS_VER_CONFIDENCIALES")) nivelesAcceso.Add((int)NivelAccesoDocumento.Confidencial);

            bool esGestorDocumental = permisos.Contains("DOCUMENTOS_VER_TODOS") || puedeArchivar;

            return await _repository.Documento.GetListadoMaestro(
                parametros, nivelesAcceso, esConsultor, userId, esGestorDocumental, trackChanges);
        }

        public async Task<DocumentoVersion> GetVersionParaPreview(int documentoId, string userId)
        {
            var doc = await _repository.Documento.GetByIdAsync(documentoId, false);
            if (doc is null) throw new DocumentoNotFoundException(documentoId);

            var permisos = await _permisosService.ObtenerPermisosUsuario(userId);
            var puedeVerListado = permisos.Contains("DOCUMENTOS_VER_LISTADO");
            var esCreador = doc.Id_Usuario == userId;
            var esAprobador = doc.AprobadorId == userId;

            if (!puedeVerListado && !esCreador && !esAprobador)
                throw new UnauthorizedAccessException("No tienes permiso para ver este documento");

            var version = doc.VersionActual;
            if (version == null || string.IsNullOrEmpty(version.RutaPdf))
                throw new InvalidOperationException("El documento no tiene PDF disponible");

            return version;
        }

        public async Task RegistrarRevisionAsync(int documentoId, string userId)
        {
            var documento = await _repository.Documento.GetByIdAsync(documentoId, trackChanges: true);
            if (documento == null) throw new Exception("Documento no encontrado");
            documento.Fecha_Revision = DateTime.Now;
            documento.Id_Usuario = userId;
            await _repository.SaveAsync();
        }

        //Obtiene todas las alertas de revisión según la norma:
        //"Los documentos se revisan y actualizan por lo menos una vez cada dos años"
        public async Task<AlertasRevisionDTO> GetAlertasRevision(string userId)
        {
            try
            {
                // Verificar permisos
                var permisos = await _permisosService.ObtenerPermisosUsuario(userId);
                var puedeVerTodas = permisos.Contains("DOCUMENTOS_VER_ALERTAS");

                if (!puedeVerTodas)
                    throw new UnauthorizedAccessException("No tienes permisos para ver las alertas de revisión");

                // Calcular fecha límite: hace 2 años desde hoy
                var fechaLimite = DateTime.UtcNow.AddYears(-2);

                // Obtener documentos nunca revisados
                var nuncaRevisados = await _repository.Documento
                    .GetDocumentosNuncaRevisadosAsync(fechaLimite, trackChanges: false);

                // Obtener documentos con revisión vencida
                var revisionVencida = await _repository.Documento
                    .GetDocumentosRevisionVencidaAsync(fechaLimite, trackChanges: false);

                // Obtener documentos aprobados sin revisión
                var aprobadosSinRevision = await _repository.Documento
                    .GetDocumentosAprobadosSinRevisionAsync(fechaLimite, trackChanges: false);

                // Consolidar todas las alertas
                var todasAlertas = new List<AlertaDocumentoDTO>();

                // Procesar nunca revisados
                foreach (var doc in nuncaRevisados)
                {
                    var diasSinRevision = (int)(DateTime.UtcNow - doc.Fecha_Creacion.GetValueOrDefault()).TotalDays;
                    todasAlertas.Add(MapearDocumentoAAlerta(doc, "Nunca revisado", diasSinRevision));
                }

                // Procesar revisión vencida
                foreach (var doc in revisionVencida)
                {
                    var diasSinRevision = (int)(DateTime.UtcNow - doc.Fecha_Revision.GetValueOrDefault()).TotalDays;
                    todasAlertas.Add(MapearDocumentoAAlerta(doc, "Revisión vencida", diasSinRevision));
                }

                // Procesar aprobados sin revisión
                foreach (var doc in aprobadosSinRevision)
                {
                    var fechaReferencia = doc.Fecha_Revision ?? doc.Fecha_Aprobacion ?? doc.Fecha_Creacion;
                    var diasSinRevision = (int)(DateTime.UtcNow - fechaReferencia.GetValueOrDefault()).TotalDays;
                    todasAlertas.Add(MapearDocumentoAAlerta(doc, "Requiere revisión", diasSinRevision));
                }

                // Ordenar: más urgentes primero (más días sin revisión)
                todasAlertas = todasAlertas
                    .OrderByDescending(a => a.DiasSinRevision)
                    .ThenBy(a => a.Proceso)
                    .ThenBy(a => a.Nombre)
                    .ToList();

                // Calcular métricas
                var urgentes = todasAlertas.Count(a => a.DiasSinRevision > 730);
                var proximosAVencer = todasAlertas.Count(a => a.DiasSinRevision >= 700 && a.DiasSinRevision <= 730);
                var enRegla = todasAlertas.Count(a => a.DiasSinRevision < 700);

                // Generar resumen ejecutivo
                var resumen = GenerarResumenEjecutivo(todasAlertas.Count, urgentes, proximosAVencer);

                return new AlertasRevisionDTO
                {
                    TotalAlertas = todasAlertas.Count,
                    DocumentosUrgentes = urgentes,
                    DocumentosProximosAVencer = proximosAVencer,
                    DocumentosEnRegla = enRegla,
                    Alertas = todasAlertas,
                    FechaCorte = fechaLimite,
                    FechaGeneracion = DateTime.UtcNow,
                    ResumenEjecutivo = resumen
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en GetAlertasRevision: {ex.Message}");
                throw;
            }
        }

        // Mapea un Documento a AlertaDocumentoDTO
        private AlertaDocumentoDTO MapearDocumentoAAlerta(Documento doc, string tipoAlerta, int diasSinRevision)
        {
            // Determinar urgencia
            string urgencia;
            if (diasSinRevision > 1095)
                urgencia = "Critica";       // Más de 3 años
            else if (diasSinRevision > 730)
                urgencia = "Alta";          // Más de 2 años
            else if (diasSinRevision >= 700)
                urgencia = "Media";         // Próximo a vencer (2 años - 30 días)
            else
                urgencia = "Baja";          // En regla

            // Obtener versión actual
            var versionActual = doc.Versiones?
                .Where(v => v.EsActual)
                .OrderByDescending(v => v.Fecha_Creacion)
                .FirstOrDefault();

            var rutaPdf = versionActual?.RutaPdf ?? "";
            var rutaWord = versionActual?.RutaWord ?? "";
            var idVersion = versionActual?.Id_Version ?? 0;

            // Obtener nombre del estado
            var nombreEstado = versionActual?.Estado switch
            {
                (int)EstadoDocumento.Borrador => "Borrador",
                (int)EstadoDocumento.EnRevision => "En Revisión",
                (int)EstadoDocumento.Aprobado => "Aprobado",
                (int)EstadoDocumento.Rechazado => "Rechazado",
                (int)EstadoDocumento.Vigente => "Vigente",
                (int)EstadoDocumento.Archivado => "Archivado",
                (int)EstadoDocumento.Eliminado => "Eliminado",
                _ => "Desconocido"
            };

            return new AlertaDocumentoDTO
            {
                Id_Documento = doc.Id_Documento,
                Nombre = doc.Nombre ?? "Sin nombre",
                Descripcion = doc.Descripcion ?? "",
                Consecutivo = doc.Consecutivo ?? "N/A",
                TipoDocumento = doc.TipoDocumento?.Nombre ?? "Sin tipo",
                Proceso = doc.Proceso?.Nombre ?? "Sin proceso",
                IdProceso = doc.IdProceso ?? "",
                IdTipoDocumento = doc.IdTipoDocumento ?? "",
                FechaCreacion = doc.Fecha_Creacion,
                FechaUltimaRevision = doc.Fecha_Revision,
                FechaAprobacion = doc.Fecha_Aprobacion,
                FechaModificacion = doc.Fecha_Modificacion,
                DiasSinRevision = diasSinRevision,
                TipoAlerta = tipoAlerta,
                Urgencia = urgencia,
                VersionActual = versionActual?.NumeroVersion ?? "1.0",
                Estado = versionActual?.Estado ?? 0,
                NombreEstado = nombreEstado,
                Creador = $"{doc.User?.Nombre ?? ""} {doc.User?.Apellido ?? ""}".Trim(),
                Aprobador = $"{doc.Aprobador?.Nombre ?? ""} {doc.Aprobador?.Apellido ?? ""}".Trim(),
                RutaPdf = rutaPdf,
                RutaWord = rutaWord,
                IdVersion = idVersion,
            };
        }

        // Genera un resumen ejecutivo en texto de las alertas
        private string GenerarResumenEjecutivo(int total, int urgentes, int proximosAVencer)
        {
            if (total == 0)
                return "Todos los documentos están al día con sus revisiones.";

            var partes = new List<string>();

            if (urgentes > 0)
                partes.Add($"{urgentes} documento(s) tienen la revisión vencida (más de 2 años).");

            if (proximosAVencer > 0)
                partes.Add($"{proximosAVencer} documento(s) están próximos a vencer su revisión.");

            if (total > urgentes + proximosAVencer)
                partes.Add($"{total - urgentes - proximosAVencer} documento(s) adicionales requieren atención.");

            return string.Join(" ", partes);
        }

        // Método para cuando el líder del proceso envía el borrador a Gestión Integral
        public async Task EnviarBorradorAGestionIntegral(int idDocumento, string userId)
        {
            var doc = await _repository.Documento.GetDocumento(idDocumento, userId, true);
            if (doc == null) throw new DocumentoNotFoundException(idDocumento);

            var lider = await _userManager.FindByIdAsync(userId);
            var proceso = await _repository.Proceso.GetByIdAsync(doc.IdProceso, false);

            // Obtener el email de Gestión Integral (puede ser una configuración o un rol específico)
            var gestionIntegral = await _userManager.GetUsersInRoleAsync("GestionIntegral");
            var emailGestionIntegral = gestionIntegral.FirstOrDefault()?.Email;

            if (!string.IsNullOrEmpty(emailGestionIntegral))
            {
                var cuerpo = PlantillasEmail.PlantillasEmail.BorradorEnviadoGestionIntegral(
                    doc.Nombre,
                    doc.Consecutivo,
                    $"{lider.Nombre} {lider.Apellido}",
                    proceso?.Nombre ?? "No especificado",
                    $"https://localhost:7168/documentos/{idDocumento}/preview"
                );

                await _emailService.EnviarNotificacionConAdjuntoAsync(
                    emailGestionIntegral,
                    $"Borrador para revisión: {doc.Nombre}",
                    cuerpo,
                    null, // Aquí podrías adjuntar el PDF si lo deseas
                    null
                );
            }
        }

        // Método para divulgar cambios
        public async Task DivulgarCambios(int idDocumento, string tipoCambio, string descripcionCambio, List<string> destinatarios)
        {
            var doc = await _repository.Documento.GetByIdAsync(idDocumento, false);
            if (doc == null) throw new DocumentoNotFoundException(idDocumento);

            var proceso = await _repository.Proceso.GetByIdAsync(doc.IdProceso, false);

            var cuerpo = PlantillasEmail.PlantillasEmail.DivulgacionCambios(
                doc.Nombre,
                doc.Consecutivo,
                tipoCambio,
                descripcionCambio,
                proceso?.Nombre ?? "No especificado",
                $"https://localhost:7168/documentos/{idDocumento}/preview"
            );

            foreach (var destinatario in destinatarios)
            {
                await _emailService.EnviarNotificacionAsync(
                    destinatario,
                    $"Cambios en documento: {doc.Nombre}",
                    cuerpo
                );
            }
        }


        // MÉTODOS AUXILIARES
        private string SeleccionarPlantilla(string tipoDocumento)
        {
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Plantillas");
            return Path.Combine(basePath, "PlantillaGeneral.docx");
        }

        private Dictionary<string, string> ConstruirMetadatos(Documento doc, string version)
        {
            return new Dictionary<string, string>
            {
                { "{Nombre}", doc.Nombre ?? "" },
                { "{Descripcion}", doc.Descripcion ?? "" },
                { "{Consecutivo}", doc.Consecutivo ?? "" },
                { "{Version}", version },
                { "{Fecha_Creacion}", doc.Fecha_Creacion?.ToString("dd/MM/yyyy") ?? "" },
                { "{Fecha_Modificacion}", doc.Fecha_Modificacion?.ToString("dd/MM/yyyy") ?? "" },
                { "{Fecha_Aprobacion}", doc.Fecha_Aprobacion?.ToString("dd/MM/yyyy") ?? "" },
                { "{Proceso}", doc.Proceso?.Nombre ?? "" },
                { "{Etiquetado}", doc.Etiquetado ?? "" },
                { "{Creador}", $"{doc.User?.Nombre ?? ""} {doc.User?.Apellido ?? ""}".Trim() },
                { "{Aprobador}", "" }
            };
        }

        private async Task<List<ControlCambioDto>> ConstruirControlCambios(int documentoId)
        {
            var versiones = await _repository.DocumentoVersion.GetByDocumentoId(documentoId, false);
            var versionesList = versiones.OrderBy(v => v.Fecha_Creacion).ToList();
            var lista = new List<ControlCambioDto>();

            // ✅ Obtener las trazabilidades para obtener descripciones de cambios
            var trazabilidades = await _trazabilidad.GetTrazabilidadesPorDocumento(documentoId);

            for (int i = 0; i < versionesList.Count; i++)
            {
                var v = versionesList[ i ];
                var user = await _userManager.FindByIdAsync(v.Id_Usuario);

                // ✅ Buscar la trazabilidad correspondiente a esta versión
                string descripcion = "Versión original";

                if (i == 0)
                {
                    descripcion = "Versión original creada";
                }
                else
                {
                    // Buscar trazabilidad de NUEVA VERSIÓN para esta versión
                    var trazabilidadVersion = trazabilidades
                        .FirstOrDefault(t =>
                            t.Accion == "NUEVA VERSIÓN" &&
                            t.VersionNueva == v.NumeroVersion);

                    if (trazabilidadVersion != null && !string.IsNullOrEmpty(trazabilidadVersion.Comentario))
                    {
                        // Limpiar el prefijo "Nueva versión: " si existe
                        descripcion = trazabilidadVersion.Comentario
                            .Replace("Nueva versión: ", "")
                            .Replace("NUEVA VERSIÓN: ", "");
                    }
                    else
                    {
                        // Fallback: buscar trazabilidad de cambio de estado
                        var trazabilidadEstado = trazabilidades
                            .FirstOrDefault(t =>
                                t.VersionNueva == v.NumeroVersion &&
                                !string.IsNullOrEmpty(t.Comentario));

                        if (trazabilidadEstado != null)
                        {
                            descripcion = trazabilidadEstado.Comentario;
                        }
                        else
                        {
                            descripcion = ObtenerDescripcionCambio(v);
                        }
                    }
                }

                lista.Add(new ControlCambioDto
                {
                    Version = v.NumeroVersion ?? "1.0",
                    Fecha = v.Fecha_Creacion.ToString("dd/MM/yyyy"),
                    Usuario = $"{user?.Nombre ?? "Sistema"} {user?.Apellido ?? ""}".Trim(),
                    Descripcion = descripcion
                });
            }

            return lista;
        }

        private string ObtenerDescripcionCambio(DocumentoVersion v)
        {
            return v.Estado switch
            {
                (int)EstadoDocumento.Borrador => "Documento en elaboración",
                (int)EstadoDocumento.EnRevision => "Documento enviado a revisión para aprobación",
                (int)EstadoDocumento.Aprobado => "Documento aprobado",
                (int)EstadoDocumento.Rechazado => "Documento rechazado - requiere correcciones",
                (int)EstadoDocumento.Vigente => "Documento vigente",
                (int)EstadoDocumento.Archivado => "Documento archivado",
                _ => "Actualización de documento"
            };
        }

        private string GenerarSiguienteVersion(string versionActual, bool esMayor)
        {
            if (string.IsNullOrEmpty(versionActual)) return "1.0";
            var partes = versionActual.Split('.');
            int mayor = 1, menor = 0;
            if (partes.Length >= 1) int.TryParse(partes[ 0 ], out mayor);
            if (partes.Length >= 2) int.TryParse(partes[ 1 ], out menor);
            if (esMayor) { mayor++; menor = 0; }
            else { menor++; }
            return $"{mayor}.{menor}";
        }

        private async Task<string> ObtenerRolUsuario(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return "Desconocido";
            var roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault() ?? "Sin rol";
        }

        private string ConstruirUrlDescargaVersion(int documentoId, int versionId)
            => $"/api/documentos/{documentoId}/version/{versionId}/download";

        private async Task<string> GenerarConsecutivo(Documento documento)
        {
            var tipoDocumento = await _repository.TipoDocumento.GetByIdAsync(documento.IdTipoDocumento, false);
            if (tipoDocumento == null) throw new Exception($"No existe el tipo documento");

            var proceso = await _repository.Proceso.GetByIdAsync(documento.IdProceso, false);
            if (proceso == null) throw new Exception($"No existe el proceso");

            var prefijoTipo = string.IsNullOrWhiteSpace(tipoDocumento.Prefijo) ? "DOC" : tipoDocumento.Prefijo;
            var prefijoProceso = string.IsNullOrWhiteSpace(proceso.Prefijo) ? "PROC" : proceso.Prefijo;

            var ultimoDocumento = await _repository.Documento.GetUltimoConsecutivoPorPrefijo(
                documento.IdTipoDocumento, documento.IdProceso);

            int nuevoNumero = 1;
            if (ultimoDocumento != null) nuevoNumero = ultimoDocumento.ConsecutivoNumero + 1;

            documento.ConsecutivoNumero = nuevoNumero;
            return $"{prefijoTipo}{prefijoProceso}{nuevoNumero:D3}";
        }

        // FIRMAS Y CONTENIDO FLEXIBLE
        private string AgregarFirmaAprobador(string? firmasJson, string nombre, string apellido)
        {
            var firmas = new List<FirmaAprobadorDto>();
            if (!string.IsNullOrEmpty(firmasJson))
            {
                try { firmas = JsonSerializer.Deserialize<List<FirmaAprobadorDto>>(firmasJson) ?? new(); }
                catch { firmas = new(); }
            }

            firmas.Add(new FirmaAprobadorDto
            {
                Nombre = nombre ?? "",
                Apellido = apellido ?? "",
                Fecha_Aprobador = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")
            });

            return JsonSerializer.Serialize(firmas);
        }

        private ContenidoDocumentoDto? DeserializarContenido(string? contenidoJson)
        {
            if (string.IsNullOrEmpty(contenidoJson))
                return null;

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonObjectDictionaryConverter() } // Agregar el convertidor
                };

                var result = JsonSerializer.Deserialize<ContenidoDocumentoDto>(contenidoJson, options);

                // Log para depuración
                if (result?.Bloques != null)
                {
                    foreach (var bloque in result.Bloques)
                    {
                        if (bloque.Tipo == "tabla" && bloque.Metadatos != null)
                        {
                            Console.WriteLine($"📊 Tabla encontrada - Claves metadatos: {string.Join(", ", bloque.Metadatos.Keys)}");
                            foreach (var kvp in bloque.Metadatos)
                            {
                                Console.WriteLine($"   [{kvp.Key}] tipo: {kvp.Value?.GetType().Name ?? "null"}");
                                if (kvp.Value is JsonElement je)
                                {
                                    Console.WriteLine($"      JsonElement: {je.ValueKind}");
                                }
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializando contenido: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                return null;
            }
        }

        private List<FirmaAprobadorDto> DeserializarFirmas(string? firmasJson)
        {
            if (string.IsNullOrEmpty(firmasJson)) return new();
            try { return JsonSerializer.Deserialize<List<FirmaAprobadorDto>>(firmasJson) ?? new(); }
            catch { return new(); }
        }

        /*private async Task NotificarSolicitudCreacion(Documento documento)
        {
            var liderProceso = await _userManager.FindByIdAsync(documento.AprobadorId);
            var creador = await _userManager.FindByIdAsync(documento.Id_Usuario);
            var proceso = await _repository.Proceso.GetByIdAsync(documento.IdProceso, false);

            if (liderProceso != null)
            {
                var cuerpo = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='background: #8b5cf6; padding: 20px; border-radius: 10px 10px 0 0;'>
                <h2 style='color: white; margin: 0;'>Nueva Solicitud de Documento</h2>
            </div>
            <div style='background: white; padding: 20px; border-radius: 0 0 10px 10px; border: 1px solid #e5e7eb;'>
                <p>Hola <strong>{liderProceso.Nombre} {liderProceso.Apellido}</strong>,</p>
                <p>Se ha presentado una nueva solicitud para la aprobación de un documento.</p>
                
                <div style='background: #f3f4f6; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                    <p style='margin: 0;'><strong>Documento:</strong> {documento.Nombre}</p>
                    <p style='margin: 5px 0;'><strong>Proceso:</strong> {proceso?.Nombre ?? "No especificado"}</p>
                    <p style='margin: 5px 0;'><strong>Solicitante:</strong> {creador?.Nombre} {creador?.Apellido}</p>
                    <p style='margin: 5px 0;'><strong>Fecha:</strong> {DateTime.Now:dd/MM/yyyy}</p>
                </div>

                <p>Por favor, revise la viabilidad de esta solicitud según las necesidades de la organización.</p>
                
                <a href='https://localhost:7168/documentos/{documento.Id_Documento}/preview' 
                   style='display: inline-block; padding: 12px 24px; background: #8b5cf6; color: white; text-decoration: none; border-radius: 8px; font-weight: bold;'>
                    Revisar Solicitud
                </a>
                
                <p style='margin-top: 20px; color: #6b7280; font-size: 12px;'>
                    Este es un mensaje automático del Sistema de Gestión Documental FileNova.
                </p>
            </div>
        </div>";

                await _emailService.EnviarNotificacionAsync(
                    liderProceso.Email,
                    $"Nueva solicitud de documento: {documento.Nombre}",
                    cuerpo
                );
            }
        }*/

    }
}