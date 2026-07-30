using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.Documentos
{
    public class ContenidoDocumentoDto
    {
        [JsonPropertyName("bloques")]
        public List<BloqueContenidoDto> Bloques { get; set; } = new();
    }
}
