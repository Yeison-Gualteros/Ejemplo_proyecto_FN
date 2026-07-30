using Entities.Models;
using FileNova.Presentation.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Shared.DataTransferObjects.TipoDocumentos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileNova.Presentation.Controllers
{
    [ApiController]
    [Route("api/tipodocumento")]
    public class TipoDocumentoController : ControllerBase
    {
        private readonly IServiceManager _service;

        public TipoDocumentoController(IServiceManager service)
        {
            _service = service;
        }

        //GET api/tipodocumento
        [HttpGet]
        [HasPermission("TipoDocumento_Ver")]
        public async Task<IActionResult> GetAll()
        {
            var tipos = await _service.TipoDocumentoService.GetAllAsync();
            return Ok(tipos);
        }

        //GET api/tipodocumento/5
        [HttpGet("{id}")]
        [HasPermission("TipoDocumento_Ver")]
        public async Task<IActionResult> GetById(string id)
        {
            var tipo = await _service.TipoDocumentoService.GetByIdAsync(id);
            return Ok(tipo);
        }

        //POST api/tipodocumento
        [HttpPost]
        [HasPermission("TipoDocumento_Crear")]
        public async Task<IActionResult> Create([FromBody] TipoDocumentoForCreationDto tipoDocumento)
        {
            if (tipoDocumento is null)
                return BadRequest("TipoDocumento object is null");

            var creado = await _service.TipoDocumentoService.CreateAsync(tipoDocumento);

            return CreatedAtAction(nameof(GetById), new { id = creado.IdTipoDocumento }, creado);

        }

        //PUT api/tipodocumento/5
        [HttpPut("{id}")]
        [HasPermission("TipoDocumento_Editar")]
        public async Task<IActionResult> Update(string id, [FromBody] TipoDocumento tipoDocumento)
        {
            if (tipoDocumento is null)
                return BadRequest("TipoDocumento object is null");
            await _service.TipoDocumentoService.UpdateAsync(id, tipoDocumento);
            return NoContent();
        }

        //PATCH api/tipodocumento/5/desactivar
        [HttpPatch("{id}/desactivar")]
        [HasPermission("TipoDocumento_Desactivar")]
        public async Task<IActionResult> Desactivar(string id)
        {
            await _service.TipoDocumentoService.DesactivarAsync(id);
            return NoContent();
        }
    }
}
