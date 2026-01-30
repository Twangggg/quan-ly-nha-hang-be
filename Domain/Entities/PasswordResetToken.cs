using System;

namespace FoodHub.Domain.Entities
{
    public class PasswordResetToken
    {
        public Guid TokenId { get; set; }                 // token_id
        public Guid EmployeeId { get; set; }              // employee_id

        public string TokenHash { get; set; } = default!; // token_hash (64)

        public DateTimeOffset ExpiresAt { get; set; }     // expires_at
        public DateTimeOffset? UsedAt { get; set; }       // used_at

        public DateTime CreatedAt { get; set; }           // created_at (default now())

        public Employee Employee { get; set; } = default!;
    }
}
