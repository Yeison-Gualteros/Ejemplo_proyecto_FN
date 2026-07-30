using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.Documentos
{
    public class ControlCambioDto
    {
        public string Version { get; set; }
        public string Fecha { get; set; }
        public string Usuario { get; set; }
        public string Descripcion { get; set; }
    }
}
