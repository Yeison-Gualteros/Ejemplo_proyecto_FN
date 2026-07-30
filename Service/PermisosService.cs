using AutoMapper;
using Contracts;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using Shared.DataTransferObjects.Permisos;

namespace Service
{
    public class PermisosService : IPermisosService
    {
        private readonly IRepositoryManager _repository;
        private readonly ILoggerManager _logger;
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;

        public PermisosService(
            IRepositoryManager repository,
            ILoggerManager logger,
            IMapper mapper,
            UserManager<User> userManager)
        {
            _repository = repository;
            _logger = logger;
            _mapper = mapper;
            _userManager = userManager;
        }

        // CONSULTAS
        public async Task<IEnumerable<PermisosDto>> GetAllPermisos(
            int? id_Permiso,
            bool trackChanges)
        {
            var permisos = await _repository.Permisos
                .GetAllPermisos(id_Permiso, trackChanges);

            return permisos == null
                ? Enumerable.Empty<PermisosDto>()
                : _mapper.Map<IEnumerable<PermisosDto>>(permisos);
        }

        public async Task<IEnumerable<PermisosDto>> GetUserPermisos(
            string userId,
            bool trackChanges)
        {
            var rol = await _repository.User.GetUserRole(userId.ToString());
            if (rol == null)
                throw new KeyNotFoundException("El usuario no tiene rol asignado.");

            var permisosRol = await _repository.Permisos
                .GetPermisosPorRoleId(rol.Id, trackChanges);

            var permisosUsuario = await _repository.Permisos
                .GetUserPermisos(userId, trackChanges);

            var permisos = permisosRol
                .Concat(permisosUsuario)
                .Distinct();

            return _mapper.Map<IEnumerable<PermisosDto>>(permisos);
        }

        public async Task<IEnumerable<PermisosDto>> GetPermissionsByRole(string roleId)
        {
            // Traemos todos los permisos del rol como Rol_Permiso incluyendo Permiso
            var rolPermisos = await _repository.Rol_Permisos
                .GetPermisosByRole(Guid.Parse(roleId));

            // Mapear cada Rol_Permiso → PermisosDto
            return _mapper.Map<IEnumerable<PermisosDto>>(rolPermisos);
        }

        public async Task<IEnumerable<PermisosDto>> GetPermissionsByUser(string roleId)
        {
            // Traemos todos los permisos del rol como Rol_Permiso incluyendo Permiso
            var rolPermisos = await _repository.Rol_Permisos
                .GetPermisosByRole(Guid.Parse(roleId));

            // Mapear cada Rol_Permiso → PermisosDto
            return _mapper.Map<IEnumerable<PermisosDto>>(rolPermisos);
        }

        // CRUD
        public async Task<PermisosDto> CreatePermiso(PermisosForCreationDto permiso)
        {
            var entity = _mapper.Map<Permiso>(permiso);

            _repository.Permisos.Create(entity);
            await _repository.SaveAsync();

            return _mapper.Map<PermisosDto>(entity);
        }

        public async Task<PermisosDto> GetPermisoById(
            int id_Permiso,
            bool trackChanges)
        {
            var permiso = await _repository.Permisos
                .GetPermisoById(id_Permiso, trackChanges);

            if (permiso == null)
                throw new KeyNotFoundException("Permiso no encontrado.");

            return _mapper.Map<PermisosDto>(permiso);
        }

        public async Task<PermisosDto> UpdatePermiso(int id_Permiso, PermisoForUpdateDto dto)
        {
            var permiso = await _repository.Permisos
                .GetPermisoById(id_Permiso, true);

            if (permiso == null)
                throw new KeyNotFoundException("Permiso no encontrado.");

            _mapper.Map(dto, permiso);
            await _repository.SaveAsync();

            return _mapper.Map<PermisosDto>(permiso);
        }

        public async Task<bool> DeletePermiso(int id_Permiso)
        {
            var permiso = await _repository.Permisos
                .GetPermisoById(id_Permiso, true);

            if (permiso == null)
                return false;

            _repository.Permisos.Delete(permiso);
            await _repository.SaveAsync();

            return true;
        }

        // USUARIOS
        public async Task AddPermissionsToUser(string userId, List<int> permisosIds)
        {
            foreach (var permisoId in permisosIds)
            {
                _repository.UserPermisos.Create(new User_Permiso
                {
                    UserId = userId.ToString(),
                    Id_Permiso = permisoId
                });
            }

            await _repository.SaveAsync();
        }

        public async Task RemovePermissionFromUser(string userId, int permisoId)
        {
            var entity = _repository.UserPermisos
                .FindByCondition(
                    x => x.UserId == userId.ToString() &&
                         x.Id_Permiso == permisoId,
                    trackChanges: true)
                .FirstOrDefault();

            if (entity == null)
                return; // No hay nada que eliminar

            _repository.UserPermisos.Delete(entity);
        }

        // ROLES
        public async Task AddPermissionsToRole(string roleId, List<int> permisosIds)
        {
            if (permisosIds == null || !permisosIds.Any()) return;

            foreach (var permisoId in permisosIds)
            {
                var existe = await _repository.Rol_Permisos.ExistsAsync(roleId, permisoId);
                if (existe) continue;

                _repository.Rol_Permisos.Create(new Rol_Permiso
                {
                    Id_Rol = roleId,
                    Id_Permiso = permisoId
                });
            }

            await _repository.SaveAsync();
        }

        public async Task RemovePermissionFromRole(string roleId, int permisoId)
        {
            var entity = await _repository.Rol_Permisos
                .FindByCondition(rp => rp.Id_Rol == roleId && rp.Id_Permiso == permisoId, trackChanges: true)
                .FirstOrDefaultAsync();

            if (entity == null) return;

            _repository.Rol_Permisos.Delete(entity);
            await _repository.SaveAsync();
        }

        // agregamos el método de sincronización
        public async Task UpdatePermissionsOfRole(string roleId, List<int> permisosIds)
        {
            // Obtener permisos actuales del rol
            var permisosActuales = await _repository.Permisos
                .GetPermisosPorRoleId(roleId, trackChanges: true);

            var permisosActualesIds = permisosActuales.Select(p => p.Id_Permiso).ToList();

            // Eliminar permisos que ya no están seleccionados
            foreach (var permisoId in permisosActualesIds)
            {
                if (!permisosIds.Contains(permisoId))
                {
                    await RemovePermissionFromRole(roleId, permisoId);
                }
            }

            // Agregar permisos nuevos
            foreach (var permisoId in permisosIds)
            {
                if (!permisosActualesIds.Contains(permisoId))
                {
                    _repository.Rol_Permisos.Create(new Rol_Permiso
                    {
                        Id_Rol = roleId,
                        Id_Permiso = permisoId
                    });
                }
            }

            await _repository.SaveAsync();
        }

        public async Task UpdatePermissionsOfUser(SaveUserPermisosDto dto)
        {
            if (dto == null)
                throw new Exception("DTO es null");

            var userId = dto.UserId;
            dto.PermisosIds ??= new List<int>();

            // Obtener relaciones actuales (User_Permiso)
            var actuales = await _repository.UserPermisos
                .FindByCondition(up => up.UserId == userId, trackChanges: true)
                .ToListAsync();

            var actualesIds = actuales
                .Select(x => x.Id_Permiso)
                .ToList();

            // Eliminar los que ya no existen
            foreach (var entity in actuales)
            {
                if (!dto.PermisosIds.Contains(entity.Id_Permiso))
                {
                    _repository.UserPermisos.Delete(entity);
                }
            }

            // Agregar los nuevos
            foreach (var permisoId in dto.PermisosIds)
            {
                if (!actualesIds.Contains(permisoId))
                {
                    _repository.UserPermisos.Create(new User_Permiso
                    {
                        UserId = userId,
                        Id_Permiso = permisoId
                    });
                }
            }
 
            await _repository.SaveAsync();
        }

        public async Task<IEnumerable<PermisosDto>> GetPermisosUIByRole(string roleId)
        {
            // Todos los permisos
            var todos = await _repository.Permisos.GetAllPermisos(null, false);

            // Permisos del rol
            var permisosRol = await _repository.Permisos
                .GetPermisosPorRoleId(roleId, false);

            var permisosRolIds = permisosRol.Select(p => p.Id_Permiso).ToHashSet();

            return todos.Select(p => new PermisosDto
            {
                Id_Permiso = p.Id_Permiso,
                Nombre = p.Nombre,

                Heredado = permisosRolIds.Contains(p.Id_Permiso),
                Selected = permisosRolIds.Contains(p.Id_Permiso),
                Disabled = permisosRolIds.Contains(p.Id_Permiso),
                Source = permisosRolIds.Contains(p.Id_Permiso)
                    ? "role"
                    : "available"
            });
        }

        public async Task<IEnumerable<PermisosDto>> GetPermisosUIByUser(string userId)
        {
            // Rol del usuario
            var rol = await _repository.User.GetUserRole(userId.ToString());
            if (rol == null)
                throw new Exception("Usuario sin rol");

            // Todos los permisos
            var todos = await _repository.Permisos.GetAllPermisos(null, false);

            // Permisos del rol
            var permisosRol = await _repository.Permisos
                .GetPermisosPorRoleId(rol.Id, false);

            // Permisos extra del usuario
            var permisosUser = await _repository.Permisos
                .GetUserPermisos(userId, false);

            var rolIds = permisosRol.Select(p => p.Id_Permiso).ToHashSet();
            var userIds = permisosUser.Select(p => p.Id_Permiso).ToHashSet();

            return todos.Select(p => new PermisosDto
            {
                Id_Permiso = p.Id_Permiso,
                Nombre = p.Nombre,

                Heredado = rolIds.Contains(p.Id_Permiso),

                Selected = userIds.Contains(p.Id_Permiso),

                Disabled = rolIds.Contains(p.Id_Permiso),

                Source = rolIds.Contains(p.Id_Permiso)
                ? "role"
                : userIds.Contains(p.Id_Permiso)
                    ? "user"
                    : "available"
            });

        }

        public async Task SaveUserPermisos(string userId, List<int> permisosIds)
        {
            permisosIds ??= new List<int>();

            var actuales = await _repository.UserPermisos
                .FindByCondition(up => up.UserId == userId, trackChanges: true)
                .ToListAsync();

            var actualesIds = actuales.Select(x => x.Id_Permiso).ToList();

            foreach (var entity in actuales)
            {
                if (!permisosIds.Contains(entity.Id_Permiso))
                    _repository.UserPermisos.Delete(entity);
            }

            foreach (var permisoId in permisosIds)
            {
                if (!actualesIds.Contains(permisoId))
                {
                    _repository.UserPermisos.Create(new User_Permiso
                    {
                        UserId = userId,
                        Id_Permiso = permisoId
                    });
                }
            }
        }


        public async Task<bool> UsuarioTienePermiso(string userId, string permisoNombre)
        {
            // Permisos directos
            var permisosUsuario = await _repository.UserPermisos.GetPermissionsForUser(userId);

            if (permisosUsuario.Any(p => p.Permiso.Nombre == permisoNombre))
                return true;

            // Rol del usuario
            var rol = await _repository.User.GetUserRole(userId);

            if (rol == null) return false;

            var permisosRol = await _repository.Permisos
                .GetPermisosPorRoleId(rol.Id, false);

            return permisosRol.Any(p => p.Nombre == permisoNombre);
        }

        public async Task<HashSet<string>> ObtenerPermisosUsuario(string userId)
        {
            // Rol
            var rol = await _repository.User.GetUserRole(userId);

            // Permisos rol
            var permisosRol = rol != null
                ? await _repository.Permisos.GetPermisosPorRoleId(rol.Id, false)
                : new List<Permiso>();

            // Permisos directos
            var permisosUsuario = await _repository.Permisos
                .GetUserPermisos(userId, false);

            // Unir TODO
            var permisos = permisosRol
                .Concat(permisosUsuario)
                .Select(p => p.Nombre)
                .Distinct()
                .ToHashSet();

            return permisos;
        }
    }
}
