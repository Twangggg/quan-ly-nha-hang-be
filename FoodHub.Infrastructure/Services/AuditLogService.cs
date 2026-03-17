using FoodHub.Application.Interfaces;
using System.Text.Json;

namespace FoodHub.Infrastructure.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ICurrentUserService _currentUserService;

        public AuditLogService(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        public string GetActorInfo()
        {
            if (_currentUserService.IsAuthenticated)
            {
                return JsonSerializer.Serialize(new
                {
                    type = "Employee",
                    id = _currentUserService.UserId,
                    code = _currentUserService.EmployeeCode,
                    ip = _currentUserService.IpAddress
                });
            }

            return JsonSerializer.Serialize(new
            {
                type = "System/Public",
                ip = _currentUserService.IpAddress
            });
        }
    }
}
