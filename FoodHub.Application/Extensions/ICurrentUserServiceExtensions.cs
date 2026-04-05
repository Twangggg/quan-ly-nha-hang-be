using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Application.Constants;

namespace FoodHub.Application.Extensions
{
    public static class ICurrentUserServiceExtensions
    {
        public static Guid? GetUserIdAsGuid(this ICurrentUserService currentUserService)
        {
            if (Guid.TryParse(currentUserService.UserId, out var userId))
            {
                return userId;
            }

            return null;
        }

        public static Guid GetRequiredUserIdAsGuid(this ICurrentUserService currentUserService)
        {
            var userId = currentUserService.GetUserIdAsGuid();
            if (userId == null)
            {
                throw new UnauthorizedAccessException(MessageKeys.Common.Unauthorized);
            }

            return userId.Value;
        }
    }
}
