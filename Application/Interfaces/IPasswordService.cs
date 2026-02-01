namespace FoodHub.Application.Interfaces
{
    public interface IPasswordService
    {
        string GenerateRandomPassword();
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
    }
}
