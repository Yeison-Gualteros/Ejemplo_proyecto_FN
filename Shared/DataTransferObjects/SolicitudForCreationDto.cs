using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects
{
    public record class SolicitudForCreationDto
    {
        public string Nombre_Solicitud { get; init; } = string.Empty;
        public DateTime Fecha_Solicitud { get; init; }
        public string Estado { get; init; } = string.Empty;
        public string Detalles { get; init; } = string.Empty;
    }
}
