using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.Documentos
{
    public class ListadoDocumentoDto
    {
        public int Id { get; set; }
        public string Consecutivo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; } 
        public string TipoDocumento { get; set; }
        public string Proceso { get; set; }

        public DateTime? FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        public string RutaArchivo { get; set; }

        public VersionActualDto VersionActual { get; set; }

        public DateTime? UltimaModificacion { get; set; }

        public string Usuario { get; set; }
        public string UsuarioSubio { get; set; } 
        public string ApellidoUsuario { get; set; } 

        // APROBADOR
        public string AprobadorNombre { get; set; }
        public string AprobadorApellido { get; set; }
        public string AprobadorId { get; set; } 

        // ETIQUETADO Y ESTADO
        public string Etiquetado { get; set; }
        public int NivelAcceso { get; set; }
        public int Estado { get; set; } 

        public string IdTipoDocumento { get; set; } 
        public string IdProceso { get; set; } 

        public DateTime? FechaAprobacion { get; set; } 
        public DateTime? FechaRevision { get; set; }
        public string? IdCreador { get; set; }      // ID del usuario que creó el documento
        public bool EsCreador { get; set; }          // Si el usuario actual es el creador
        public bool PerteneceAlProceso { get; set; } // Si el usuario pertenece al mismo proceso

        public string? ContenidoJson { get; set; }

        public ContenidoDocumentoDto? Contenido
        {
            get
            {
                if (string.IsNullOrEmpty(ContenidoJson))
                    return null;

                try
                {
                    return JsonSerializer.Deserialize<ContenidoDocumentoDto>(
                        ContenidoJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}