using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.RequestFeatures
{
    public class DocumentoParameters : RequestParameters
    {
        public DocumentoParameters()
        {
            Orden = "fecha";
            PageNumber = 1;
            PageSize = 10;
        }

        // búsqueda
        public string? Busqueda { get; set; }

        // filtros
        public string? TipoDocumento { get; set; }
        public string? Proceso { get; set; }
        public string? Usuario { get; set; }
        public string? Estado { get; set; }
        public string? Etiquetado { get; set; } 

        // fechas
        public DateTime? MinFecha { get; set; }
        public DateTime? MaxFecha { get; set; } = DateTime.MaxValue;

        public string Orden { get; set; } = "fecha";
        public string Direccion { get; set; } = "desc";

        public bool ValidFechaRango =>
            !MinFecha.HasValue || !MaxFecha.HasValue || MinFecha <= MaxFecha;
    }
}
