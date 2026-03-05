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
            if (originalEmail != null)
            {
                Email =
                    originalEmail.Length + suffix.Length > 150
                        ? originalEmail.Substring(0, 150 - suffix.Length) + suffix
                        : originalEmail + suffix;
            }

            // RELEASE Username and Phone for the new record
            Username = null;
            Phone = null;

            UpdatedAt = DateTime.UtcNow;

            return new Employee
            {
                EmployeeId = Guid.NewGuid(),
                FullName = FullName,
                Email = originalEmail!,
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

        public void DeleteEmployee(Guid auditorId)
        {
            var shortId = Guid.NewGuid().ToString("N")[..8];
            var originalEmail = Email;
            var originalPhone = Phone ?? string.Empty;
            var originalUsername = Username ?? string.Empty;
            Status = EmployeeStatus.Inactive;

            var suffix = $"-del{shortId}";

            if (!string.IsNullOrEmpty(originalEmail))
            {
                Email =
                    originalEmail.Length + suffix.Length > 150
                        ? originalEmail[..(150 - suffix.Length)] + suffix
                        : originalEmail + suffix;
            }

            if (!string.IsNullOrEmpty(originalPhone))
            {
                Phone =
                    originalPhone.Length + suffix.Length > 15
                        ? originalPhone[..(15 - suffix.Length)] + suffix
                        : originalPhone + suffix;
            }

            if (!string.IsNullOrEmpty(originalUsername))
            {
                Username =
                    originalUsername.Length + suffix.Length > 50
                        ? originalUsername[..(50 - suffix.Length)] + suffix
                        : originalUsername + suffix;
            }

            UpdatedAt = DateTime.UtcNow;
            DeletedAt = DateTime.UtcNow;
            UpdatedBy = auditorId;
        }
    }
}
