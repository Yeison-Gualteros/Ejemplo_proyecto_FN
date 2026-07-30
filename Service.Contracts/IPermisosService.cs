using Shared.DataTransferObjects.Permisos;

namespace Service.Contracts
{
    public interface IPermisosService
    {
        // CONSULTAS
        Task<IEnumerable<PermisosDto>> GetAllPermisos(int? id_Permiso, bool trackChanges );

        Task<IEnumerable<PermisosDto>> GetUserPermisos(string userId, bool trackChanges );

        Task<IEnumerable<PermisosDto>> GetPermissionsByRole( string roleId);
        Task<IEnumerable<PermisosDto>>  GetPermissionsByUser(string id);
        // CRUD
        Task<PermisosDto> CreatePermiso(PermisosForCreationDto permiso);

        Task<PermisosDto> GetPermisoById( int id_Permiso, bool trackChanges);

        Task<PermisosDto> UpdatePermiso( int id_Permiso, PermisoForUpdateDto permisoForUpdate);

        Task<bool> DeletePermiso( int id_Permiso);

        // ASIGNACIÓN ROL
        Task AddPermissionsToRole(string roleId, List<int> permisosIds);

        Task RemovePermissionFromRole( string roleId, int permisoId );
        Task UpdatePermissionsOfRole(string roleId, List<int> permisosIds);

        Task<IEnumerable<PermisosDto>> GetPermisosUIByRole(string roleId);
        Task<IEnumerable<PermisosDto>> GetPermisosUIByUser(string userId);

        Task SaveUserPermisos(string userId, List<int> permisosIds);

        Task RemovePermissionFromUser(string userId, int permisoId);

        //validar permisos dinamicamente
        Task<bool> UsuarioTienePermiso(string userId, string permiso);

        Task<HashSet<string>> ObtenerPermisosUsuario(string userId);

    }
}