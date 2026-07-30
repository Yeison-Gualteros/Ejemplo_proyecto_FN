using Entities.Models;
using Shared.DataTransferObjects.Procesos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
    public interface IProcesoService
    {
        Task<IEnumerable<ProcesoDto>> GetAllAsync();
        Task<ProcesoDto?> GetByIdAsync(string id);
        Task<Proceso?> CreateAsync(ProcesoForCreateDto proceso);
        Task UpdateAsync(string id, ProcesoForUpdateDto proceso);
        Task DeleteAsync(string id);
    }
}
