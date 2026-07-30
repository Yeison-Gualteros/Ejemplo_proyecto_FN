using Entities.Models;
using Shared.DataTransferObjects.Documentos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Interface
{
    public interface IProcesoRepository
    {
        Task<IEnumerable<Proceso>> GetAllAsync(bool trackChanges);
        Task<Proceso?> GetByIdAsync(string idProceso, bool trackChanges);
        Task<Proceso?> GetNombreAsync(string nombre, bool trackChanges);
        Task<Proceso?> GetPrefijoAsync(string prefijo, bool trackChanges);

        void Create(Proceso proceso);
        void Update(Proceso proceso);
        void Delete(Proceso proceso);
    }
}
