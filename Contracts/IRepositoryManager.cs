using Contracts.Interface;
using Microsoft.AspNetCore.Identity;

namespace Contracts
{
    public interface IRepositoryManager
    {
        IDocumentoRepository Documento { get; }
        ISolicitudRepository Solicitud { get; } 
        ITrazabilidad_DocumentoRepository trazabilidad_Documento { get; }
        IRoleRepository Role { get; }
        IPermisosRepository Permisos { get; }
        IUserRepository User { get; }
        IUserPermisosRepository UserPermisos { get; }
        IRol_PermisosRepository Rol_Permisos { get; }
        IDocumentoVersionRepository DocumentoVersion { get; }
        ITipoDocumentoRepository TipoDocumento { get; }
        IProcesoRepository Proceso { get; }



        Task SaveAsync();
    }
}
