using FoodHub.Domain.Entities;

namespace FoodHub.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(Employee employee);
<<<<<<< HEAD
        string GenerateRefreshToken();
        int GetTokenExpirationSeconds();
        int GetRefreshTokenExpirationDays();
=======
>>>>>>> origin/feature/profile-nhudm
    }
}
