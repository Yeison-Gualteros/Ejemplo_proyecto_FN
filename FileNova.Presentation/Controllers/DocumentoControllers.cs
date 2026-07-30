using Entities.Enums;
using Entities.Exceptions;
using Entities.Models;
using FileNova.Presentation.Authorization;
using Marvin.Cache.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Shared.DataTransferObjects;
using Shared.DataTransferObjects.Documentos;
using Shared.RequestFeatures;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace FileNova.Presentation.Controllers
{
    [Route("api/documentos")]
    [ApiController]
    public class DocumentoControllers : ControllerBase
    {
        private readonly IServiceManager _service;
        private readonly UserManager<User> _userManager;


        public DocumentoControllers(IServiceManager service, IServiceManager serviceManager, UserManager<User> userManager)
        {
            _service = service;
            _userManager = userManager;

        }



        //Get /api/documetnos
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HasPermission("DOCUMENTOS_VER")]
        [HttpGet]
        public async Task<IActionResult> GetAllDocumentos([FromQuery] DocumentoParameters documentoParameters)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { error = "Usuario no autenticado" });

                var documentos = await _service.DocumentoService
                    .GetAllDocumentos(documentoParameters, userId, false);

                var paginacionJson = JsonSerializer.Serialize(documentos.metaData);

                Console.WriteLine($"X-Pagination JSON: {paginacionJson}");

                Response.Headers.Append("X-Pagination", paginacionJson);

                return Ok(documentos.documentos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetAllDocumentos: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }


        // Get /api/documentos/{Id_Documento}
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("{Id_Documento:int}", Name = "GetDocumento")]
        [ResponseCache(NoStore = true, Duration = 0)]
        [HasPermission("DOCUMENTOS_VER")]
        public async Task<IActionResult> GetDocumentoById(int Id_Documento)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { error = "Usuario no autenticado" });

                var documento = await _service.DocumentoService
                    .GetDocumento(Id_Documento, userId, trackChanges: false);

                Console.WriteLine($"Documento {Id_Documento} - IdProceso: {documento.IdProceso}");

                return Ok(documento);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetDocumentoById: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("{id:int}")]
        [HasPermission("DOCUMENTOS_EDITAR")]
        public async Task<IActionResult> UpdateDocumento( int id, [FromForm] DocumentoForUpdateDto dto, [FromForm] IFormFile? archivo)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { error = "Usuario no autenticado" });

                // ✅ Validar que el usuario pertenece al proceso del documento
                if (!await ValidarAccesoDocumento(id, userId, "editar"))
                    return Forbid();

                await _service.DocumentoService.UpdateDocumento(id, userId, dto, archivo, dto.Comentario);

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en UpdateDocumento: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Post /api/documento/
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("upload", Name = "CreateDocumento")]
        [ResponseCache(NoStore = true, Duration = 0)]
        [HasPermission("DOCUMENTOS_CREAR")]
        public async Task<IActionResult> SubirDocumento([FromForm] DocumentoForCreationDto documento, [FromForm] IFormFile? archivo)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userId == null)
                    return Unauthorized(new { error = "Usuario no autenticado" });

                // ✅ Validar que el usuario pertenece al proceso donde intenta crear
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return Unauthorized(new { error = "Usuario no encontrado" });

                var esAdmin = await _userManager.IsInRoleAsync(user, "Administrador")
                           || await _userManager.IsInRoleAsync(user, "Admin");

                if (!esAdmin && user.IdProceso != documento.IdProceso)
                    return Forbid();

                documento.Id_Usuario = userId;

                // Validar archivo
                if (archivo != null)
                {
                    var extensionesPermitidas = new[] { ".pdf", ".docx", ".xlsx" };
                    var extension = Path.GetExtension(archivo.FileName).ToLower();

                    if (!extensionesPermitidas.Contains(extension))
                        return BadRequest(new { error = "Tipo de archivo no permitido." });

                    if (archivo.Length > 5 * 1024 * 1024)
                        return BadRequest(new { error = "Archivo demasiado grande, máximo 5MB." });

                    documento.Tamaño_KB = archivo.Length / 1024f;
                }
                else
                {
                    documento.Tamaño_KB = 0;
                }

                Console.WriteLine("ContenidoData: " + documento.ContenidoData);

                documento.Fecha_Subida = DateTime.Now;

                var documentoEntity = await _service.DocumentoService
                    .CreateDocumentoAsync(documento, archivo);

                return Ok(documentoEntity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en SubirDocumento: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPatch("{id}/enviar-revision")]
        [HasPermission("DOCUMENTOS_ENVIAR_REVISION")]
        public async Task<IActionResult> EnviarRevision(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Usuario no autenticado");

            try
            {
                await _service.DocumentoService
                    .CambiarEstadoDocumento(id, (int)EstadoDocumento.EnRevision, userId, dto: null, comentario: null);

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("{Id_Documento:int}/version")]
        [HasPermission("DOCUMENTOS_NUEVA_VERSION")]
        public async Task<IActionResult> CrearNuevaVersion(int Id_Documento, [FromForm] DocumentoForCreationDto dto, [FromForm] IFormFile archivo)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            await _service.DocumentoService
                .CrearNuevaVersion(dto, Id_Documento, userId, archivo);

            return NoContent();
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HasPermission("DOCUMENTOS_APROBAR")]
        [HttpPatch("{id}/rechazar")]
        public async Task<IActionResult> RechazarDocumento(int id, [FromBody] RevisionDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Usuario no autenticado");

            if (string.IsNullOrWhiteSpace(dto.Comentario))
                return BadRequest("El comentario es obligatorio");

            await _service.DocumentoService.RechazarDocumento(id, dto.Comentario, userId);

            return NoContent();
        }

        // delete /api/documentos/{Id_Documento}
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpDelete("{Id_Documento:int}", Name = "EliminarDocumento")]
        [ResponseCache(NoStore = true, Duration = 0)]
        [HasPermission("DOCUMENTOS_ELIMINAR")] // Esto ya valida el permiso
        public async Task<IActionResult> DeleteDocumento(int Id_Documento)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { error = "Usuario no autenticado" });

                // Validar acceso al proceso
                if (!await ValidarAccesoDocumento(Id_Documento, userId, "eliminar"))
                    return Forbid();

                await _service.DocumentoService.DeleteDocumento(Id_Documento, userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en DeleteDocumento: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpOptions(Name = "GetDocumetnosOptions")]
        [ResponseCache(NoStore = true, Duration = 0)]
        [Authorize(Roles = "Administrador")]
        public IActionResult GetDocumetnosOptions()
        {
            Response.Headers.Add("Allow", "GET, OPTIONS, POST, PUT");
            return Ok();
        }

        [HttpPatch("{id}/estado")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] DocumentoForUpdateDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            await _service.DocumentoService
                .CambiarEstadoDocumento(id, dto.Estado, userId, dto, comentario: null);

            return NoContent();
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HasPermission("DOCUMENTOS_DESCARGAR")]
        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var doc = await _service.DocumentoService.GetDocumentoParaDescargar(id, userId);

            if (doc == null)
                return NotFound("Documento no encontrado");

            var ruta = doc.VersionActual?.RutaPdf;

            if (string.IsNullOrEmpty(ruta))
                return BadRequest("El documento no tiene PDF asociado");

            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                ruta.TrimStart('/')
            );

            if (!System.IO.File.Exists(path))
                return NotFound("Archivo no encontrado en el servidor");

            var bytes = await System.IO.File.ReadAllBytesAsync(path);
 
            var nombreLimpio = LimpiarNombre(doc.Nombre);
            var procesoLimpio = LimpiarNombre(doc.TipoDocumento?.Nombre ?? "SIN_PROCESO");

            var nombreArchivo = $"{doc.Consecutivo}_{procesoLimpio}_{nombreLimpio}.pdf";
            nombreArchivo = nombreArchivo.Replace(" ", "_");

            Response.Headers.Add("Access-Control-Expose-Headers", "Content-Disposition");

            return File(bytes, "application/pdf", nombreArchivo);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HasPermission("DOCUMENTOS_DESCARGAR")]
        [HttpGet("{id}/version/{versionId}/download")]
        public async Task<IActionResult> DownloadVersion(DocumentoForUpdateDto dto, int id, int versionId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var version = await _service.DocumentoService
                .GetVersionParaDescargar(id, versionId, userId);

            if (version == null)
                return NotFound("Versión no encontrada");

            var ruta = version.RutaPdf;

            if (string.IsNullOrEmpty(ruta))
                return BadRequest("La versión no tiene PDF");

            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                ruta.TrimStart('/')
            );

            if (!System.IO.File.Exists(path))
                return NotFound("Archivo no encontrado");

            var bytes = await System.IO.File.ReadAllBytesAsync(path);

            var documento = await _service.DocumentoService.GetDocumentoParaDescargar(id, userId);

            var nombreLimpio = LimpiarNombre(documento.Nombre);
            var procesoLimpio = LimpiarNombre(documento.Proceso?.Nombre ?? "SIN_PROCESO");

            var nombreArchivo = $"{documento.Consecutivo}_{procesoLimpio}_{nombreLimpio}_v{version.NumeroVersion}.pdf";
            nombreArchivo = nombreArchivo.Replace(" ", "_");

            return File(bytes, "application/pdf", nombreArchivo);
        }

        [HttpGet("{id}/preview")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HasPermission("DOCUMENTOS_VER_LISTADO")]
        public async Task<IActionResult> PreviewDocumento(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Usuario no autenticado");

                // Usar el método simple de preview
                var version = await _service.DocumentoService.GetVersionParaPreview(id, userId);

                var path = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    version.RutaPdf.TrimStart('/')
                );

                if (!System.IO.File.Exists(path))
                    return NotFound($"Archivo no encontrado: {path}");

                var bytes = await System.IO.File.ReadAllBytesAsync(path);

                // Configurar para visualización inline
                Response.Headers.Add("Content-Disposition", "inline; filename=\"documento.pdf\"");

                return File(bytes, "application/pdf");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (DocumentoNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en preview: {ex}");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpGet("ver-pdf/{id}")]
        public async Task<IActionResult> VerPdf(int id)
        {
            var pdfBytes = await _service.DocumentoService.GenerarPdf(id);

            return File(pdfBytes, "application/pdf");
        }

        [HttpGet("descargar-pdf/{id}")]
        public async Task<IActionResult> DescargarPdf(int id)
        {
            var pdfBytes = await _service.DocumentoService.GenerarPdf(id);

            return File(pdfBytes, "application/pdf", $"documento_{id}.pdf");
        }

        [HttpGet("{id}/download-word")]
        public async Task<IActionResult> DownloadWord(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var doc = await _service.DocumentoService.GetDocumentoParaDescargar(id, userId);

            var ruta = doc.VersionActual?.RutaWord;

            if (string.IsNullOrEmpty(ruta))
                return NotFound("No hay Word disponible");

            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                ruta.TrimStart('/')
            );

            if (!System.IO.File.Exists(path))
                return NotFound();

            var bytes = await System.IO.File.ReadAllBytesAsync(path);

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"{doc.Nombre}.docx");
        }

        [HttpGet("{id}/version/{versionId}/preview")]
        public async Task<IActionResult> PreviewVersion(int id, int versionId)
        {
            

            var version = await _service.DocumentoService
                .GetVersionParaDescargar(id, versionId, null);
            

            if (version == null)
                return NotFound("Versión no encontrada");

            if (string.IsNullOrEmpty(version.RutaPdf))
                return NotFound("La versión no tiene PDF");

            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                version.RutaPdf.TrimStart('/')
            );

            if (!System.IO.File.Exists(path))
                return NotFound("Archivo no encontrado");

            var bytes = await System.IO.File.ReadAllBytesAsync(path);
            

            return File(bytes, "application/pdf");
        }

        string LimpiarNombre(string nombre)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                nombre = nombre.Replace(c, '_');
            }
            return nombre;
        }

        [HttpGet("listado-maestro")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HasPermission("DOCUMENTOS_VER_LISTADO")]
        public async Task<IActionResult> GetListadoMaestro([FromQuery] DocumentoParameters parametros)
        {
            try
            {
                var user = HttpContext.User;

                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var roles = user.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

                bool puedeArchivar = roles.Contains("Gestor Documental")
                  || roles.Contains("Admin");

                var permisos = user.FindAll("permission")
                    .Select(p => p.Value)
                    .ToList();

                bool esConsultor = roles.Contains("Consultor");

                var result = await _service.DocumentoService
                    .GetListadoMaestro(parametros, permisos, esConsultor, userId, puedeArchivar, trackChanges: false);

                Response.Headers.Add(
                    "X-Pagination",
                    JsonSerializer.Serialize(result.MetaData)
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id}/registrar-revision")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> RegistrarRevision(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Usuario no autenticado");

                // Verificar si el usuario tiene permiso para revisar
                var tienePermiso = User.HasClaim("permission", "DOCUMENTOS_REVISAR") ||
                                   User.IsInRole("Gestor Documental");

                if (!tienePermiso)
                {
                    // Si no tiene permiso, simplemente retornamos OK sin hacer 
                    return Ok(new { actualizado = false, mensaje = "Usuario sin permiso de revisión" });
                }

                await _service.DocumentoService.RegistrarRevisionAsync(id, userId);

                return Ok(new { actualizado = true, mensaje = "Fecha de revisión actualizada" });
            }
            catch (Exception ex)
            {
                // No lanzar error, solo registrar
                Console.WriteLine($"Error al registrar revisión: {ex.Message}");
                return Ok(new { actualizado = false, mensaje = ex.Message });
            }
        }

        // GET /api/documentos/alertas-revision
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HasPermission("DOCUMENTOS_VER_ALERTAS")] // Solo administradores/SGI
        [HttpGet("alertas-revision")]
        public async Task<IActionResult> GetAlertasRevision()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { error = "Usuario no autenticado" });

                var alertas = await _service.DocumentoService.GetAlertasRevision(userId);

                return Ok(alertas);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetAlertasRevision: {ex.Message}");
                return StatusCode(500, new { error = "Error interno al generar alertas de revisión" });
            }
        }

        

        private async Task<bool> ValidarAccesoDocumento(int idDocumento, string userId, string accion)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Los administradores tienen acceso total
            var esAdmin = await _userManager.IsInRoleAsync(user, "Administrador")
                       || await _userManager.IsInRoleAsync(user, "Admin");
            if (esAdmin) return true;

            // Validar pertenencia al proceso
            var documento = await _service.DocumentoService.GetDocumento(idDocumento, userId, false);
            if (documento == null) return false;

            if (user.IdProceso != documento.IdProceso)
            {
                Console.WriteLine($"Usuario {userId} intentó {accion} documento {idDocumento} de otro proceso");
                return false;
            }

            return true;
        }
    }
}