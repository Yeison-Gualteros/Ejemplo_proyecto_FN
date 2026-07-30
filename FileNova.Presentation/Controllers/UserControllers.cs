using Entities.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Shared.DataTransferObjects.Permisos;
using Shared.DataTransferObjects.User;
using Shared.RequestFeatures;
using System.Security.Claims;
using System.Text.Json;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IServiceManager _services;
    private readonly UserManager<User> _userManager;

    public UsersController(IServiceManager services, UserManager<User> userManager)
    {
        _services = services;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] UserParameters parameters)
    {
        var users = await _services.UserService.GetAllAsync(parameters);

        Response.Headers.Add("X-Pagination",
            JsonSerializer.Serialize(users.MetaData));

        return Ok(new
        {
            data = users,
            meta = users.MetaData
        });
    }

    [HttpGet("aprobadores")]
    public async Task<IActionResult> GetAprobadores()
    {
        var usuarios = await _services.UserService.GetUsuariosConPermiso("DOCUMENTOS_APROBAR");

        return Ok(usuarios);
    }

    //Endpoint para obtener aprobadores por proceso
    [HttpGet("aprobadores/proceso/{idProceso}")]
    public async Task<IActionResult> GetAprobadoresByProceso(string idProceso)
    {
        var usuarios = await _services.UserService.GetUsuariosConPermisoByProcesoAsync("DOCUMENTOS_APROBAR", idProceso);
        return Ok(usuarios);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(string id)
        => Ok(await _services.UserService.GetByIdAsync(id));

    [HttpGet("{userId}/permisos-ui")]
    public async Task<IActionResult> GetPermisosUIByUser(string userId)
    {
        var permisos = await _services.permisosService
            .GetPermisosUIByUser(userId);

        return Ok(permisos);
    }

    [HttpGet("{id}/permisos-edicion")]
    public async Task<IActionResult> GetPermisosEdicion(string id)
    {
        var result = await _services.UserService.GetPermisosEdicionAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] UserForRegistrationDto dto)
    {
        var result = await _services.UserService.CreateAsync(dto);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return CreatedAtAction(
            nameof(GetUser),
            new { id = result.Data!.Id },
            result.Data
        );

    }



    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, UserForUpdateDto dto)
        => Ok(await _services.UserService.UpdateAsync(id, dto));

    [HttpGet("{id}/permisos")]
    public async Task<IActionResult> GetPermisosUsuario(string id)
    {
        var user = await _services.UserService.GetByIdAsync(id);
        if (user == null) return NotFound();

        // Retornar solo los permisos extra del usuario (Source == "user")
        var permisosExtra = user.Permisos
            .Where(p => p.Source == "user")
            .Select(p => new
            {
                p.Id_Permiso,
                p.Nombre,
                p.Source
            });

        return Ok(permisosExtra);
    }

    [HttpPut("{id}/full")]
    public async Task<IActionResult> UpdateUserFull(
    string id,
    [FromBody] UserForUpdateFullDto dto)
    {
        await _services.UserService.UpdateFullAsync(id, dto);
        return NoContent();
    }

    [HttpGet("dashboard/permisos")]
    public async Task<IActionResult> GetUsuariosDashboard()
    {
        try
        {
            var usuarios = await _services.UserService.GetUsuariosConPermisosDashboardAsync();
            return Ok(usuarios);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Error al obtener usuarios para el dashboard",
                detail = ex.Message
            });
        }
    }
    // En tu UsersController
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("perfil")]
    public async Task<IActionResult> GetPerfil()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return NotFound("Usuario no encontrado");

        return Ok(new
        {
            id = user.Id,
            nombre = user.Nombre,
            apellido = user.Apellido,
            email = user.Email,
            idProceso = user.IdProceso,
           
        });
    }
}