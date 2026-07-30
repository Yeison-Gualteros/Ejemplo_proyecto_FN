using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.Documentos
{
    public class DocumentoVersionDTO
    {
        public int Id_Version { get; set; }

        public string? Ruta { get; set; }

        public string NumeroVersion { get; set; }

        public int Estado { get; set; }

        public bool EsActual { get; set; }
    }
}
