using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.Documentos
{
    public class AlertasRevisionDTO
    {
        //Total de documentos que requieren revisión
        public int TotalAlertas { get; set; }

        //Documentos con más de 2 años sin revisión (urgentes)
        public int DocumentosUrgentes { get; set; }

        //Documentos próximos a vencer (entre 700 y 730 días)
        public int DocumentosProximosAVencer { get; set; }

        //Documentos con revisión normal (menos de 700 días)
        public int DocumentosEnRegla { get; set; }

        //Lista detallada de todas las alertas
        public List<AlertaDocumentoDTO> Alertas { get; set; } = new();

        //Fecha límite usada para el cálculo (actual - 2 años)
        public DateTime FechaCorte { get; set; }

        //Fecha en que se generaron las alertas
        public DateTime FechaGeneracion { get; set; }

        //Resumen ejecutivo en texto
        public string ResumenEjecutivo { get; set; } = string.Empty;
    }
}
