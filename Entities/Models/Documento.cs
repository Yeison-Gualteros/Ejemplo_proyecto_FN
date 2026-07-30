
using Entities.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Xml;


namespace Entities.Models
{
    public class Documento
    {
        [Key]
        public int Id_Documento { get; set; }

        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public string? Codigo { get; set; }

        public string? Tipo { get; set; }
        public int ConsecutivoNumero { get; set; }
        public string Consecutivo { get; set; }
        public string? ContenidoJson { get; set; }  // Almacena el cuerpo completo como JSON
        public string? FirmasAprobacionJson { get; set; }

        

        // versión activa
        public int? VersionActualId { get; set; }

        [ForeignKey("VersionActualId")]
        public DocumentoVersion? VersionActual { get; set; }

        //usuarios
        public string? Id_Usuario { get; set; }
        [JsonIgnore]
        public User? User { get; set; }
        
        //aprobadores
        public string? AprobadorId { get; set; }
        public User? Aprobador { get; set; }  
        public string? Etiquetado { get; set; }

        public DateTime? Fecha_Creacion { get; set; } = DateTime.Now;
        public DateTime? Fecha_Modificacion { get; set; } = DateTime.Now;

        public string IdTipoDocumento { get; set; }
        public DateTime? Fecha_Aprobacion { get; set; }
        public DateTime? Fecha_Revision { get; set; }
        [ ForeignKey(nameof(IdTipoDocumento))]
        public TipoDocumento TipoDocumento { get; set; }
        public string IdProceso { get; set; }

        [ForeignKey(nameof(IdProceso))]
        public Proceso Proceso { get; set; }

        public ICollection<DocumentoVersion>? Versiones { get; set; }

        public ICollection<Trazabilidad_Documento> Trazabilidad_Documentos { get; set; } = new List<Trazabilidad_Documento>();
        public NivelAccesoDocumento NivelAcceso { get; set; }
    }
}
