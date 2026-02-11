namespace FoodHub.Application.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? EmployeeCode { get; }
        string? Role { get; }
        bool IsAuthenticated { get; }
        string? IpAddress { get; }
    }
}
