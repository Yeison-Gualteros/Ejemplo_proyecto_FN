using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace FileNova.Presentation.Authorization
{
    public class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private const string PREFIX = "Permission";
        private readonly DefaultAuthorizationPolicyProvider _fallback;

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallback = new DefaultAuthorizationPolicyProvider(options);
        }
        
        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith(PREFIX))
            {
                var permiso = policyName.Replace(PREFIX, "");
                var policy = new AuthorizationPolicyBuilder();
                policy.AddRequirements(new PermissionRequirement(permiso));
                return Task.FromResult(policy.Build());
            }
            return _fallback.GetPolicyAsync(policyName);
        }
    }
}
