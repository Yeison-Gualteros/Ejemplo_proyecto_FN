using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Models
{
    public class Solicitud
    {
        [Key]
        [Column("id_solicitud")]
        public int Id_Solicitud { get; set; }
        public string? Nombre_Solicitud { get; set; }
        public DateTime Fecha_Solicitud { get; set; }
        public string? Estado { get; set; }
        public string? Detalles { get; set; }
        
        [ForeignKey("User")]
        [Column("id_usuario")]
        public string? Id_Usuario { get; set; }
        public User? User { get; set; }
        public string? Tipo_Solicitud { get; set; }
    }
}
