namespace FoodHub.Infrastructure.Security
{
    public class JwtSettings
    {
        public string SecretKey { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public int ExpiresInMinute { get; set; } = 60; // 1 hour default
        public int RefreshTokenExpiresInDays { get; set; } = 7; // 7 days default
    }
}
