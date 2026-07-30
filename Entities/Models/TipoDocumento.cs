using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Models
{
    public class TipoDocumento
    {
        [Key]
        public string IdTipoDocumento { get; set; }
        public string? Nombre { get; set; }
        public string? Prefijo { get; set; }
        public bool Estado { get; set; }
        public string? PlantillaPath { get; set; }

        public ICollection<Documento> Documentos { get; set; } = new List<Documento>();
    }
}
