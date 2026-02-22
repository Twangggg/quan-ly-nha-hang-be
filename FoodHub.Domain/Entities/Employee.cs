using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Employee : BaseEntity
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
        public virtual ICollection<AuditLog> TargetLogs { get; set; } = new List<AuditLog>();
        public virtual ICollection<AuditLog> PerformedLogs { get; set; } = new List<AuditLog>();
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } =
            new List<RefreshToken>();

        public Employee() { }

        public static bool IsManagerRole(EmployeeRole role)
        {
            return role == EmployeeRole.Manager;
        }

        public static bool IsDifferentRole(EmployeeRole CurrentRole, EmployeeRole NewRole)
        {
            return CurrentRole != NewRole;
        }

        public bool IsActive()
        {
            return Status == EmployeeStatus.Active;
        }

        public Employee ChangeRole(EmployeeRole newRole)
        {
            var timestamp = DateTime.UtcNow.Ticks;
            var originalEmail = Email;
            var originalUsername = Username;
            var originalPhone = Phone;
            Status = EmployeeStatus.Inactive;

            var suffix = $"_old_{timestamp}";
            if (originalEmail.Length + suffix.Length > 150)
            {
                Email = originalEmail.Substring(0, 150 - suffix.Length) + suffix;
            }
            else
            {
                Email = originalEmail + suffix;
            }

            Username = null;
            Phone = null;
            UpdatedAt = DateTime.UtcNow;

            return new Employee
            {
                EmployeeId = Guid.NewGuid(),
                FullName = FullName,
                Email = originalEmail,
                Username = originalUsername,
                PasswordHash = PasswordHash,
                Phone = originalPhone,
                Address = Address,
                DateOfBirth = DateOfBirth,
                Role = newRole,
                Status = EmployeeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
        }
    }
}
