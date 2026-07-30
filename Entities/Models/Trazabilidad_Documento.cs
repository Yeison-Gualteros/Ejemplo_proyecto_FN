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
    public class Trazabilidad_Documento
    {
        [Key]
        [Column("id_trazabilidad_documento")]
        public int Id_Trazabilidad { get; set; }

        public string? Accion { get; set; }
        public string? Comentario { get; set; }
        public DateTime Fecha_Cambio { get; set; }

        // Documento
        [ForeignKey("Documento")]
        [Column("id_documento")]
        public int Id_Documento { get; set; }
        [JsonIgnore]
        public Documento? Documento { get; set; }

        // Usuario
        [ForeignKey("User")]
        [Column("id_usuario")]
        public string? Id_Usuario { get; set; }
        public User? User { get; set; }

        public string? Rol { get; set; }

        

        public int? EstadoAnterior { get; set; }
        public int? EstadoNuevo { get; set; }

        public string? VersionAnterior { get; set; }
        public string? VersionNueva { get; set; }

        public string? RutaAnterior { get; set; }
        public string? RutaNueva { get; set; }

    }
}
