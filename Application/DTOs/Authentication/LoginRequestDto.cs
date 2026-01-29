namespace FoodHub.Application.DTOs.Authentication
{
    public class LoginRequestDto
    {
        public string EmployeeCode { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
}
