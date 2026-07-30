using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.Documentos
{
    public class DocumentoForUpdateDto
    {
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public string? Id_Usuario { get; set; }
        public int Estado { get; set; }
        public string? Tipo { get; set; }
        public string AprobadorId { get; set; }
        public string IdProceso { get; set; }
        public string Etiquetado { get; set; }
        public string? ContenidoData { get; set; }
        public string? Comentario { get; set; }
        public DateTime Fecha_Creacion { get; set; }
        public DateTime Fecha_Modificacion { get; set; }
        public DateTime? Fecha_Aprobacion { get; set; }
        public DateTime? Fecha_Revision { get; set; }
        public string idTipoDocumento { get; set; }
    }
}