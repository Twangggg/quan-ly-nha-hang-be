using FoodHub.Domain.Entities;

namespace FoodHub.Application.Interfaces.Common
{
    public interface ITokenService
    {
        string GenerateAccessToken(Employee employee);
        string GenerateRefreshToken();
        int GetTokenExpirationSeconds();
        int GetRefreshTokenExpirationDays();
    }
}
