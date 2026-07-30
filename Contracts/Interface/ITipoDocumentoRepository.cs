using Entities.Models;
using Shared.DataTransferObjects.Documentos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Interface
{
    public interface ITipoDocumentoRepository
    {
        Task<IEnumerable<TipoDocumento>> GetAllAsync(bool trackChanges);
        Task<TipoDocumento?> GetByIdAsync(string id, bool trackChanges);
        Task<TipoDocumento?> GetNombreAsync(string nombre, bool trackChanges);
        Task<TipoDocumento?> GetPrefijoAsync(string prefijo, bool trackChanges);

        void Create(TipoDocumento tipoDocumento);
        void Update(TipoDocumento tipoDocumento);
            
    }
}
