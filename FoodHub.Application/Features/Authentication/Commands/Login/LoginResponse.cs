using System;

namespace FoodHub.Application.Features.Authentication.Commands.Login
{
    public class LoginResponse
    {
        public Guid EmployeeId { get; set; }
        public string AccessToken { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public double RefreshTokenExpiresIn { get; set; } // Duration in Seconds
        public int ExpiresIn { get; set; } // Access Token Duration in Seconds
    }
}
