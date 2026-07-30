using Contracts.Interface;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repository.Extensions;
using Shared.RequestFeatures;

namespace Repository.Clases
{
    public class UserRepository : RepositoryBase<User>, IUserRepository
    {
        private readonly RepositoryContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public UserRepository(RepositoryContext context, UserManager<User> userManager, RoleManager<Role> roleManager) : base(context)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<PagedList<User>> GetUsersAsync(UserParameters parameters)
        {
            var query = _context.Users
                .Include(u => u.User_Permisos)
                    .ThenInclude(up => up.Permiso)
                .FilteUser()
                .SearchUser(parameters.Busqueda)
                .SortUser(parameters.Orden);

            var count = await query.CountAsync();

            var items = await query
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .ToListAsync();

            return new PagedList<User>(
                items,
                count,
                parameters.PageNumber,
                parameters.PageSize
            );
        }

        public async Task<User?> GetByIdAsync(string userId)
        {
            return await _context.Users
                .Include(u => u.User_Permisos)
                    .ThenInclude(up => up.Permiso)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<Role?> GetUserRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return null;

            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault();

            if (roleName == null)
                return null;

            return await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == roleName);
        }

        public async Task<IEnumerable<User>> GetAllActiveUsersWithPermissionsAsync()
        {
            return await _context.Users
                .Include(u => u.User_Permisos)
                    .ThenInclude(up => up.Permiso)
                .Where(u => u.Estado == 1)
                .OrderBy(u => u.UserName)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetUsuariosConPermisByProcesoAsync(string permiso, string idProceso)
        {
            // Obtener el ID del permiso primero 
            var permisoId = await _context.Permisos
                .Where(p => p.Nombre == permiso)
                .Select(p => p.Id_Permiso)
                .FirstOrDefaultAsync();

            if (permisoId == 0)
                return new List<User>();

            // Obtener usuarios del proceso que tienen el permiso (directo o por rol)
            // Usando SQL directo con LINQ optimizado
            var usuariosConPermiso = await _context.Users
                .Where(u => u.IdProceso == idProceso && u.Estado == 1)
                .Select(u => new
                {
                    Usuario = u,
                    TienePermisoDirecto = u.User_Permisos.Any(up => up.Id_Permiso == permisoId),
                    TienePermisoPorRol = _context.UserRoles
                        .Where(ur => ur.UserId == u.Id)
                        .Join(_context.Rol_Permisos,
                            ur => ur.RoleId,
                            rp => rp.Id_Rol,
                            (ur, rp) => rp.Id_Permiso)
                        .Any(rpId => rpId == permisoId)
                })
                .ToListAsync();

            // Filtrar los que tienen el permiso (directo o por rol)
            var resultado = usuariosConPermiso
                .Where(x => x.TienePermisoDirecto || x.TienePermisoPorRol)
                .Select(x => x.Usuario)
                .ToList();

            return resultado;
        }
    } 
}