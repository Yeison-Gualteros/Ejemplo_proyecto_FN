using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.Documentos
{
    public class DocumentoForCreationDto
    {
        public string? Id_Usuario { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public string IdTipoDocumento { get; set; }
        public string AprobadorId { get; set; }
        public string IdProceso { get; set; }
        public string Etiquetado { get; set; }
        public int NivelAcceso { get; set; }
        public float Tamaño_KB { get; set; }
        public string? Ruta { get; set; }
        public DateTime Fecha_Subida { get; set; } 
         public string? Tipo { get; set; }
        public string? ContenidoData { get; set; }
        public string? NombreCreador { get; set;}
        public string? ApellidoCreador { get; set; }
        public string? Comentario { get; set; }
        

        public DateTime? Fecha_Revision { get; set; }

        public ContenidoDocumentoDto? Contenido { get; set; }

    }
}
