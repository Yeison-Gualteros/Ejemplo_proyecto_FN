using Entities.Models;
using Shared.DataTransferObjects.Documentos;
using Shared.RequestFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Interface
{
    public interface IDocumentoVersionRepository
    {
        Task<DocumentoVersion> GetActual(int Id_Documento);
        Task<IEnumerable<DocumentoVersion>> GetByDocumentoId(int idDocumento, bool trackChanges);
        Task<DocumentoVersion> GetVersionById(int documentoId, int versionId);

        void Create(DocumentoVersion version);
        Task DesactivarVersionesActuales(int idDocumento);
        
    }
}
