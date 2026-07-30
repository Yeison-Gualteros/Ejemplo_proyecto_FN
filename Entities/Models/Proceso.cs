using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Models
{
    public class Proceso
    {
        [Key]
        public string IdProceso { get; set; }

        public string? Nombre { get; set; }
        public string? Prefijo { get; set; }
        public bool Estado { get; set; }

        public ICollection<Documento> documentos { get; set; } = new List<Documento>();
        public ICollection<User> Usuarios { get; set; } = new List<User>();
    }
}
