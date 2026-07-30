using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.TipoDocumentos
{
    public class TipoDocumentoForUpdateDto
    {
        public string? Nombre { get; set; }
        public string? Prefijo { get; set; }
        public bool Estado { get; set; }
    }
}
