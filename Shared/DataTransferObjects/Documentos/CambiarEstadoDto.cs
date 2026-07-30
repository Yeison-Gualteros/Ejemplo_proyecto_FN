using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.Documentos
{
    public class CambiarEstadoDto
    {
        public int Estado { get; set; }
        public string Id_Usuario { get; set; } = string.Empty;
        public string? Comentario { get; set; }
        public DateTime Fecha_Aprobador { get; set; } = DateTime.Now;
    }
}
