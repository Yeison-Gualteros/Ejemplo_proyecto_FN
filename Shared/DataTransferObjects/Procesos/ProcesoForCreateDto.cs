using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.Procesos
{
    public class ProcesoForCreateDto
    {
        public string? Nombre { get; set; }
        public string? Prefijo { get; set; }
        public bool Estado { get; set; }
    }
}
