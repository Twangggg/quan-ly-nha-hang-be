using Microsoft.AspNetCore.Authorization;

namespace FoodHub.WebAPI.Presentation.Attributes
{
    public class HasPermissionAttribute : AuthorizeAttribute
    {
        public HasPermissionAttribute(string permission)
            : base(policy: $"Permission.{permission}") { }
    }
}
