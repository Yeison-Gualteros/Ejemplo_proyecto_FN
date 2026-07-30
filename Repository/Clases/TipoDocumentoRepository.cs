using Contracts.Interface;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Shared.DataTransferObjects.TipoDocumentos;


namespace Repository.Clases
{
    public class TipoDocumentoRepository : RepositoryBase<TipoDocumento>, ITipoDocumentoRepository
    {
        public TipoDocumentoRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public async Task<IEnumerable<TipoDocumento>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
            .OrderBy(t => t.Nombre)
            .ToListAsync();

        public async Task<TipoDocumento?> GetByIdAsync(string id, bool trackChanges) =>
            await FindByCondition(td => td.IdTipoDocumento == id, trackChanges)
            .FirstOrDefaultAsync();

        public async Task<TipoDocumento?> GetNombreAsync(string nombre, bool trackChanges) =>
            await FindByCondition(td => td.Nombre.ToLower() == nombre.ToLower(), trackChanges)
            .FirstOrDefaultAsync();

        public async Task<TipoDocumento?> GetPrefijoAsync(string prefijo, bool trackChanges) =>
            await FindByCondition(td => td.Prefijo.ToLower() == prefijo.ToLower(), trackChanges)
            .FirstOrDefaultAsync();

        public void Create(TipoDocumentoForCreationDto tipoDocumento) => Create(tipoDocumento);
        public void Update(TipoDocumentoForUpdateDto tipoDocumento) => Update(tipoDocumento);
    }
}
