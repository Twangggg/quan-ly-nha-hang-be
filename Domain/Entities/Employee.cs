using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Employee
    {
        // Khóa chính: Tên gọi rõ ràng, dễ hiểu
        public Guid EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = null!;
        public string? Username { get; set; }
        public string PasswordHash { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public EmployeeRole Role { get; set; }
        public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeleteAt { get; set; }
        public virtual ICollection<AuditLog> TargetLogs { get; set; } = new List<AuditLog>();
        public virtual ICollection<AuditLog> PerformedLogs { get; set; } = new List<AuditLog>();
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public Employee() { }
    }
}
