using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.Documentos
{
    public class AlertaDocumentoDTO
    {
        // Identificación
        public int Id_Documento { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Consecutivo { get; set; } = string.Empty;

        // Clasificación
        public string TipoDocumento { get; set; } = string.Empty;
        public string Proceso { get; set; } = string.Empty;
        public string IdProceso { get; set; } = string.Empty;
        public string IdTipoDocumento { get; set; } = string.Empty;

        // Fechas clave
        public DateTime? FechaCreacion { get; set; }
        public DateTime? FechaUltimaRevision { get; set; }
        public DateTime? FechaAprobacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        // Métricas de alerta
        public int DiasSinRevision { get; set; }
        public string TipoAlerta { get; set; } = string.Empty;
        public string Urgencia { get; set; } = string.Empty;

        // Versión
        public string VersionActual { get; set; } = string.Empty;
        public int Estado { get; set; }
        public string NombreEstado { get; set; } = string.Empty;

        // Responsables
        public string Creador { get; set; } = string.Empty;
        public string Aprobador { get; set; } = string.Empty;

        //Rutas
        public string RutaPdf { get; set; } = string.Empty;
        public string RutaWord { get; set; } = string.Empty;
        public int IdVersion { get; set; }

        // ===== PROPIEDADES CALCULADAS PARA EL FRONTEND =====

        //Texto legible del tiempo sin revisión
        public string TiempoSinRevision => DiasSinRevision switch
        {
            > 1095 => $"{DiasSinRevision / 365} años",
            > 730 => $"Vencido por {DiasSinRevision - 730} días",
            > 365 => $"1 año y {(DiasSinRevision - 365) / 30} meses",
            > 30 => $"{DiasSinRevision / 30} meses",
            _ => $"{DiasSinRevision} días"
        };

        //Icono según nivel de urgencia
        public string IconoUrgencia => Urgencia switch
        {
            "Critica" => "",
            "Alta" => "",
            "Media" => "",
            _ => ""
        };

        //Clases CSS para el borde según urgencia
        public string ClaseColorUrgencia => Urgencia switch
        {
            "Critica" => "border-red-500 bg-red-50",
            "Alta" => "border-orange-400 bg-orange-50",
            "Media" => "border-yellow-400 bg-yellow-50",
            _ => "border-green-400 bg-green-50"
        };

        //Clases CSS para el badge de urgencia
        public string ClaseBadgeUrgencia => Urgencia switch
        {
            "Critica" => "bg-red-100 text-red-700 border-red-300",
            "Alta" => "bg-orange-100 text-orange-700 border-orange-300",
            "Media" => "bg-yellow-100 text-yellow-700 border-yellow-300",
            _ => "bg-green-100 text-green-700 border-green-300"
        };

        //Indica si la alerta es crítica (más de 3 años)
        public bool EsCritica => DiasSinRevision > 1095;

        //Indica si la alerta es urgente (más de 2 años)
        public bool EsUrgente => DiasSinRevision > 730;

        //Indica si está próximo a vencer (entre 700 y 730 días)
        public bool EstaProximoAVencer => DiasSinRevision >= 700 && DiasSinRevision <= 730;
    }

}
