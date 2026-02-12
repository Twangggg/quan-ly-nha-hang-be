using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FoodHub.Infrastructure.Security
{
    public class JwtTokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IPermissionProvider _permissionProvider;

        public JwtTokenService(
            IOptions<JwtSettings> options,
            IPermissionProvider permissionProvider
        )
        {
            _jwtSettings = options.Value;
            _permissionProvider = permissionProvider;
        }

        public string GenerateAccessToken(Employee employee)
        {
            var permissions = _permissionProvider.GetPermissionsByRole(employee.Role);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, employee.EmployeeId.ToString()),
                new(ClaimTypes.NameIdentifier, employee.EmployeeId.ToString()),
                new(ClaimTypes.Name, employee.FullName),
                new(ClaimTypes.Email, employee.Email),
                new(ClaimTypes.Role, employee.Role.ToString()),
                new("EmployeeCode", employee.EmployeeCode),
            };

            // Thêm các quyền cụ thể vào Token
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("Permission", permission));
            }

            if (!string.IsNullOrEmpty(employee.Username))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.UniqueName, employee.Username));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

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
            using var randomNumberGenerator =
                System.Security.Cryptography.RandomNumberGenerator.Create();
            randomNumberGenerator.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public int GetRefreshTokenExpirationDays()
        {
            return _jwtSettings.RefreshTokenExpiresInDays;
        }
    }
}
