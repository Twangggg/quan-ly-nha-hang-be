namespace FoodHub.Application.DTOs.Authentication
{
    public class LoginResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
<<<<<<< HEAD
        public string EmployeeCode { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string RefreshToken { get; set; } = string.Empty;
        public double RefreshTokenExpiresIn { get; set; } // Duration in Seconds
        public int ExpiresIn { get; set; } // Access Token Duration in Seconds
=======
        public string TokenType { get; set; } = string.Empty;
        public EmployeeInfoDto User { get; set; } = null!;
>>>>>>> origin/feature/profile-nhudm
    }
}
