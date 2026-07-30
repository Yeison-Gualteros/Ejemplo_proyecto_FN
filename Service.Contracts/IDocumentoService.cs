using Entities.Models;
using Microsoft.AspNetCore.Http;
using Shared.DataTransferObjects.Documentos;
using Shared.RequestFeatures;
using System.Collections.Generic;

namespace Service.Contracts
{
    public interface IDocumentoService
    {
        //Obtener todos los documentos
        Task<(IEnumerable<DocumentoDTO> documentos, MetaData metaData)>GetAllDocumentos(DocumentoParameters documentoParameters, string userId, bool trackChanges);

        Task<DocumentoDTO> GetDocumento(int Id_Documento, string userId, bool trackChanges);

        //Obtener un documento por ID
        Task<IEnumerable<DocumentoDTO>> GetDocumentoByIds(IEnumerable<int> Id_Documento, bool trackChanges);

        Task CrearNuevaVersion(DocumentoForCreationDto documentoForCreationDto, int idDocumento, string userId, IFormFile archivo);
        //Crear un documento
        Task<DocumentoDTO> CreateDocumentoAsync(DocumentoForCreationDto dto, IFormFile archivo);


        //Eliminar un documento
        Task DeleteDocumento(int Id_Documento, string userId);

        //Guardar Documento
        Task<string> GuardarArchivoAsync(IFormFile archivo);


        //transiciones de flujo
        Task CambiarEstadoDocumento(int Id_Documento, int nuevoEstado, string Id_Usuario, DocumentoForUpdateDto dto, string? comentario);
        Task UpdateDocumento(int Id_Documento, string userId, DocumentoForUpdateDto dto, IFormFile? archivo, string comentario);

        Task RechazarDocumento(int Id_Documento, string comentario, string userId);

        Task<Documento> GetDocumentoParaDescargar(int id, string userId);
        Task<DocumentoVersion> GetVersionParaDescargar(int documentoId, int versionId, string userId);
        Task<byte[]> GenerarPdf(int id);
        Task<PagedList<ListadoDocumentoDto>> GetListadoMaestro(DocumentoParameters documentoParameters, List<string> permisos, bool esConsultor, string userId, bool puedeArchivar, bool trackChanges);

        Task<DocumentoVersion> GetVersionParaPreview(int documentoId, string userId);
        Task RegistrarRevisionAsync(int documentoId, string userId);
        // Obtiene documentos que necesitan revisión (más de 2 años sin revisar o nunca revisados)
        Task<AlertasRevisionDTO> GetAlertasRevision(string userId);

    }
}
