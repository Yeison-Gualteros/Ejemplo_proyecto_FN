using Entities.Models;
using FileNova.Presentation.Filters;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Service.Contracts;
using Shared.DataTransferObjects;
using Shared.DataTransferObjects.User;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FileNova.Presentation.Controllers
{
    [Route("api/authentication")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IServiceManager _service;
        private readonly IConfiguration _configuration;


        public AuthenticationController(IServiceManager service, IConfiguration configuration) { 
            _service = service;
            _configuration = configuration;
        }

        [HttpPost]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> RegisterUser([FromBody] UserForRegistrationDto userForRegistration)
        {
            var result = await _service.AuthenticationService.RegisterUser(userForRegistration);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.TryAddModelError(error.Code, error.Description);
                }
                return BadRequest(ModelState);
            }
            return StatusCode(201);
        }

        [HttpPost("login")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> Authenticate([FromBody] UserForAuthenticationDto userForAuthentication)
        {
            if (userForAuthentication == null)
                return BadRequest("Datos de login no proporcionados");

            if (!await _service.AuthenticationService.ValidateUser(userForAuthentication))
                return Unauthorized("Usuario o contraseña incorrectos");

            var user = _service.AuthenticationService.GetCurrentUser();

            if (user.MustChangePassword)
            {
                return Ok(new
                {
                    mustChangePassword = true,
                    userId = user.Id
                });
            }

            var tokenDto = await _service.AuthenticationService.CreateToken(populateExpiry: true);

            // Devuelve un JSON con los tokens
            return Ok(new
            {
                accessToken = tokenDto.AccessToken,
                refreshToken = tokenDto.RefreshToken
            });
        }


        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var result = await _service.AuthenticationService.ChangePassword(dto);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Contraseña actualizada correctamente" });
        }

        
        //Login con Directorio Activo (LDAP)
        [HttpPost("login-ldap")]
        public async Task<IActionResult> LoginLdap([FromBody] Login request)
        {
            if (!_service.AuthenticationService.ValidateLdapUser( request.UserName, request.Password))
            {
                return Unauthorized("Usuario o contraseña incorrectos");
            }

            await _service.AuthenticationService.FindOrCreateLdapUser(request.UserName, request.Password);


            var tokenDto = await _service.AuthenticationService.CreateToken(populateExpiry: true);

            return Ok(new
            {
                accessToken = tokenDto.AccessToken,
                refreshToken = tokenDto.RefreshToken
            });
        }


        // Login Con Google (OAuth2)
        [HttpGet("login-google")]
        public IActionResult GoogleLogin()
        {
            var props = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleResponse), "Authentication")
            };

            return Challenge(props, "Google");
        }

        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(
                IdentityConstants.ExternalScheme);

            if (!result.Succeeded || result.Principal == null)
                return Redirect($"{_configuration["Frontend:BaseUrl"]}/login?error=auth_failed");

            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email) ||
                !email.EndsWith("@gammaingenieros.com"))
                return Redirect($"{_configuration["Frontend:BaseUrl"]}/login?error=domain");

            var userExists = await _service.AuthenticationService.UserExists(email);

            if (!userExists)
            {
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                var message = "Debe iniciar sesion primero con su usuario y contrasena corporativa.";

                return Redirect(
                    $"{_configuration["Frontend:BaseUrl"]}/login?error={Uri.EscapeDataString(message)}");
            }

            var user = await _service.AuthenticationService.GetUserByEmail(email);

            _service.AuthenticationService.SetCurrentUser(user);

            var tokenDto = await _service.AuthenticationService.CreateToken(true);

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            return Redirect(
                $"{_configuration["Frontend:BaseUrl"]}/google-success?token={tokenDto.AccessToken}");
        }
    }
}
