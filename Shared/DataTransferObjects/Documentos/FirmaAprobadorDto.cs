using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.Documentos
{
    public class FirmaAprobadorDto
    {
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public string? Fecha_Aprobador { get; set; }
    }
}
