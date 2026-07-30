using Contracts.Interface;


namespace Service.Contracts
{
    public interface IServiceManager
    {
        IDocumentoService DocumentoService { get; }
        ISolicitudService SolicitudService { get; }
        ITrazabilidad_DocumentoService Trazabilidad_DocumentoService { get; }

        IAuthenticationService AuthenticationService { get; }
        IRoleService RoleService { get; }
        IPermisosService permisosService { get; }
        IUserService UserService { get; }
        ITipoDocumentoService TipoDocumentoService { get; }
        IProcesoService ProcesoService { get; }


    }
}
