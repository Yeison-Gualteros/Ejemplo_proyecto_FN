using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Shared.DataTransferObjects;
using Shared.DataTransferObjects.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Interface
{
    public interface IAuthenticationService
    {
        Task<IdentityResult> RegisterUser(UserForRegistrationDto userForRegistration);

        Task<bool> ValidateUser(UserForAuthenticationDto userForAuthentication);
        Task<TokenDto> CreateToken(bool populateExpiry = true);

        Task<TokenDto> RefreshToken(TokenDto tokenDto);

        Task<IdentityResult> ChangePassword(ChangePasswordDto dto);
        User GetCurrentUser();
        bool ValidateLdapUser(string username, string password);
        Task<User> FindOrCreateLdapUser(string username, string password);
        Task<bool> UserExists(string email);
        Task<User?> GetUserByEmail(string email);
        void SetCurrentUser(User user);
    }
}
