using Entities.Models;
using Shared.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Interface
{
    public interface IPermisosRepository
    {
        Task<IEnumerable<Permiso>> GetAllPermisos(int? id, bool trackChanges);
        Task<List<Permiso>> GetPermisosPorRoleId(string roleId, bool trackChanges);
        Task<IEnumerable<Permiso>> GetUserPermisos(string userId, bool trackChanges);
        Task<Permiso> GetPermisoById(int id, bool trackChanges);
        Task<List<string>> GetAllPermisosNombres();
        void Create(Permiso entity);
        void Delete(Permiso entity);



    }
}
