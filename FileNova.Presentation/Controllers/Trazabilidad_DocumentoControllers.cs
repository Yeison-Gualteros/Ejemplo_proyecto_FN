using Entities.Models;
using FileNova.Presentation.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FileNova.Presentation.Controllers
{
    [ApiController]
    [Route("api/documentos")]
    public class Trazabilidad_DocumentoControllers : ControllerBase
    {
        private readonly IServiceManager _service;
        public Trazabilidad_DocumentoControllers(IServiceManager service)
        {
            _service = service;
        }

        [HttpGet("{id}/trazabilidades")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HasPermission("DOCUMENTOS_VER_HISTORIAL")]
        public async Task<IActionResult> GetTrazabilidad(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var trazabilidad = await _service.Trazabilidad_DocumentoService
                    .GetAllTrazabilidad_DocumentoAsync(id, userId, false);

                return Ok(trazabilidad);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = ex.Message,
                    detalle = ex.InnerException?.Message
                });
            }
        }
    }
}
