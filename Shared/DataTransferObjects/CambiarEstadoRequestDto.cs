using Shared.DataTransferObjects.Documentos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects
{
    public class CambiarEstadoRequestDto
    {
        public int Estado { get; set; }
        public DocumentoForUpdateDto Documento { get; set; }
    }
}
