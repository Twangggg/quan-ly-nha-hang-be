namespace FoodHub.Application.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        IEnumerable<string> Roles { get; }
        bool IsInRole(string role);
    }
}
