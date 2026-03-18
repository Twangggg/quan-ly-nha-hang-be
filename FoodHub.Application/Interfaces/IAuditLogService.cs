using FoodHub.Domain.Enums;

namespace FoodHub.Application.Interfaces
{
    public interface IAuditLogService
    {
        string GetActorInfo();
        Task LogActivityAsync(AuditAction action, string entityName, string? entityId = null, object? oldValues = null, object? newValues = null);
    }
}
