using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.Documentos
{
    public class TablaAprovador
    {
        public string? NombreAprobador { get; set; }
        public string? ApellidoAprobador { get; set; }
        public string? NombreCreador { get; set; }
        public string? ApellidoCreador { get; set; }
        public DateTime FechaCreacionCreador { get; set; }
        public DateTime FechaAprobacion { get; set; }
    }
}
