using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace FoodHub.Infrastructure.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IServiceProvider _serviceProvider;

        public AuditLogService(ICurrentUserService currentUserService, IServiceProvider serviceProvider)
        {
            _currentUserService = currentUserService;
            _serviceProvider = serviceProvider;
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

        public async Task LogActivityAsync(AuditAction action, string entityName, string? entityId = null, object? oldValues = null, object? newValues = null)
        {
            var auditLog = new AuditLog
            {
                LogId = Guid.NewGuid(),
                EntityName = entityName,
                EntityId = entityId ?? string.Empty,
                Action = action,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                ActorInfo = GetActorInfo(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            // Resolve IUnitOfWork at runtime to break circular dependency (AppDbContext -> AuditLogService -> IUnitOfWork -> AppDbContext)
            var unitOfWork = _serviceProvider.GetRequiredService<IUnitOfWork>();
            await unitOfWork.Repository<AuditLog>().AddAsync(auditLog);
            await unitOfWork.SaveChangeAsync(CancellationToken.None);
        }
    }
}
