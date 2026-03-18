using FoodHub.Application.Interfaces;

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
                throw new UnauthorizedAccessException("User is not authenticated or UserId is invalid.");
            }

            return userId.Value;
        }
    }
}
