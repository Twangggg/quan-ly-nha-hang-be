using System.Security.Cryptography;
using FoodHub.Application.Interfaces;

namespace FoodHub.Infrastructure.Security
{
    public class PasswordService : IPasswordService
    {
        private static readonly char[] _alphanumericChars =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
        private static readonly char[] _specialChars = "!@#$%^&*()_-+=".ToCharArray();

        public string GenerateRandomPassword()
        {
            // Policy: 8 alphanumeric + 1 special character
            int alphaLength = 8;
            int specialLength = 1;
            char[] password = new char[alphaLength + specialLength];
            int index = 0;

            // Generate 8 alphanumeric characters
            for (int i = 0; i < alphaLength; i++)
            {
                password[index++] = _alphanumericChars[
                    RandomNumberGenerator.GetInt32(_alphanumericChars.Length)
                ];
            }

            // Generate 1 special character
            for (int i = 0; i < specialLength; i++)
            {
                password[index++] = _specialChars[
                    RandomNumberGenerator.GetInt32(_specialChars.Length)
                ];
            }

            // Shuffle the result using Fisher-Yates algorithm to mix the special char
            int n = password.Length;
            while (n > 1)
            {
                n--;
                int k = RandomNumberGenerator.GetInt32(n + 1);
                var value = password[k];
                password[k] = password[n];
                password[n] = value;
            }

            return new string(password);
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
    }
}
