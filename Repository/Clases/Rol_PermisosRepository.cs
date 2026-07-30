using Contracts;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Repository
{
    public class Rol_PermisoRepository : IRol_PermisosRepository
    {
        private readonly RepositoryContext _context;

        public Rol_PermisoRepository(RepositoryContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsAsync(string roleId, int permisoId)
        {
            return await _context.Rol_Permisos
                .AnyAsync(rp => rp.Id_Rol == roleId && rp.Id_Permiso == permisoId);
        }


        public async Task<IEnumerable<Rol_Permiso>> GetPermisosByRole(Guid roleId)
        {
            return await _context.Rol_Permisos
                .Where(rp => rp.Id_Rol == roleId.ToString())
                .Include(rp => rp.Permiso)
                .ToListAsync();
        }

        public async Task<Rol_Permiso?> GetRolePermiso(Guid roleId, int permisoId)
        {
            return await _context.Rol_Permisos
                .FirstOrDefaultAsync(rp =>
                    rp.Id_Rol == roleId.ToString() &&
                    rp.Id_Permiso == permisoId);
        }

        public void Create(Rol_Permiso entity)
        {
            _context.Rol_Permisos.Add(entity);
        }

        public void Delete(Rol_Permiso entity)
        {
            _context.Rol_Permisos.Remove(entity);
        }

        public IQueryable<Rol_Permiso> FindByCondition(Expression<Func<Rol_Permiso, bool>> predicate, bool trackChanges)
        {
            return trackChanges
                ? _context.Rol_Permisos.Where(predicate)
                : _context.Rol_Permisos.AsNoTracking().Where(predicate);
        }
    }
}
