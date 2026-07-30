using Contracts;
using Contracts.Interface;
using Entities.Models;
using Service.Contracts;
using Shared.DataTransferObjects.TipoDocumentos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class TipoDocumentoService : ITipoDocumentoService
    {
        private readonly IRepositoryManager _repository;

        public TipoDocumentoService(IRepositoryManager repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<TipoDocumento>> GetAllAsync()
        {
            return await _repository.TipoDocumento.GetAllAsync(trackChanges : false);
        }

        public async Task<TipoDocumento?> GetByIdAsync(string id)
        {
            var tipo = await _repository.TipoDocumento.GetByIdAsync(id, false);

            if(tipo is null)
                throw new Exception($"No se encontró el tipo de documento con id {id}");

            return tipo;
        }

        public async Task<TipoDocumento> CreateAsync(TipoDocumentoForCreationDto dto)
        {

            if (dto == null)
                throw new Exception("DTO null");

            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new Exception("Nombre es obligatorio");

            if (string.IsNullOrWhiteSpace(dto.Prefijo))
                throw new Exception("Prefijo es obligatorio");

            //Validar Nombre
            var existeNombre = await _repository.TipoDocumento.GetNombreAsync(dto.Nombre.Trim(), false);
            if (existeNombre is not null)
                throw new Exception($"Ya existe un tipo de documento con el nombre {dto.Nombre}");

            //Validar Prefijo
            var existePrefijo = await _repository.TipoDocumento.GetPrefijoAsync(dto.Prefijo.Trim(), false);
            if (existePrefijo is not null)
                throw new Exception($"Ya existe un tipo de documento con el prefijo {dto.Prefijo}");

           

            var tipoDocumento = new TipoDocumento
            {
                IdTipoDocumento = Guid.NewGuid().ToString(),
                Nombre = dto.Nombre,
                Prefijo = dto.Prefijo,
                Estado = true
            };

            _repository.TipoDocumento.Create(tipoDocumento);
            await _repository.SaveAsync();

            return tipoDocumento;
        }

        public async Task UpdateAsync(string id, TipoDocumento tipoDocumento)
        {
            var tipo = await _repository.TipoDocumento.GetByIdAsync(id, true);
            if (tipo is null)
                throw new Exception($"No se encontró el tipo de documento con id {id}");

            //validar Nombre Unico
            var existeNombre = await _repository.TipoDocumento.GetNombreAsync(tipoDocumento.Nombre.Trim(), false);
            if (existeNombre is not null && existeNombre.IdTipoDocumento != id)
                throw new Exception($"Ya existe un tipo de documento con el nombre {tipoDocumento.Nombre}");

            //no se cambia el prefijo si ya hay documentos asociados
            if (tipo.Prefijo != tipoDocumento.Prefijo)
            {
                var tieneDocumentos = tipo.Documentos.Any();
                if (tieneDocumentos)
                    throw new Exception($"No se puede cambiar el prefijo porque hay documentos asociados al tipo de documento {tipo.Nombre}");

                tipo.Prefijo= tipoDocumento.Prefijo.Trim().ToUpper();

            }

            tipo.Nombre = tipoDocumento.Nombre.Trim();

            _repository.TipoDocumento.Update(tipo);
            await _repository.SaveAsync();
        }

        public async Task DesactivarAsync(string id)
        {
            var tipo = await _repository.TipoDocumento.GetByIdAsync(id, true);
            if (tipo is null)
                throw new Exception($"No se encontró el tipo de documento con id {id}");

            tipo.Estado = false;

            _repository.TipoDocumento.Update(tipo);
            await _repository.SaveAsync();
        }
    }
}
