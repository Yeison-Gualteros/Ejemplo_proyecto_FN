using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.Documentos
{
    public class DocumentoDTO
    {
        public int Id_Documento { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public float Tamaño_KB { get; set; }

        public int Estado { get; set; }
        public string? Ruta { get; set; }

        public DateTime? Fecha_Creacion { get; set; }
        public DateTime? Fecha_Modificacion { get; set; }

        public string? Id_Usuario { get; set; }
        public string? AprobadorId { get; set; }

        public bool EsAprobador { get; set; }
        // USUARIO
        public string? UsuarioSubio { get; set; }
        public string? ApellidoUsuario { get; set; }
        public string? RolUsuario { get; set; }

        // APROBADOR
        public string? AprobadorNombre { get; set; }
        public string? AprobadorApellido { get; set; }
        public string? RolAprobador { get; set; }

        // NEGOCIO
        public string? IdProceso { get; set; }
        public string? Etiquetado { get; set; }
        public string? Tipo { get; set; }
        public string? IdTipoDocumento { get; set; }

        public string? ContenidoJson { get; set; }

        public DateTime? Fecha_Revision { get; set; }

        // VERSION
        public DocumentoVersionDTO? VersionActual { get; set; }

        
        public string? FirmasAprobacionJson { get; set; }
    }
}
