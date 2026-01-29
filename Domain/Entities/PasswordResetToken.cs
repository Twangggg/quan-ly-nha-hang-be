using System;

namespace FoodHub.Domain.Entities
{
    public class PasswordResetToken
    {
        public Guid TokenId { get; set; }
        public Guid EmployeeId { get; set; }
        public string TokenHash { get; set; } = null!;
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset? UsedAt { get; set; }
        public bool IsUsed => UsedAt.HasValue;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual Employee Employee { get; set; } = null!;
    }
}
