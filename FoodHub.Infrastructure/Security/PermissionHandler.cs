using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace FoodHub.Infrastructure.Security
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement
        )
        {
            // Lấy tất cả các Claim có type là "Permission" từ Token
            var permissions = context
                .User.FindAll(c => c.Type == "Permission")
                .Select(c => c.Value);

            // Kiểm tra xem user có Permission yêu cầu hay không
            if (permissions.Contains(requirement.Permission))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
