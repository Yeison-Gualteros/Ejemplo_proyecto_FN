using FileNova.Presentation.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Shared.DataTransferObjects.Procesos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileNova.Presentation.Controllers
{
    [ApiController]
    [Route("api/proceso")]
    public class ProcesoController : ControllerBase
    {
        private readonly IServiceManager _service;

        public ProcesoController(IServiceManager service)
        {
            _service = service;
        }

        //GET api/proceso
        [HttpGet]
        [HasPermission("PROCESO_VER")]
        public async Task<IActionResult> GetAll()
        {
            var procesos = await _service.ProcesoService.GetAllAsync();
            return Ok(procesos);
        }

        //GET api/proceso/5
        [HttpGet("{id}")]
        [HasPermission("PROCESO_VER")]
        public async Task<IActionResult> GetById(string id)
        {
            var proceso = await _service.ProcesoService.GetByIdAsync(id);
            return Ok(proceso);
        }

        //POST api/proceso
        [HttpPost]
        [HasPermission("PROCESO_CREAR")]
        public async Task<IActionResult> Create([FromBody] ProcesoForCreateDto proceso)
        {
            if (proceso is null)
                return BadRequest("Proceso object is null");

            var creado = await _service.ProcesoService.CreateAsync(proceso);
            return CreatedAtAction(nameof(GetById), new { id = creado.IdProceso }, creado);
        }

        //pUT api/proceso/5
        [HttpPut("{id}")]
        [HasPermission("PROCESO_EDITAR")]
        public async Task<IActionResult> Update(string id, [FromBody] ProcesoForUpdateDto proceso)
        {
            if (proceso is null)
                return BadRequest("Proceso object is null");
            await _service.ProcesoService.UpdateAsync(id, proceso);
            return NoContent();
        }

        //delete api/proceso/5
        [HttpDelete("{id}")]
        [HasPermission("PROCESO_ELIMINAR")]
        public async Task<IActionResult> Delete(string id)
        {
            await _service.ProcesoService.DeleteAsync(id);
            return NoContent();
        }
    }
}
