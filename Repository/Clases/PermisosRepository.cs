using Contracts.Interface;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Clases
{
    public class PermisosRepository : RepositoryBase<Permiso>, IPermisosRepository 
    {
        private readonly RepositoryContext _context;
        public PermisosRepository(RepositoryContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Permiso>> GetPermisosPorRoleId(string roleId, bool trackChanges)
        {
            return await _context.Rol_Permisos
                .Where(rp => rp.Id_Rol == roleId)
                .Include(rp => rp.Permiso)
                .Select(rp => rp.Permiso)
                .ToListAsync();
        }

        public async Task<IEnumerable<Permiso>> GetPermisosByIds(List<int> permisosIds)
        {
            return await _context.Permisos
                                 .Where(p => permisosIds.Contains(p.Id_Permiso))
                                 .ToListAsync();
        }

        public async Task<IEnumerable<Permiso>> GetAllPermisos(int? id_Permisos, bool trackChanges)
        {
            if (id_Permisos.HasValue)
            {
                return await FindByCondition(
                        p => p.Id_Permiso == id_Permisos.Value,
                        trackChanges)
                    .ToListAsync();
            }

            return await FindAll(trackChanges)
                .ToListAsync();
        }

        public async Task<IEnumerable<Permiso>> GetUserPermisos(string userId, bool trackChanges)
        {
            return await _context.user_Permisos
                .Where(up => up.UserId == userId)
                .Include(up => up.Permiso)
                .Select(up => up.Permiso)
                .ToListAsync();
        }

        public async Task<List<string>> GetAllPermisosNombres()
        {
            return await _context.Permisos
                .Select(p => p.Nombre)
                .ToListAsync();
        }


        public async Task<Permiso> CreatePermiso(Permiso permiso)
        {
            await _context.Permisos.AddAsync(permiso);
            await _context.SaveChangesAsync();
            return permiso;
        }

        public async Task<Permiso?> GetPermisoById(int id, bool trackChanges)
        {
            return await FindByCondition(
                p => p.Id_Permiso == id,
                trackChanges)
                .FirstOrDefaultAsync();
        }

        public void DeletePermiso(Permiso permiso)
        {
            Delete(permiso);
        }

    }
}
