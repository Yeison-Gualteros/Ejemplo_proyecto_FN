using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Contracts.Interface
{
    public interface IUserPermisosRepository
    {
        Task<User_Permiso> GetByUserIdAndPermisoId(Guid userId, int permisoId);
        Task<IEnumerable<User_Permiso>> GetPermissionsForUser(string userId);
        Task Create(User_Permiso userPermiso);
        Task Delete(User_Permiso userPermiso);
        IQueryable<User_Permiso> FindByCondition(Expression<Func<User_Permiso, bool>> expression, bool trackChanges);
    }
}

