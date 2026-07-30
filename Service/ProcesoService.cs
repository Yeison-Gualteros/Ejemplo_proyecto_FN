using Contracts;
using Entities.Models;
using Service.Contracts;
using Shared.DataTransferObjects.Procesos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class ProcesoService : IProcesoService
    {
        private readonly IRepositoryManager _repository;

        public ProcesoService(IRepositoryManager repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ProcesoDto>> GetAllAsync()
        {
            var procesos = await _repository.Proceso.GetAllAsync(trackChanges: false);
            return procesos.Select(p => new ProcesoDto
            {
                IdProceso = p.IdProceso,
                Nombre = p.Nombre,
                Prefijo = p.Prefijo,
                Estado = p.Estado
                
            });
        }

        public async Task<ProcesoDto?> GetByIdAsync(string id)
        {
            var proceso = await _repository.Proceso.GetByIdAsync(id, false);
            if (proceso is null)
                throw new Exception($"No se encontró el proceso con id {id}");
            return new ProcesoDto
            {
                Nombre = proceso.Nombre,
                Prefijo = proceso.Prefijo,
                Estado = proceso.Estado
                
            };
        }

        public async Task<Proceso?> CreateAsync(ProcesoForCreateDto proceso)
        {
            if (proceso == null)
                throw new Exception("DTO null");

            if (string.IsNullOrWhiteSpace(proceso.Nombre))
                throw new Exception("Nombre es obligatorio");

            if (string.IsNullOrWhiteSpace(proceso.Prefijo))
                throw new Exception("Prefijo es obligatorio");

            //Validar Nombre
            var existeNombre = await _repository.Proceso.GetNombreAsync(proceso.Nombre.Trim(), false);
            if (existeNombre is not null)
                throw new Exception($"Ya existe un proceso con el nombre {proceso.Nombre}");

            //Validar Prefijo
            var existePrefijo = await _repository.Proceso.GetPrefijoAsync(proceso.Prefijo.Trim(), false);
            if (existePrefijo is not null)
                throw new Exception($"Ya existe un proceso con el prefijo {proceso.Prefijo}");

            var nuevoProceso = new Proceso
            {
                IdProceso = Guid.NewGuid().ToString(),
                Nombre = proceso.Nombre,
                Prefijo = proceso.Prefijo,
                Estado = true
            };
            _repository.Proceso.Create(nuevoProceso);
            await _repository.SaveAsync();
            return nuevoProceso;
        }

        public async Task UpdateAsync(string id, ProcesoForUpdateDto proceso)
        {
            if (proceso == null)
                throw new Exception("DTO null");

            if (string.IsNullOrWhiteSpace(proceso.Nombre))
                throw new Exception("Nombre es obligatorio");

            if (string.IsNullOrWhiteSpace(proceso.Prefijo))
                throw new Exception("Prefijo es obligatorio");

            var procesoExistente = await _repository.Proceso.GetByIdAsync(id, true);
            if (procesoExistente is null)
                throw new Exception($"No se encontró el proceso con id {id}");

            //Validar Nombre
            var existeNombre = await _repository.Proceso.GetNombreAsync(proceso.Nombre.Trim(), false);
            if (existeNombre is not null && existeNombre.IdProceso != id)
                throw new Exception($"Ya existe un proceso con el nombre {proceso.Nombre}");
            //Validar Prefijo
            var existePrefijo = await _repository.Proceso.GetPrefijoAsync(proceso.Prefijo.Trim(), false);
            if (existePrefijo is not null && existePrefijo.IdProceso != id)
                throw new Exception($"Ya existe un proceso con el prefijo {proceso.Prefijo}");

            procesoExistente.Nombre = proceso.Nombre;
            procesoExistente.Prefijo = proceso.Prefijo;
            procesoExistente.Estado = (bool)proceso.Estado;
            
            _repository.Proceso.Update(procesoExistente);
            await _repository.SaveAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var procesoExistente = await _repository.Proceso.GetByIdAsync(id, true);
            if (procesoExistente is null)
                throw new Exception($"No se encontró el proceso con id {id}");
            _repository.Proceso.Delete(procesoExistente);
            await _repository.SaveAsync();
        }
    }
}
