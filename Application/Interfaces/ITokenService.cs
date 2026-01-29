using FoodHub.Domain.Entities;

namespace FoodHub.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(Employee employee);
        string GenerateRefreshToken();
        int GetTokenExpirationSeconds();
        int GetRefreshTokenExpirationDays();
    }
}
