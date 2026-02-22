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

            return new RefreshToken
            {
                Token = token,
                Expires = DateTime.UtcNow.AddDays(expirationDays),
                EmployeeId = employeeId,
            };
        }
    }
}
