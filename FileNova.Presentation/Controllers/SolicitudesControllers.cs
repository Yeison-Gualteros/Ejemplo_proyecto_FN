using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileNova.Presentation.Controllers
{
    [ApiController]
    [Route("api/solicitudes")]
    public class SolicitudesControllers : ControllerBase
    {
        private readonly IServiceManager _service;
        public SolicitudesControllers(IServiceManager service)
        {
            _service = service;
        }

        // GET: /api/solicitudes
        [HttpGet]
        public IActionResult GetAllSolicitudes()
        {
            var solicitudes = _service.SolicitudService.GetAllSolicitudes(trackChanges: false);
            return Ok(solicitudes);
        }

        // GET: /api/solicitudes/5
        [HttpGet("{Id_Solicitud:int}")]
        public IActionResult GetSolicitudById(int Id_Solicitud)
        {
            var solicitud = _service.SolicitudService.GetSolicitudById(Id_Solicitud, trackChanges: false);
            if (solicitud == null)
                return NotFound($"No se encontró la solicitud con Id {Id_Solicitud}");
            return Ok(solicitud);
        }

        // GET: /api/solicitudes/usuario/3
        [HttpGet("usuario/{Id_Usuario:int}")]
        public IActionResult GetSolicitudesByUsuario(int Id_Usuario)
        {
            var solicitudes = _service.SolicitudService.GetSolicitudes(Id_Usuario, trackChanges: false);
            return Ok(solicitudes);
        }

        // POST: /api/solicitudes/usuario/3
        [HttpPost("usuario/{Id_Usuario:int}")]
        public IActionResult CreateSolicitudForUsuario(int Id_Usuario, [FromBody] Shared.DataTransferObjects.SolicitudForCreationDto solicitud)
        {
            if (solicitud is null)
                return BadRequest("SolicitudForCreationDto object is null");
            var solicitudToReturn = _service.SolicitudService.CreateSolicitud(Id_Usuario, solicitud, trackChanges: false);
            return CreatedAtRoute("GetSolicitudById", new { Id_Solicitud = solicitudToReturn.Id_Solicitud }, solicitudToReturn);
        }
    }
}
