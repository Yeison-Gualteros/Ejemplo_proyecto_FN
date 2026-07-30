using AutoMapper;
using Contracts;
using Contracts.Interface;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Repository;
using Service.Contracts;
using Shared.DataTransferObjects;
using Shared.DataTransferObjects.User;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.Protocols;
using System.Net;

namespace Service
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ILoggerManager _logger;
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly RepositoryContext _context;
        private readonly JwtConfiguration _jwtConfig;
        private readonly IEmailService _emailService;
        private readonly string _ldapServer = "LDAP://gamma.local:389/DC=gamma,DC=local";
        private readonly int _ldapport = 389;

        private User? _currentUser;

        public AuthenticationService(
            ILoggerManager logger,
            IMapper mapper,
            UserManager<User> userManager,
            IOptions<JwtConfiguration> config,
            RepositoryContext context,
            IEmailService emailService)
        {
            _logger = logger;
            _mapper = mapper;
            _userManager = userManager;
            _context = context;
            _jwtConfig = config.Value;
            _emailService = emailService;
        }

        // Registro de usuario
        public async Task<IdentityResult> RegisterUser(UserForRegistrationDto dto)
        {
            var user = _mapper.Map<User>(dto);

            user.UserName = dto.UserName.ToLower();
            user.Estado = 1;
            user.MustChangePassword = true;

            // Contraseña temporal
            var tempPassword = string.IsNullOrWhiteSpace(dto.Password)
                ? $"Temp@{Guid.NewGuid().ToString("N")[..8]}"
                : dto.Password;

            var result = await _userManager.CreateAsync(user, tempPassword);

            if (!result.Succeeded)
                return result;

            if (dto.RoleIds != null && dto.RoleIds.Any())
            {
                var roleId = dto.RoleIds.First();
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);

                if (role == null)
                    return IdentityResult.Failed(
                        new IdentityError { Description = "Rol no válido" });

                await _userManager.AddToRoleAsync(user, role.Name);
            }

            await _emailService.SendPasswordAsync(
                user.Email,
                user.UserName,
                tempPassword
            );

            return result;
        }

        // Validar login
        public async Task<bool> ValidateUser(UserForAuthenticationDto dto)
        {
            if (dto == null) return false;

            // Normalizar y buscar usuario
            var normalizedUserName = _userManager.NormalizeName(dto.UserName);
            _currentUser = await _userManager.FindByNameAsync(normalizedUserName);

            if (_currentUser == null)
            {
                _logger.LogWarn($"Usuario no encontrado: {dto.UserName}");
                return false;
            }

            // Bloquear usuarios inactivos o eliminados
            if (_currentUser.Estado == 3)
            {
                _logger.LogWarn($"Usuario inactivo: {dto.UserName}");
                return false;
            }
            if (_currentUser.Estado == 0)
            {
                _logger.LogWarn($"Usuario eliminado: {dto.UserName}");
                return false;
            }

            // Validar contraseña
            bool validPassword = await _userManager.CheckPasswordAsync(_currentUser, dto.Password);

            if (!validPassword)
                _logger.LogWarn($"Contraseña incorrecta: {dto.UserName}");

            return validPassword;
        }


        // Crear JWT con roles y permisos
        public async Task<TokenDto> CreateToken(bool populateExpiry = true)
        {
            if (_currentUser == null)
                throw new InvalidOperationException("Usuario no validado.");

            var signingCredentials = GetSigningCredentials();
            var claims = await GetClaims();
            var token = GenerateJwtToken(signingCredentials, claims);

            // Refresh token
            string refreshToken = GenerateRefreshToken();
            _currentUser.RefreshTokken = refreshToken;

            if (populateExpiry)
                _currentUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _userManager.UpdateAsync(_currentUser);

            return new TokenDto(
                AccessToken: new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken: refreshToken
            );
        }

        private SigningCredentials GetSigningCredentials()
        {
            var key = Encoding.UTF8.GetBytes(_jwtConfig.Key!);
            var secret = new SymmetricSecurityKey(key);
            return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
        }

        private async Task<List<Claim>> GetClaims()
        {
            if (_currentUser == null)
                throw new InvalidOperationException("Usuario no validado.");

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, _currentUser.UserName),
        new Claim(ClaimTypes.NameIdentifier, _currentUser.Id),
        new Claim("mustChangePassword", _currentUser.MustChangePassword.ToString()),
        new Claim("IdProceso", _currentUser.IdProceso ?? ""),
        new Claim("idProceso", _currentUser.IdProceso ?? ""),
    };

            // ✅ OPTIMIZACIÓN: Una sola consulta para obtener TODO
            var userId = _currentUser.Id;

            // Consulta única con JOINs para obtener rol y permisos
            var query = await (from user in _context.Users
                               where user.Id == userId
                               select new
                               {
                                   // Roles del usuario
                                   Roles = (from ur in _context.UserRoles
                                            join r in _context.Roles on ur.RoleId equals r.Id
                                            where ur.UserId == userId
                                            select new
                                            {
                                                r.Name,
                                                Permisos = (from rp in _context.Rol_Permisos
                                                            join p in _context.Permisos on rp.Id_Permiso equals p.Id_Permiso
                                                            where rp.Id_Rol == r.Id
                                                            select p.Nombre).ToList()
                                            }).ToList(),

                                   // Permisos extra directos del usuario
                                   PermisosExtra = (from up in _context.user_Permisos
                                                    join p in _context.Permisos on up.Id_Permiso equals p.Id_Permiso
                                                    where up.UserId == userId
                                                    select p.Nombre).ToList()
                               }).FirstOrDefaultAsync();

            if (query != null)
            {
                var permissionSet = new HashSet<string>();

                // Agregar permisos de roles
                foreach (var role in query.Roles)
                {
                    if (!string.IsNullOrEmpty(role.Name))
                        claims.Add(new Claim(ClaimTypes.Role, role.Name));

                    foreach (var permiso in role.Permisos)
                        if (!string.IsNullOrEmpty(permiso))
                            permissionSet.Add(permiso);
                }

                // Agregar permisos extra
                foreach (var permiso in query.PermisosExtra)
                    if (!string.IsNullOrEmpty(permiso))
                        permissionSet.Add(permiso);

                // Agregar claims de permisos
                foreach (var permiso in permissionSet)
                    claims.Add(new Claim("permission", permiso));
            }

            return claims;
        }


        private JwtSecurityToken GenerateJwtToken(SigningCredentials creds, List<Claim> claims)
        {
            return new JwtSecurityToken(
                issuer: _jwtConfig.ValidIssuer,
                audience: _jwtConfig.ValidAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_jwtConfig.Expires)),
                signingCredentials: creds
            );
        }

        private static string GenerateRefreshToken()
        {
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
                return Convert.ToBase64String(randomBytes);
            }
        }

        // Refresh token
        public async Task<TokenDto> RefreshToken(TokenDto tokenDto)
        {
            var principal = GetPrincipalFromExpiredToken(tokenDto.AccessToken);

            var user = await _userManager.FindByNameAsync(principal.Identity!.Name!);
            if (user == null ||
                user.RefreshTokken != tokenDto.RefreshToken ||
                user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                throw new Exception("Refresh token inválido o expirado.");
            }

            _currentUser = user;
            return await CreateToken(populateExpiry: false);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidation = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key!)),
                ValidateLifetime = false,
                ValidIssuer = _jwtConfig.ValidIssuer,
                ValidAudience = _jwtConfig.ValidAudience
            };

            var handler = new JwtSecurityTokenHandler();
            SecurityToken validatedToken;

            var principal = handler.ValidateToken(token, tokenValidation, out validatedToken);
            if (validatedToken is not JwtSecurityToken jwt ||
                !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Token inválido");
            }

            return principal;
        }

        public User GetCurrentUser()
        {
            if (_currentUser == null)
                throw new InvalidOperationException("Usuario no autenticado.");

            return _currentUser;
        }

        public async Task<IdentityResult> ChangePassword(ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
                return IdentityResult.Failed(
                    new IdentityError { Description = "Usuario no encontrado" });

            var result = await _userManager.ChangePasswordAsync(
                user,
                dto.OldPassword,
                dto.NewPassword
            );

            if (!result.Succeeded)
                return result;

            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);

            return IdentityResult.Success;
        }

        public async Task<User> FindOrCreateLdapUser(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
            {
                var ldapData = GetLdapUserInfo(username, password);

                user = new User
                {
                    UserName = username,
                    Email = ldapData.Email,
                    Nombre = ldapData.Nombre,
                    Apellido = ldapData.Apellido,
                    MustChangePassword = false,
                    Estado = 1,
                    IsLdapUser = true,
                    FechaCreacion = DateTime.UtcNow,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                    throw new Exception("Error creando usuario LDAP: " + errors);
                }
            }

            _currentUser = user;
            return user;
        }

        // Login DLAP 
        public bool ValidateLdapUser(string username, string password)
        {
            var ldapService = "gamma.local";
            var ldapPort = 389;
            using var connection = new LdapConnection(new LdapDirectoryIdentifier(ldapService, ldapPort));

            connection.AuthType = AuthType.Negotiate;

            var upn = username + "@gamma.local";
            try
            {
                connection.Bind(new NetworkCredential(upn, password));

                return true; // Si el bind es exitoso, el usuario es válido

            }
            catch (LdapException)
            {
                //(DOMINIO\usuario)
                var netbios = @"GAMMA\" + username;
                try
                {
                    connection.Bind(new NetworkCredential(netbios, password));
                    return true;
                }
                catch (LdapException ex)
                {
                    _logger.LogError($"LDAP credenciales inválidas: {ex.Message}");
                    return false;
                }
            }
        }

        private (string? Email, string? Nombre, string? Apellido, string? DisplayName) GetLdapUserInfo(string username, string password)
        {
            var ldapService = "gamma.local";
            var ldapPort = 389;

            using var connection = new LdapConnection(
                new LdapDirectoryIdentifier(ldapService, ldapPort));

            connection.AuthType = AuthType.Negotiate;

            var upn = username + "@gamma.local";
            connection.Bind(new NetworkCredential(upn, password));

            var searchRequest = new SearchRequest(
                "DC=gamma,DC=local",
                $"(sAMAccountName={username})",
                SearchScope.Subtree,
                new[] { "mail", "displayName", "givenName", "sn" }
            );

            var response = (SearchResponse)connection.SendRequest(searchRequest);

            if (response.Entries.Count == 0)
                return (null, null, null, null);

            var entry = response.Entries[0];

            var email = entry.Attributes["mail"]?[0]?.ToString();
            var displayName = entry.Attributes["displayName"]?[0]?.ToString();
            var nombre = entry.Attributes["givenName"]?[0]?.ToString();
            var apellido = entry.Attributes["sn"]?[0]?.ToString();

            return (email, nombre, apellido, displayName);
        }

        //public async Task<User> FindOrCreateExternalUser(string email, string? nombre, string? apellido, string? displayName)
        //{
        //    var user = await _userManager.FindByEmailAsync(email);

        //    if (user == null)
        //    {
        //        user = new User
        //        {
        //            UserName = email,
        //            Email = email,
        //            Nombre = nombre ?? displayName,
        //            Apellido = apellido,
        //            MustChangePassword = false,
        //            Estado = 1,
        //            EmailConfirmed = true,
        //            IsLdapUser = false, // 🔥 IMPORTANTE
        //            FechaCreacion = DateTime.UtcNow
        //        };

        //        var result = await _userManager.CreateAsync(user);

        //        if (!result.Succeeded)
        //        {
        //            var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
        //            throw new Exception(errors);
        //        }
        //    }

        //    _currentUser = user;
        //    return user;
        //}

        public async Task<User?> GetUserByEmail(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public void SetCurrentUser(User user)
        {
            _currentUser = user;
        }

        //private (string? Email, string? Nombre, string? Apellido, string ? DisplayName) GetLdapUserInfoByEmail(string email)
        //{
        //    var ldapService = "gamma.local";
        //    var ldapPort = 389;

        //    using var connection = new LdapConnection(new LdapDirectoryIdentifier(ldapService, ldapPort));

        //    connection.AuthType = AuthType.Negotiate;

        //    connection.Bind();

        //    var searchRequest = new SearchRequest(
        //        "DC=gamma,DC=local",
        //        $"(mail={email})",
        //        SearchScope.Subtree,
        //        new[] { "mail", "displayName", "givenName", "sn" }
        //    );

        //    var response = (SearchResponse)connection.SendRequest(searchRequest);

        //    if (response.Entries.Count == 0)
        //        return (null, null, null, null);

        //    var entry = response.Entries[0];

        //    var ldapEmail = entry.Attributes["mail"]?[0]?.ToString();
        //    var displayName = entry.Attributes["displayName"]?[0]?.ToString();
        //    var nombre = entry.Attributes["givenName"]?[0]?.ToString();
        //    var apellido = entry.Attributes["sn"]?[0]?.ToString();
        //    var userName = entry.Attributes["sAMAccountName"]?[0]?.ToString();

        //    return (ldapEmail, nombre, apellido, displayName);

        //}

        public async Task<bool> UserExists(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user != null;
        }

        //public bool LdapUserExistsByEmail(string email)
        //{
        //    var ldapService = "gamma.local";
        //    var ldapPort = 389;

        //    using var connection = new LdapConnection(
        //        new LdapDirectoryIdentifier(ldapService, ldapPort));

        //    connection.AuthType = AuthType.Negotiate;
        //    connection.Bind(); // Usa credenciales del servidor

        //    var searchRequest = new SearchRequest(
        //        "DC=gamma,DC=local",
        //        $"(mail={email})",
        //        SearchScope.Subtree,
        //        new[] { "mail" }
        //    );

        //    var response = (SearchResponse)connection.SendRequest(searchRequest);

        //    return response.Entries.Count > 0;
        //}

    }
}
