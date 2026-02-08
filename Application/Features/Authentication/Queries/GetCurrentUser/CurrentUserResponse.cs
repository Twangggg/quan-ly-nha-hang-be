namespace FoodHub.Application.Features.Authentication.Queries.GetCurrentUser
{
    public class CurrentUserResponse
    {
        public string EmployeeCode { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}
