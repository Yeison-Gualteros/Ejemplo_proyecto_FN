using Microsoft.AspNetCore.Authorization;

namespace FileNova.Presentation.Authorization
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            var hasPermission = context.User.Claims
                .Any(c => c.Type == "permission" &&
                          c.Value == requirement.Permiso);

            if (hasPermission)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }

}
