using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Shared.RequestFeatures;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Interface
{
    public interface IRoleRepository
    {
        Task<Role> GetByIdAsync(int roleId);
        Task<IEnumerable<Role>> GetAllRolesAsync();
        Task<PagedList<Role>> GetAllRoles(RoleParameters roleParameters, bool trackChanges);
        Task<Role> GetRoleById(string roleId, bool trackChanges);
        Task<Role> GetById(int roleId, bool trackChanges);
        void CreateRol(Role role);
        void DeleteRol(Role role);
    }
}
