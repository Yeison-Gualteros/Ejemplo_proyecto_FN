using System;
using System.Linq.Expressions;
using Entities.Models;
using System.Linq;

namespace Contracts
{
    public interface IRol_PermisosRepository
    {
        Task<bool> ExistsAsync(string roleId, int permisoId);
        Task<IEnumerable<Rol_Permiso>> GetPermisosByRole(Guid roleId);
        Task<Rol_Permiso?> GetRolePermiso(Guid roleId, int permisoId);
        void Create(Rol_Permiso entity);
        void Delete(Rol_Permiso entity);
        IQueryable<Rol_Permiso> FindByCondition(Expression<Func<Rol_Permiso, bool>> predicate, bool trackChanges);
    }
}
