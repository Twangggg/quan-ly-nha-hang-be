using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace FoodHub.Infrastructure.Security
{
    public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
            : base(options) { }

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            var policy = await base.GetPolicyAsync(policyName);

            if (policy != null)
            {
                return policy;
            }

            // Nếu policyName bắt đầu bằng "Permission.", tự động tạo policy mới
            if (policyName.StartsWith("Permission.", StringComparison.OrdinalIgnoreCase))
            {
                var permission = policyName.Substring("Permission.".Length);
                return new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionRequirement(permission))
                    .Build();
            }

            return null;
        }
    }
}
