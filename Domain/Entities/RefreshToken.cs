using System;

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
    }
}
