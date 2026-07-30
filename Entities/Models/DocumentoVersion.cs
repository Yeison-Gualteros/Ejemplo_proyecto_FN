using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Entities.Models
{
    public class DocumentoVersion
    {
        [Key]
        public int Id_Version { get; set; }

        public int Id_Documento { get; set; }
        [JsonIgnore]
        public Documento Documento { get; set; }

        public string NumeroVersion { get; set; }

        public string? RutaPdf { get; set; }
        public string? RutaWord { get; set; }
        public string? Tipo { get; set; }
        public float Tamaño_KB { get; set; }

        public int Estado { get; set; }

        public bool EsActual { get; set; }

        public DateTime Fecha_Creacion { get; set; }


        public string Id_Usuario { get; set; }
        public User User { get; set; }

        public string? AprobadorId { get; set; }

        [ForeignKey("AprobadorId")]
        public User? Aprobador { get; set; }

        public DateTime? Fecha_Revision { get; set; } = DateTime.Now;

        public ICollection<Trazabilidad_Documento> Trazabilidades { get; set; }
    }
}
