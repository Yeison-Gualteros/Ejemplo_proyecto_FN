using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects
{
    public class Trazabilidad_DocumentoDTO
    {
        public int Id_Trazabilidad { get; set; }
        public string? Accion { get; set; }
        public DateTime Fecha_Cambio { get; set; }
        public int Id_Documento { get; set; }
        public string Id_Usuario { get; set; }
        public string? Comentario { get; set; }
        public string? NombreUsuario { get; set; }
        public string? ApellidoUsuario { get; set; }
        public string? Rol { get; set; }

        public string? RutaAnterior { get; set; }
        public string? RutaNueva { get; set; }
        public string? VersionAnterior { get; set; }
        public string? VersionNueva { get; set; }
        public int? EstadoAnterior { get; set; }
        public int? EstadoNuevo { get; set; }

    }
}
