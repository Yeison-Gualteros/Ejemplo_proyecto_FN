using Contracts.Interface;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Shared.DataTransferObjects.Procesos;

namespace Repository.Clases
{
    public class ProcesoRepository : RepositoryBase<Proceso>, IProcesoRepository
    {
        public ProcesoRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }

        public async Task<IEnumerable<Proceso>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

        public async Task<Proceso?> GetByIdAsync(string idProceso, bool trackChanges) =>
            await FindByCondition(p => p.IdProceso == idProceso, trackChanges)
                .FirstOrDefaultAsync();

        public async Task<Proceso?> GetNombreAsync(string nombre, bool trackChanges) =>
            await FindByCondition(p => p.Nombre.ToLower() == nombre.ToLower(), trackChanges)
                .FirstOrDefaultAsync();

        public async Task<Proceso?> GetPrefijoAsync(string prefijo, bool trackChanges) =>
            await FindByCondition(p => p.Prefijo.ToLower() == prefijo.ToLower(), trackChanges)
                .FirstOrDefaultAsync();

        public void Create(ProcesoForCreateDto proceso) => Create(proceso);

        public void Update(ProcesoForUpdateDto proceso) => Update(proceso);

        public void Delete(Proceso proceso) => base.Delete(proceso);
    }
}