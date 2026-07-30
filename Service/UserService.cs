using AutoMapper;
using Contracts;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repository;
using Service.Contracts;
using Shared.DataTransferObjects;
using Shared.DataTransferObjects.Permisos;
using Shared.DataTransferObjects.User;
using Shared.RequestFeatures;
using System.Security.Cryptography;
using System.Diagnostics;

namespace Service
{
    public class UserService : IUserService
    {
        private readonly IRepositoryManager _repository;
        private readonly ILoggerManager _logger;
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly RepositoryContext _context;
        private readonly IPermisosService _permisosService;
        private readonly IEmailService _emailService;

        public UserService(
            IRepositoryManager repository,
            IMapper mapper,
            ILoggerManager logger,
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            RepositoryContext repositoryContext,
            IPermisosService permisosService,
            IEmailService emailService)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = repositoryContext;
            _permisosService = permisosService;
            _emailService = emailService;
        }

        public async Task<PagedList<UserDto>> GetAllAsync(UserParameters parameters)
        {
            var stopwatchTotal = Stopwatch.StartNew();
            _logger.LogInfo($"⏱️ [INICIO] GetAllAsync - {DateTime.Now:HH:mm:ss.fff}");

            var stopwatchQuery = Stopwatch.StartNew();
            var users = await _repository.User.GetUsersAsync(parameters);
            stopwatchQuery.Stop();
            _logger.LogInfo($"⏱️ [CONSULTA] GetUsersAsync: {stopwatchQuery.ElapsedMilliseconds}ms - {users.Count()} usuarios");

            var stopwatchMap = Stopwatch.StartNew();
            // ✅ Usar MapUsersAsync en lugar de MapUserAsync para cada usuario
            var usersDto = await MapUsersAsync(users.ToList());
            stopwatchMap.Stop();

            stopwatchTotal.Stop();
            _logger.LogInfo($"⏱️ [FIN] GetAllAsync - Total: {stopwatchTotal.ElapsedMilliseconds}ms");

            // Mantener el orden original
            var usersDtoOrdered = users.Select(u => usersDto.FirstOrDefault(d => d.Id == u.Id))
                .Where(d => d != null)
                .ToList();

            return new PagedList<UserDto>(
                usersDtoOrdered,
                users.MetaData.TotalCount,
                users.MetaData.CurrentPage,
                users.MetaData.PageSize
            );
        }

        public async Task<IEnumerable<UserDto>> GetUsuariosConPermiso(string permiso)
        {
            var usuarios = await _repository.User
                .FindAll(false)
                .Include(u => u.User_Permisos)
                    .ThenInclude(up => up.Permiso)
                .ToListAsync();

            var resultado = new List<User>();

            foreach (var user in usuarios)
            {
                var tieneDirecto = user.User_Permisos
                    .Any(up => up.Permiso.Nombre == permiso);

                var roles = await _userManager.GetRolesAsync(user);

                bool tienePorRol = false;

                if (roles.Any())
                {
                    var role = await _roleManager.FindByNameAsync(roles.First());

                    if (role != null)
                    {
                        var permisosRol = await _repository.Permisos
                            .GetPermisosPorRoleId(role.Id, false);

                        tienePorRol = permisosRol.Any(p => p.Nombre == permiso);

                    }
                    if (tieneDirecto || tienePorRol)
                    {
                        resultado.Add(user);
                    }
                }


            }
            return _mapper.Map<IEnumerable<UserDto>>(resultado);
        }

        public async Task<UserDto> GetByIdAsync(string id)
        {
            // ✅ Validar que el ID no sea null o vacío
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarn("⚠️ GetByIdAsync: ID nulo o vacío");
                throw new ArgumentException("El ID del usuario no puede ser nulo o vacío");
            }

            _logger.LogInfo($"🔍 GetByIdAsync: Buscando usuario con ID: '{id}'");

            var user = await _repository.User.GetByIdAsync(id);

            if (user == null)
            {
                _logger.LogWarn($"⚠️ GetByIdAsync: Usuario no encontrado con ID: '{id}'");

                // ✅ Verificar si el ID parece ser de otro tipo (ej: ID de documento numérico)
                if (int.TryParse(id, out _))
                {
                    _logger.LogWarn($"⚠️ El ID '{id}' parece ser numérico. ¿Se está confundiendo con un ID de documento?");
                }

                throw new KeyNotFoundException($"Usuario no encontrado con ID: {id}");
            }

            _logger.LogInfo($"✅ GetByIdAsync: Usuario encontrado: {user.UserName}");
            return await MapUserAsync(user);
        }

        public async Task<ServiceResultDto<UserDto>> CreateAsync(UserForRegistrationDto dto)
        {
            if (dto.RoleIds == null || !dto.RoleIds.Any())
            {
                return new ServiceResultDto<UserDto>
                {
                    Success = false,
                    Error = "Debe seleccionar un rol"
                };
            }

            var roleId = dto.RoleIds.First();
            var role = await _roleManager.FindByIdAsync(roleId);

            if (role == null)
            {
                return new ServiceResultDto<UserDto>
                {
                    Success = false,
                    Error = "El rol seleccionado no existe"
                };
            }

            if (!string.IsNullOrEmpty(dto.IdProceso))
            {
                var proceso = await _repository.Proceso.GetByIdAsync(dto.IdProceso, false);
                if (proceso == null)
                {
                    return new ServiceResultDto<UserDto>
                    {
                        Success = false,
                        Error = $"El proceso {dto.IdProceso} no existe"
                    };
                }
            }

            // Crear usuario
            var user = _mapper.Map<User>(dto);
            user.UserName = dto.UserName!.ToLower();
            user.Estado = 1;
            user.MustChangePassword = true;

            user.IdProceso = dto.IdProceso;

            var password = GenerarPasswordSegura();

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return new ServiceResultDto<UserDto>
                {
                    Success = false,
                    Error = string.Join(" | ", result.Errors.Select(e => e.Description))
                };
            }

            // ASIGNAR ROL 
            await _userManager.AddToRoleAsync(user, role.Name);

            // Permisos extra
            if (dto.Permisos != null)
            {
                await _permisosService.SaveUserPermisos(user.Id, dto.Permisos);
            }

            // Email
            try
            {
                await _emailService.SendPasswordAsync(
                    user.Email!,
                    user.UserName!,
                    password
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarn($"No se pudo enviar correo: {ex.Message}");
                // NO lanzar excepción
            }


            return new ServiceResultDto<UserDto>
            {
                Success = true,
                Data = await MapUserAsync(user)
            };
        }



        public async Task<UserDto> UpdateAsync(string id, UserForUpdateDto dto)
        {
            var user = await _userManager.FindByIdAsync(id)
                ?? throw new Exception("Usuario no existe");

            _mapper.Map(dto, user);
            await _userManager.UpdateAsync(user);

            return await GetByIdAsync(user.Id);
        }


        // MÉTODO CLAVE
        private async Task<List<UserDto>> MapUsersAsync(List<User> users)
        {
            if (!users.Any()) return new List<UserDto>();

            var userIds = users.Select(u => u.Id).ToList();

            // 1. Cargar todos los permisos extra de una vez
            var userPermisosDict = await _context.user_Permisos
                .Include(up => up.Permiso)
                .Where(up => userIds.Contains(up.UserId))
                .GroupBy(up => up.UserId)
                .ToDictionaryAsync(g => g.Key, g => g.Select(up => up.Permiso).ToList());

            // 2. Cargar todos los roles de los usuarios de una vez
            var userRolesDict = await _context.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .GroupBy(ur => ur.UserId)
                .ToDictionaryAsync(g => g.Key, g => g.Select(ur => ur.RoleId).ToList());

            // 3. Cargar todos los roles con sus permisos de una vez
            var rolesConPermisosDict = await _context.Roles
                .Include(r => r.Rol_Permisos)
                    .ThenInclude(rp => rp.Permiso)
                .ToDictionaryAsync(r => r.Id, r => r);

            // 4. Cargar todos los procesos de una vez
            var procesosDict = await _context.Procesos
                .ToDictionaryAsync(p => p.IdProceso, p => p);

            var result = new List<UserDto>();

            foreach (var user in users)
            {
                // Obtener permisos extra del usuario
                var permisosExtra = userPermisosDict.GetValueOrDefault(user.Id) ?? new List<Permiso>();

                // Obtener roles del usuario
                var roleIds = userRolesDict.GetValueOrDefault(user.Id) ?? new List<string>();

                // Obtener permisos del rol
                var permisosRol = new List<Permiso>();
                foreach (var roleId in roleIds)
                {
                    if (rolesConPermisosDict.TryGetValue(roleId, out var role))
                    {
                        permisosRol.AddRange(role.Rol_Permisos.Select(rp => rp.Permiso));
                    }
                }

                // Combinar permisos (sin duplicados)
                var permisosFinales = permisosRol
                    .Union(permisosExtra)
                    .Select(p => new PermisosDto
                    {
                        Id_Permiso = p.Id_Permiso,
                        Nombre = p.Nombre,
                        Source = permisosExtra.Contains(p) ? "user" : "role"
                    })
                    .ToList();

                var rolNombre = roleIds.Count > 0 && rolesConPermisosDict.TryGetValue(roleIds.First(), out var primerRol)
                    ? primerRol.Name
                    : null;

                var proceso = user.IdProceso != null ? procesosDict.GetValueOrDefault(user.IdProceso) : null;

                result.Add(new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Nombre = user.Nombre,
                    Apellido = user.Apellido,
                    Estado = user.Estado,
                    Rol = rolNombre,
                    Permisos = permisosFinales,
                    IdProceso = user.IdProceso,
                    NombreProceso = proceso?.Nombre
                });
            }

            return result;
        }

        private async Task<UserDto> MapUserAsync(User user)
        {
            var usersDto = await MapUsersAsync(new List<User> { user });
            return usersDto.FirstOrDefault();
        }

        private static string GenerarPasswordSegura()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(9)) + "!";
        }

        public async Task UpdateFullAsync(string userId, UserForUpdateFullDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new Exception("Usuario no encontrado");

            if (!string.IsNullOrEmpty(dto.IdProceso) && dto.IdProceso != user.IdProceso)
            {
                var proceso = await _repository.Proceso.GetByIdAsync(dto.IdProceso, false);
                if (proceso == null)
                {
                    throw new Exception($"El proceso {dto.IdProceso} no existe");
                }
                user.IdProceso = dto.IdProceso;
            }

            // Actualizar campos básicos
            _mapper.Map(dto, user);
            await _userManager.UpdateAsync(user);

            if (!string.IsNullOrWhiteSpace(dto.RoleId))
            {
                var role = await _roleManager.FindByIdAsync(dto.RoleId)
                    ?? throw new Exception("Rol no válido");

                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);

                await _userManager.AddToRoleAsync(user, role.Name);

                // Sincronizar permisos extra: eliminar los que ahora son parte del rol
                if (dto.PermisosIds != null)
                {
                    var permisosDelRol = await _repository.Permisos
                        .GetPermisosPorRoleId(dto.RoleId, false);

                    var permisosRolIds = permisosDelRol.Select(p => p.Id_Permiso).ToHashSet();

                    // Solo mantener permisos extra que NO estén en el rol
                    var permisosExtraFiltrados = dto.PermisosIds
                        .Where(p => !permisosRolIds.Contains(p))
                        .ToList();

                    await _permisosService.SaveUserPermisos(userId, permisosExtraFiltrados);
                }
            }
            else
            {
                // Si no se cambió de rol, simplemente actualizar permisos extra
                if (dto.PermisosIds != null && dto.PermisosIds.Any())
                {
                    await _permisosService.SaveUserPermisos(userId, dto.PermisosIds);
                }
                else if (dto.PermisosIds != null && !dto.PermisosIds.Any())
                {
                    // Eliminar TODOS los permisos extra
                    await _permisosService.SaveUserPermisos(userId, new List<int>());
                }

            }
        }

        public async Task<object> GetPermisosEdicionAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new Exception("Usuario no existe");
            // Rol
            var roles = await _userManager.GetRolesAsync(user);
            var rolNombre = roles.FirstOrDefault();
            var role = string.IsNullOrEmpty(rolNombre)
                ? null : await _roleManager.FindByNameAsync(rolNombre);
            // Permisos del rol
            var permisosRol = role == null
                ? new List<Permiso>() :
                await _repository.Permisos.GetPermisosPorRoleId(role.Id, false);

            var permisosRolIds = permisosRol.Select(p => p.Id_Permiso).ToHashSet();
            // Permisos extra del usuario
            await _context.Entry(user).Collection(u => u.User_Permisos).Query().Include(up => up.Permiso).LoadAsync();
            var permisosExtra = user.User_Permisos.Select(up => up.Permiso).ToList();
            var permisosExtraIds = permisosExtra.Select(p => p.Id_Permiso).ToHashSet();
            // TODOS los permisos
            var todos = await _repository.Permisos.GetAllPermisos(null, false);
            // DISPONIBLES = no rol y no extra
            var disponibles = todos.Where(p => !permisosRolIds.Contains(p.Id_Permiso) && !permisosExtraIds.Contains(p.Id_Permiso)).Select(p => new PermisosDto { Id_Permiso = p.Id_Permiso, Nombre = p.Nombre }).ToList();
            return new { permisosRol = permisosRol.Select(p => new PermisosDto { Id_Permiso = p.Id_Permiso, Nombre = p.Nombre, Source = "role" }), permisosExtra = permisosExtra.Select(p => new PermisosDto { Id_Permiso = p.Id_Permiso, Nombre = p.Nombre, Source = "user" }), permisosDisponibles = disponibles };
        }

        public async Task<IEnumerable<object>> GetUsuariosConPermisosDashboardAsync()
        {
            // Obtener usuarios activos con permisos directos
            var usuarios = await _repository.User.GetAllActiveUsersWithPermissionsAsync();

            // Obtener TODOS los roles con sus permisos (1 sola consulta)
            var rolesConPermisos = await _roleManager.Roles
                .Include(r => r.Rol_Permisos)
                    .ThenInclude(rp => rp.Permiso)
                .ToListAsync();

            // Obtener TODOS los UserRoles de una vez
            var userIds = usuarios.Select(u => u.Id).ToList();
            var userRolesDict = await _context.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .GroupBy(ur => ur.UserId)
                .ToDictionaryAsync(g => g.Key, g => g.Select(ur => ur.RoleId).ToList());

            var resultado = new List<object>();

            foreach (var usuario in usuarios)
            {
                var userRoleIds = userRolesDict.GetValueOrDefault(usuario.Id, new List<string>());

                // Permisos directos
                var permisosDirectos = usuario.User_Permisos?
                    .Select(up => up.Permiso?.Nombre ?? "")
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList() ?? new List<string>();

                // Permisos de los roles
                var permisosRol = rolesConPermisos
                    .Where(r => userRoleIds.Contains(r.Id))
                    .SelectMany(r => r.Rol_Permisos?
                        .Select(rp => rp.Permiso?.Nombre ?? "") ?? Enumerable.Empty<string>())
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList();

                // Combinar
                var todosPermisos = permisosDirectos
                    .Union(permisosRol)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                resultado.Add(new
                {
                    usuario.Id,
                    usuario.UserName,
                    usuario.Nombre,
                    usuario.Apellido,
                    usuario.Estado,
                    Permisos = todosPermisos
                });
            }

            return resultado;
        }

        public async Task<IEnumerable<UserDto>> GetUsuariosConPermisoByProcesoAsync(string permiso, string IdProceso)
        {
            var usuarios = await _repository.User.GetUsuariosConPermisByProcesoAsync(permiso, IdProceso);
            return _mapper.Map<IEnumerable<UserDto>>(usuarios);
        }
    }
}