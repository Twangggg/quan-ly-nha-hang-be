using System.Security.Claims;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Enums;

namespace FoodHub.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public EmployeeRole Role => (EmployeeRole)Enum.Parse(typeof(EmployeeRole), _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role) ?? string.Empty);

        public string? EmployeeCode => _httpContextAccessor.HttpContext?.User?.FindFirstValue("EmployeeCode");
    }
}
