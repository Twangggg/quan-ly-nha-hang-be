using System.Security.Cryptography;
using System.Text;

namespace FoodHub.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public Guid EmployeeId { get; set; }
        public virtual Employee Employee { get; set; } = null!;

        /// <summary>
        /// Policy: RememberMe → 30 ngày, còn lại → theo config (mặc định 7 ngày)
        /// </summary>
        public static RefreshToken Create(
            Guid employeeId,
            string token,
            bool rememberMe,
            int configDays
        )
        {
            var expirationDays = rememberMe ? 30 : configDays;

            return CreateWithDays(employeeId, token, expirationDays);
        }

        public static RefreshToken CreateWithDays(
            Guid employeeId,
            string token,
            int expirationDays
        )
        {
            var hashedToken = HashToken(token);

            return new RefreshToken
            {
                Token = hashedToken,
                Expires = DateTime.UtcNow.AddDays(expirationDays),
                EmployeeId = employeeId,
            };
        }

        public void Revoke(DateTime? revokedAt = null)
        {
            if (IsRevoked)
            {
                return;
            }

            IsRevoked = true;
            UpdatedAt = revokedAt ?? DateTime.UtcNow;
        }

        public static string HashToken(string token)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(token);

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }
    }
}
