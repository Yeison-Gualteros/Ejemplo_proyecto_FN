using Contracts;
using Entities.Models;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Shared.DataTransferObjects.Permisos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileNova.Presentation.Controllers
{
    [Route("api/permisos")]
    [ApiController]
    public class PermisosControllers : ControllerBase
    {
        private readonly IServiceManager _serviceManager;

        public PermisosControllers(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        // GET: api/permisos/all
        [HttpGet("all")]
        public async Task<IActionResult> GetAllPermisos()
        {
            try
            {
                // Llamada al servicio de permisos
                var permisos = await _serviceManager.permisosService.GetAllPermisos(null, false);

                // Retornamos solo los campos necesarios
                var permisosDto = permisos.Select(p => new
                {
                    Id_Permiso = p.Id_Permiso,
                    Nombre = p.Nombre
                });

                return Ok(permisosDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error interno al obtener permisos",
                    detail = ex.Message
                });
            }
        }

        [HttpPut("user-permisos")]
        public async Task<IActionResult> UpdateUserPermissions([FromBody] SaveUserPermisosDto dto)
        {
            if (dto == null)
                return BadRequest("DTO es NULL (body mal enviado)");

            if (dto.PermisosIds == null)
                return BadRequest("PermisosIds es NULL");

            await _serviceManager.permisosService
                .SaveUserPermisos(dto.UserId, dto.PermisosIds);

            return NoContent();
        }

        [HttpGet("{userId}/permisos")]
        public async Task<IActionResult> GetUserPermissions(string userId, [FromQuery] bool trackChanges)
        {
            var permisos = await _serviceManager.permisosService.GetUserPermisos(userId, trackChanges);
            if (permisos == null || !permisos.Any())
                return NotFound("Este usuario no tiene permisos.");

            return Ok(permisos);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPermisos([FromQuery] int? id_Permisos, [FromQuery] bool trackChanges = false)
        {
            try
            {
                var permisos = await _serviceManager
                    .permisosService.GetAllPermisos(id_Permisos, trackChanges);

                if (permisos == null || !permisos.Any())
                    return Ok(Enumerable.Empty<object>());

                return Ok(permisos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error interno al obtener permisos",
                    detail = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePermiso([FromBody] PermisosForCreationDto permiso)
        {
            if (permiso == null || string.IsNullOrWhiteSpace(permiso.Nombre))
                return BadRequest("El nombre del permiso es obligatorio.");

            var creado = await _serviceManager.permisosService.CreatePermiso(permiso);
            return CreatedAtAction(nameof(GetPermisoById), new { id = creado.Id_Permiso }, creado);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPermisoById(int id, [FromQuery] bool trackChanges = false)
        {
            var permiso = await _serviceManager.permisosService.GetPermisoById(id, trackChanges);
            if (permiso == null)
                return NotFound("Permiso no encontrado.");

            return Ok(permiso);
        }

        [HttpGet("{roleId}/permisos-ui")]
        public async Task<IActionResult> GetPermisosUIByRole(string roleId)
        {
            var permisos = await _serviceManager.permisosService
                .GetPermisosUIByRole(roleId);

            return Ok(permisos);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePermiso(int id, [FromBody] PermisoForUpdateDto permisoForUpdate)
        {
            if (permisoForUpdate == null || string.IsNullOrWhiteSpace(permisoForUpdate.Nombre))
                return BadRequest("El nombre del permiso es obligatorio.");

            var actualizado = await _serviceManager.permisosService.UpdatePermiso(id, permisoForUpdate);
            if (actualizado == null)
                return NotFound("Permiso no encontrado.");

            return Ok(actualizado);
        }


        
        // Eliminar un permiso
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePermiso(int id)
        {
            var eliminado = await _serviceManager.permisosService.DeletePermiso(id);
            if (!eliminado)
                return NotFound("Permiso no encontrado.");

            return NoContent();
        }

        [HttpPost("{roleId}/permisos")]
        public async Task<IActionResult> AddPermissionsToRole(string roleId, [FromBody] List<int> permisosIds)
        {
            if (permisosIds == null || !permisosIds.Any())
                return BadRequest("Debe seleccionar al menos un permiso.");

            await _serviceManager.permisosService.AddPermissionsToRole(roleId, permisosIds);
            return Ok("Permisos asignados correctamente al rol.");
        }

        [HttpDelete("{userId}/permisos/{permisoId}")]
        public async Task<IActionResult> RemovePermissionFromUser(string userId, int permisoId)
        {
            await _serviceManager.permisosService.RemovePermissionFromUser(userId, permisoId);
            return NoContent();  
        }
    }
}
