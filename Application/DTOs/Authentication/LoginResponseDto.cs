namespace FoodHub.Application.DTOs.Authentication
{
    public class LoginResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public EmployeeInfoDto User { get; set; } = null!;
    }
}
