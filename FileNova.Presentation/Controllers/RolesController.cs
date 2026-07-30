using Contracts.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Shared.RequestFeatures;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Shared.DataTransferObjects.Roles;
using Microsoft.AspNetCore.Http.HttpResults;
using Entities.Models;


namespace FileNova.Presentation.Controllers
{
    [Route("api/roles")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly RoleManager<Role> _roleManager;
        private readonly IServiceManager _service;
       

        public RolesController(RoleManager<Role> roleManager, IServiceManager service)
        {
            _roleManager = roleManager;
            _service = service;
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetAllRoles([FromQuery] RoleParameters roleParameters)
        {
            var roles = await _service.RoleService.GetAllRoles(roleParameters, trackChanges: false);

            Response.Headers["X-Pagination"] = JsonSerializer.Serialize(roles.metaData);


            return Ok(new
            {
                roles = roles.roles,
                metaData = roles.metaData
            });

        }

        [HttpGet("{id}/permisos")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetPermisosDelRol(string id)
        {
            var permisos = await _service.permisosService.GetPermissionsByRole(id);
            if (permisos == null)
            {
                return NotFound(new { message = "No se encontraron permisos para este rol" });
            }

            return Ok(permisos);
        }


        [HttpGet("{id}", Name ="GetRol")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetRoleById(string id)
        {
            var role = await _service.RoleService.GetRoleById(id, trackChanges: false);
            return Ok(role);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ActualizarRol(string id, [FromBody] RolForUpdateDto rolForUpdate)
        {
            if (rolForUpdate is null)
                return BadRequest("La actualización es nula");

            // Convertir id a Guid
            if (!Guid.TryParse(id, out Guid roleGuid))
                return BadRequest("ID de rol inválido");

            // Actualizar datos básicos del rol
            var rolActualizado = await _service.RoleService.ActualizarRol(id, rolForUpdate, trackChanges: true);

            // Actualizar permisos
            if (rolForUpdate.Permisos != null)
            {
                await _service.permisosService.UpdatePermissionsOfRole(id, rolForUpdate.Permisos);
            }

            // Obtener permisos actualizados para enviar al front
            var permisosActualizados = await _service.permisosService.GetPermissionsByRole(roleGuid.ToString());

            var rolDtoConPermisos = new
            {
                rolActualizado.Id,
                rolActualizado.Name,
                Permisos = permisosActualizados.Select(p => p.Id_Permiso)
            };

            return Ok(rolDtoConPermisos);
        }

        // post /api/roles
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CreateRole([FromBody] RolForCreationDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "El nombre del rol no puede estar vacío." });

            var roleExists = await _roleManager.RoleExistsAsync(dto.Name);
            if (roleExists)
                return Conflict(new { message = $"El rol '{dto.Name}' ya existe." });

            var role = new Role { Name = dto.Name };
            var result = await _roleManager.CreateAsync(role);

            if (!result.Succeeded)
                return BadRequest(new { message = "Error al crear el rol", errors = result.Errors.Select(e => e.Description) });

            // Asignar permisos si vienen
            if (dto.Permisos != null && dto.Permisos.Any())
            {
                await _service.permisosService.AddPermissionsToRole(role.Id, dto.Permisos);
            }

            return CreatedAtAction(nameof(GetRoleById), new { id = role.Id }, new
            {
                id = role.Id,
                name = role.Name,
                normalizedName = role.NormalizedName
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRol(string id)
        {
            try
            {
                await _service.RoleService.DeleteRol(id, trackChanges: true);
                return Ok(new { message = "Rol eliminado correctamente" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "El rol no existe" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }
    }
}