using System;
using System.Collections.Generic;

namespace Entities.Enums
{
    public static class DocumentoWorkflowPolicy
    {
        public static readonly Dictionary<(EstadoDocumento from, EstadoDocumento to), string> PermisosRequeridos =
            new()
            {
                // CREADOR
                {(EstadoDocumento.Borrador, EstadoDocumento.EnRevision), "DOCUMENTOS_ENVIAR_REVISION" },
                {(EstadoDocumento.Rechazado, EstadoDocumento.EnRevision), "DOCUMENTOS_ENVIAR_REVISION" },

                // APROBADOR - De EnRevision a Vigente DIRECTAMENTE
                {(EstadoDocumento.EnRevision, EstadoDocumento.Vigente), "DOCUMENTOS_APROBAR" },
                {(EstadoDocumento.EnRevision, EstadoDocumento.Rechazado), "DOCUMENTOS_APROBAR" },

                // GESTOR DOCUMENTAL
                {(EstadoDocumento.Vigente, EstadoDocumento.Archivado), "DOCUMENTOS_ARCHIVAR" }
            };

        public static bool TryGetPermiso(EstadoDocumento actual, EstadoDocumento nuevo, out string permiso)
        {
            return PermisosRequeridos.TryGetValue((actual, nuevo), out permiso!);
        }
    }
}