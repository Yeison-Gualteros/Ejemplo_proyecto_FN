using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.Documentos
{
    public class DocumentoInformeDto
    {
        public string? Titulo { get; set; }
        public DateTime? Fecha1{ get; set; }
        public DateTime? Fecha2 { get; set; }

        public string? Resumen { get; set; }
        public string? Antecedentes { get; set; }
        public string? Falla { get; set; }
        public string? Revision { get; set; }
        public string? Solucion { get; set; }
        public string? RespuestaFabrica { get; set; }
        public string? Conclusiones { get; set; }
        public string? PlanAccion { get; set; }

        public DateTime Fecha_Creacion { get; set; }

        public DateTime Fecha_Modificacion { get; set; } = DateTime.Now;
        public DateTime? Fecha_Aprobacion { get; set; }
    }
}
