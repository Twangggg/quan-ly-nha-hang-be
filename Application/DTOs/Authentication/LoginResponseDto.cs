namespace FoodHub.Application.DTOs.Authentication
{
    public class LoginResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int ExpiresIn { get; set; } // Seconds
    }
}
