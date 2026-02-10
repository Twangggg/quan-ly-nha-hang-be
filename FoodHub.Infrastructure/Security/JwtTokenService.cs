using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FoodHub.Infrastructure.Security
{
    public class JwtTokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;

        public JwtTokenService(IOptions<JwtSettings> options)
        {
            _jwtSettings = options.Value;
        }

        public string GenerateAccessToken(Employee employee)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, employee.EmployeeId.ToString()),
                new(ClaimTypes.NameIdentifier, employee.EmployeeId.ToString()),
                new(ClaimTypes.Name, employee.FullName),
                new(ClaimTypes.Email, employee.Email),
                new(ClaimTypes.Role, employee.Role.ToString()),
                new("EmployeeCode", employee.EmployeeCode)
            };

            if (!string.IsNullOrEmpty(employee.Username))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.UniqueName, employee.Username));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinute),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public int GetTokenExpirationSeconds()
        {
            return _jwtSettings.ExpiresInMinute * 60;
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var randomNumberGenerator = System.Security.Cryptography.RandomNumberGenerator.Create();
            randomNumberGenerator.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public int GetRefreshTokenExpirationDays()
        {
            return _jwtSettings.RefreshTokenExpiresInDays;
        }
    }
}
