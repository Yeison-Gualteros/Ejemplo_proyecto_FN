using Microsoft.AspNetCore.Authorization;

namespace FileNova.Presentation.Authorization
{
    public class HasPermissionAttribute : AuthorizeAttribute
    {
        public HasPermissionAttribute(string permiso)
        {
            Policy = $"Permission{permiso}";
        }
    }
}
