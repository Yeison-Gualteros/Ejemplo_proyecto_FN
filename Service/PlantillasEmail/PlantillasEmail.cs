using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.PlantillasEmail
{
    public class PlantillasEmail
    {
        // Convertir a static para que coincida con cómo los llamas
        public static string DocumentoEnviadoRevision(string nombreDocumento, string consecutivo, string creador, string aprobador, string urlDocumento)
        {
            return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background: #f9fafb;'>
                <div style='background: #3b82f6; padding: 20px; border-radius: 10px 10px 0 0;'>
                    <h2 style='color: white; margin: 0;'>Documento enviado a revisión</h2>
                </div>
                <div style='background: white; padding: 20px; border-radius: 0 0 10px 10px; border: 1px solid #e5e7eb;'>
                    <p>Hola <strong>{aprobador}</strong>,</p>
                    <p>El documento <strong>{nombreDocumento}</strong> ({consecutivo}) ha sido enviado para tu revisión por <strong>{creador}</strong>.</p>
                    
                    <div style='background: #eff6ff; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Documento:</strong> {nombreDocumento}</p>
                        <p style='margin: 5px 0;'><strong>Consecutivo:</strong> {consecutivo}</p>
                        <p style='margin: 5px 0;'><strong>Creador:</strong> {creador}</p>
                    </div>
                    
                    <p style='margin-top: 20px; color: #6b7280; font-size: 12px;'>
                        Este es un mensaje automático del Sistema de Gestión Documental FileNova.
                    </p>
                </div>
            </div>";
        }

        public static string DocumentoAprobado(string nombreDocumento, string consecutivo, string creador, string aprobador, string urlDocumento)
        {
            return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background: #f9fafb;'>
                <div style='background: #10b981; padding: 20px; border-radius: 10px 10px 0 0;'>
                    <h2 style='color: white; margin: 0;'>Documento Aprobado</h2>
                </div>
                <div style='background: white; padding: 20px; border-radius: 0 0 10px 10px; border: 1px solid #e5e7eb;'>
                    
                    
                    <div style='background: #ecfdf5; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0;'>
                            <p>Hola <strong>{creador}</strong>,</p>
                            <p>Tu documento <strong>{nombreDocumento}</strong> ({consecutivo}) ha sido <span style='color: #10b981; font-weight: bold;'>APROBADO</span> por <strong>{aprobador}</strong>.</p>
                        </p>
                    </div>
                    
                    <p style='margin-top: 20px; color: #6b7280; font-size: 12px;'>
                        Este es un mensaje automático del Sistema de Gestión Documental FileNova.
                    </p>
                </div>
            </div>";
        }

        public static string DocumentoRechazado(string nombreDocumento, string consecutivo, string creador, string aprobador, string comentario, string urlDocumento)
        {
            return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background: #f9fafb;'>
                <div style='background: #ef4444; padding: 20px; border-radius: 10px 10px 0 0;'>
                    <h2 style='color: white; margin: 0;'>Documento Rechazado</h2>
                </div>
                <div style='background: white; padding: 20px; border-radius: 0 0 10px 10px; border: 1px solid #e5e7eb;'>
                    <p>Hola <strong>{creador}</strong>,</p>
                    <p>Tu documento <strong>{nombreDocumento}</strong> ({consecutivo}) ha sido <span style='color: #ef4444; font-weight: bold;'>RECHAZADO</span> por <strong>{aprobador}</strong>.</p>
                    
                    {(string.IsNullOrEmpty(comentario) ? "" : $@"
                    <div style='background: #fef2f2; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ef4444;'>
                        <p style='margin: 0;'><strong>Motivo del rechazo:</strong></p>
                        <p style='margin: 10px 0 0 0;'>{comentario}</p>
                    </div>
                    ")}

                    <a href='{urlDocumento}' style='display: inline-block; padding: 12px 24px; background: #ef4444; color: white; text-decoration: none; border-radius: 8px; font-weight: bold;'>
                        Corregir documento
                    </a>
                    
                    <p style='margin-top: 20px; color: #6b7280; font-size: 12px;'>
                        Este es un mensaje automático del Sistema de Gestión Documental FileNova.
                    </p>
                </div>
            </div>";
        }

        public static string AlertaRevision(string nombreDocumento, string consecutivo, string diasSinRevision, string urlDocumento)
        {
            return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background: #f9fafb;'>
                <div style='background: #f59e0b; padding: 20px; border-radius: 10px 10px 0 0;'>
                    <h2 style='color: white; margin: 0;'>Alerta de Revisión Pendiente</h2>
                </div>
                <div style='background: white; padding: 20px; border-radius: 0 0 10px 10px; border: 1px solid #e5e7eb;'>
                    <p>El documento <strong>{nombreDocumento}</strong> ({consecutivo}) requiere revisión.</p>
                    
                    <div style='background: #fffbeb; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #f59e0b;'>
                        <p style='margin: 0;'><strong>Tiempo sin revisión:</strong> {diasSinRevision}</p>
                        <p style='margin: 5px 0 0 0;'>La norma establece revisión cada 2 años (730 días).</p>
                    </div>

                    <a href='{urlDocumento}' style='display: inline-block; padding: 12px 24px; background: #f59e0b; color: white; text-decoration: none; border-radius: 8px; font-weight: bold;'>
                        Revisar ahora
                    </a>
                    
                    <p style='margin-top: 20px; color: #6b7280; font-size: 12px;'>
                        Este es un mensaje automático del Sistema de Gestión Documental FileNova.
                    </p>
                </div>
            </div>";
        }

        // NUEVAS PLANTILLAS QUE FALTABAN

        public static string SolicitudRechazada(string nombreDocumento, string motivo, string creador, string revisor)
        {
            return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background: #f9fafb;'>
                <div style='background: #f97316; padding: 20px; border-radius: 10px 10px 0 0;'>
                    <h2 style='color: white; margin: 0;'>Solicitud No Aceptada</h2>
                </div>
                <div style='background: white; padding: 20px; border-radius: 0 0 10px 10px; border: 1px solid #e5e7eb;'>
                    <p>Hola <strong>{creador}</strong>,</p>
                    <p>Tu solicitud para el documento <strong>{nombreDocumento}</strong> no ha sido aceptada.</p>
                    
                    <div style='background: #fff7ed; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #f97316;'>
                        <p style='margin: 0;'><strong>Revisado por:</strong> {revisor}</p>
                        <p style='margin: 10px 0 0 0;'><strong>Motivo:</strong> {motivo}</p>
                    </div>

                    <p>Si considera necesario, puede presentar una nueva solicitud con los ajustes correspondientes.</p>
                    
                    <p style='margin-top: 20px; color: #6b7280; font-size: 12px;'>
                        Este es un mensaje automático del Sistema de Gestión Documental FileNova.
                    </p>
                </div>
            </div>";
        }

        public static string BorradorEnviadoGestionIntegral(string nombreDocumento, string consecutivo,
            string liderProceso, string proceso, string urlDocumento)
        {
            return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background: #f9fafb;'>
                <div style='background: #06b6d4; padding: 20px; border-radius: 10px 10px 0 0;'>
                    <h2 style='color: white; margin: 0;'>Borrador para Revisión</h2>
                </div>
                <div style='background: white; padding: 20px; border-radius: 0 0 10px 10px; border: 1px solid #e5e7eb;'>
                    <p>Hola <strong>Gestión Integral</strong>,</p>
                    <p>Se ha enviado un borrador de documento para su revisión.</p>
                    
                    <div style='background: #ecfeff; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Documento:</strong> {nombreDocumento}</p>
                        <p style='margin: 5px 0;'><strong>Consecutivo:</strong> {consecutivo}</p>
                        <p style='margin: 5px 0;'><strong>Enviado por:</strong> {liderProceso}</p>
                        <p style='margin: 5px 0;'><strong>Proceso:</strong> {proceso}</p>
                    </div>

                    <a href='{urlDocumento}' style='display: inline-block; padding: 12px 24px; background: #06b6d4; color: white; text-decoration: none; border-radius: 8px; font-weight: bold;'>
                        Revisar Borrador
                    </a>
                    
                    <p style='margin-top: 20px; color: #6b7280; font-size: 12px;'>
                        Este es un mensaje automático del Sistema de Gestión Documental FileNova.
                    </p>
                </div>
            </div>";
        }

        public static string ObservacionesBorrador(string nombreDocumento, string consecutivo,
            string observaciones, string revisor, string urlDocumento)
        {
            return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background: #f9fafb;'>
                <div style='background: #eab308; padding: 20px; border-radius: 10px 10px 0 0;'>
                    <h2 style='color: white; margin: 0;'>Observaciones al Documento</h2>
                </div>
                <div style='background: white; padding: 20px; border-radius: 0 0 10px 10px; border: 1px solid #e5e7eb;'>
                    <p>El documento <strong>{nombreDocumento}</strong> ({consecutivo}) requiere ajustes.</p>
                    
                    <div style='background: #fefce8; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #eab308;'>
                        <p style='margin: 0;'><strong>Revisor:</strong> {revisor}</p>
                        <p style='margin: 10px 0 0 0;'><strong>Observaciones:</strong></p>
                        <p style='margin: 10px 0 0 0;'>{observaciones}</p>
                    </div>

                    <a href='{urlDocumento}' style='display: inline-block; padding: 12px 24px; background: #eab308; color: white; text-decoration: none; border-radius: 8px; font-weight: bold;'>
                        Realizar Ajustes
                    </a>
                    
                    <p style='margin-top: 20px; color: #6b7280; font-size: 12px;'>
                        Este es un mensaje automático del Sistema de Gestión Documental FileNova.
                    </p>
                </div>
            </div>";
        }

        public static string DocumentoCodificado(string nombreDocumento, string consecutivo,
            string codigo, string urlDocumento)
        {
            return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background: #f9fafb;'>
                <div style='background: #22c55e; padding: 20px; border-radius: 10px 10px 0 0;'>
                    <h2 style='color: white; margin: 0;'>Documento Aprobado y Codificado</h2>
                </div>
                <div style='background: white; padding: 20px; border-radius: 0 0 10px 10px; border: 1px solid #e5e7eb;'>
                    <p>El documento ha sido aprobado y codificado según la norma fundamental.</p>
                    
                    <div style='background: #f0fdf4; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Documento:</strong> {nombreDocumento}</p>
                        <p style='margin: 5px 0;'><strong>Consecutivo</strong> {consecutivo}</p>
                        <p style='margin: 5px 0;'><strong>Fecha de Aprobación:</strong> {DateTime.Now:dd/MM/yyyy}</p>
                    </div>

                    <a href='{urlDocumento}' style='display: inline-block; padding: 12px 24px; background: #22c55e; color: white; text-decoration: none; border-radius: 8px; font-weight: bold;'>
                        Ver Documento
                    </a>
                    
                    <p style='margin-top: 20px; color: #6b7280; font-size: 12px;'>
                        Este es un mensaje automático del Sistema de Gestión Documental FileNova.
                    </p>
                </div>
            </div>";
        }

        public static string DivulgacionCambios(string nombreDocumento, string consecutivo,
            string tipoCambio, string descripcionCambio, string proceso, string urlDocumento)
        {
            return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background: #f9fafb;'>
                <div style='background: #6366f1; padding: 20px; border-radius: 10px 10px 0 0;'>
                    <h2 style='color: white; margin: 0;'>Cambios del documento</h2>
                </div>
                <div style='background: white; padding: 20px; border-radius: 0 0 10px 10px; border: 1px solid #e5e7eb;'>
                    <p>Se informa sobre cambios en la documentación del sistema de gestión.</p>
                    
                    <div style='background: #eef2ff; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Documento:</strong> {nombreDocumento}</p>
                        <p style='margin: 5px 0;'><strong>Consecutivo:</strong> {consecutivo}</p>
                        <p style='margin: 5px 0;'><strong>Tipo de Cambio:</strong> {tipoCambio}</p>
                        <p style='margin: 5px 0;'><strong>Proceso:</strong> {proceso}</p>
                        <p style='margin: 5px 0;'><strong>Descripción:</strong> {descripcionCambio}</p>
                    </div>

                    <a href='{urlDocumento}' style='display: inline-block; padding: 12px 24px; background: #6366f1; color: white; text-decoration: none; border-radius: 8px; font-weight: bold;'>
                        Ver Documento Actualizado
                    </a>
                    
                    <p style='margin-top: 20px; color: #6b7280; font-size: 12px;'>
                        Este es un mensaje automático del Sistema de Gestión Documental FileNova.<br>
                        
                    </p>
                </div>
            </div>";
        }
    }
}