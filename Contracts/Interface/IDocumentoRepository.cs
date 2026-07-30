using Entities.Models;
using Shared.DataTransferObjects.Documentos;
using Shared.RequestFeatures;
using Microsoft.AspNetCore.Http;
using Entities.Enums;

namespace Contracts.Interface
{
    public interface IDocumentoRepository
    {
        Task<PagedList<Documento>> GetAllDocumentos(DocumentoParameters documentoParameters, bool trackChanges);
        Task<Documento> GetDocumento(int Id_Documento, string userId, bool trackChanges);
        Task<IEnumerable<Documento>> GetDocumentoByIds(IEnumerable<int> Id_Documentos, bool trackChanges);
        
        void CreateDocumento(Documento documento);
        void DeleteDocumento(Documento documento);
        Task<PagedList<DocumentoDTO>> GetDocumentosFiltrados(DocumentoParameters documentoParameters, string userId, bool verTodos, bool puedeAprobar, bool puedeArchivar, List<int> nivelesAcceso, bool trackChanges);
        Task<Documento> GetByIdAsync(int id, bool trackChanges);
        Task<DocumentoVersion> GetVersionById(int documentoId, int versionId);
        Task<int> GetConsecutivo();
        Task<PagedList<ListadoDocumentoDto>> GetListadoMaestro(DocumentoParameters parametros, List<int> nivelAcceso, bool esConsultor, string userID, bool esGestorDocumental, bool trackChanges);

        Task<Documento?> GetUltimoConsecutivoPorPrefijo(string idTipoDocumento, string idProceso);
        Task<List<Documento>> GetActivosConConsecutivoMayor(string idTipoDocumento, string idProceso, int consecutivoMinimo, bool trackChanges);

        // Obtiene documentos que NUNCA han sido revisados y tienen más de 2 años de creados
        Task<List<Documento>> GetDocumentosNuncaRevisadosAsync(DateTime fechaLimite, bool trackChanges);

        // Obtiene documentos con revisión vencida (más de 2 años desde última revisión)
        Task<List<Documento>> GetDocumentosRevisionVencidaAsync(DateTime fechaLimite, bool trackChanges);

        // Obtiene documentos aprobados que requieren revisión
        Task<List<Documento>> GetDocumentosAprobadosSinRevisionAsync(DateTime fechaLimite, bool trackChanges);

    }
}
