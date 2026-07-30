using Microsoft.AspNetCore.Authorization;

namespace FileNova.Presentation.Authorization
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permiso { get; }
        public PermissionRequirement(string permiso) {
            Permiso = permiso;
        }
    }
}
