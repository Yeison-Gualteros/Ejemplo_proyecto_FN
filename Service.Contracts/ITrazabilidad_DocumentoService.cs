using Entities.Models;
using Shared.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
    public interface ITrazabilidad_DocumentoService
    {
        Task CreateTrazabilidadDocumento(
            int idDocumento,
            string accion,
            string? comentario,
            string userId,
            string rol,
            string? rutaNueva = null,
            string? rutaAnterior = null,
            int? estadoAnterior = null,
            int? estadoNuevo = null,
            string? versionAnterior = null,
            string? versionNueva = null
        );
        Task<IEnumerable<Trazabilidad_DocumentoDTO>> GetAllTrazabilidad_DocumentoAsync(int idDocumento, string userId, bool trackChanges);

        Task<IEnumerable<Trazabilidad_Documento>> GetTrazabilidadesPorDocumento(int documentoId);


    }
}
