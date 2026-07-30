using Contracts.Interface;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Repository.Extensions;
using Shared.RequestFeatures;
using System.Data;

namespace Repository.Clases
{
    public class RoleRepository : RepositoryBase<Role>, IRoleRepository
    {
        public RoleRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }

        public async Task<PagedList<Role>> GetAllRoles(RoleParameters roleParameters, bool trackChanges)
        {
            var query = FindByCondition(r => true, trackChanges)
                .Include(r => r.Rol_Permisos) // Rol_Permiso
                    .ThenInclude(rp => rp.Permiso)
                .SearchRole(roleParameters.Busqueda)
                .SortRole($"{roleParameters.Orden} {roleParameters.Direccion}");

            var rolesList = await query.ToListAsync();

            return PagedList<Role>.ToPageList(
                rolesList,
                roleParameters.PageNumber,
                roleParameters.PageSize
            );
        }


        public async Task<IEnumerable<Role>> GetAllRolesAsync()
        {
            return await RepositoryContext.Roles
                .Include(r => r.Rol_Permisos)
                    .ThenInclude(rp => rp.Permiso)
                .ToListAsync();
        }

        public async Task<Role> GetByIdAsync(int roleId)
        {
            return await RepositoryContext.Roles
                .Include(r => r.Rol_Permisos)
                    .ThenInclude(rp => rp.Permiso)
                .FirstOrDefaultAsync(r => r.Id == roleId.ToString());
        }

        public async Task<Role> GetRoleById(string roleId, bool trackChanges) =>
            await FindByCondition(r => r.Id.Equals(roleId), trackChanges)
                  .SingleOrDefaultAsync();

        public async Task<Role> GetById(int roleId, bool trackChanges)
        {
            return await RepositoryContext.Roles
                .AsNoTracking() // Si trackChanges es false, usamos AsNoTracking()
                .FirstOrDefaultAsync(role => role.Id == roleId.ToString());
        }

        public void CreateRol(Role role) => Create(role);

        public void DeleteRol(Role role) => Delete(role);

    }
}