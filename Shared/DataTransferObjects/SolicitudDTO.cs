using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects
{
    public class SolicitudDTO
    {
        public int Id_Solicitud { get; set; }
        public int Id_Usuario { get; set; }
        public DateTime Fecha_Solicitud { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string Detalles { get; set; } = string.Empty;
    }
}
