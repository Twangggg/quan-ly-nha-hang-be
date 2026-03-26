using System.Security.Claims;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace FoodHub.Infrastructure.Security
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IPermissionProvider _permissionProvider;

        public PermissionHandler(IPermissionProvider permissionProvider)
        {
            _permissionProvider = permissionProvider;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement
        )
        {
            // 1. Kiểm tra Permission từ Claims (Legacy - Giữ lại để ổn định token cũ)
            var permissionsInClaims = context
                .User.FindAll(c => c.Type == "Permission")
                .Select(c => c.Value);

            if (permissionsInClaims.Contains(requirement.Permission))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // 2. Kiểm tra Role từ Claims và lấy Permission tương ứng từ Provider (Optimization)
            var roleClaim = context.User.FindFirst(c => c.Type == ClaimTypes.Role);
            if (roleClaim != null && Enum.TryParse<EmployeeRole>(roleClaim.Value, out var role))
            {
                var permissionsByRole = _permissionProvider.GetPermissionsByRole(role);
                if (permissionsByRole.Contains(requirement.Permission))
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}
