using Entities.Models;
using Shared.DataTransferObjects.TipoDocumentos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
    public interface ITipoDocumentoService
    {
        Task<IEnumerable<TipoDocumento>> GetAllAsync();
        Task<TipoDocumento?> GetByIdAsync(string id);

        Task<TipoDocumento?> CreateAsync(TipoDocumentoForCreationDto tipoDocumento);
        Task UpdateAsync(string id, TipoDocumento tipoDocumento);
        Task DesactivarAsync(string id);
    }
}
