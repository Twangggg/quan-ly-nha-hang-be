using FoodHub.Domain.Enums;

namespace FoodHub.Application.Interfaces
{
    public interface ICurrentUserService
    {
        string UserId { get; }
        EmployeeRole Role { get; }
        string? EmployeeCode { get; }
    }
}
