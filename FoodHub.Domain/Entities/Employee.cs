using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Employee : BaseEntity
    {
        private const int EmployeeEmailMaxLength = 150;
        private const int EmployeeUsernameMaxLength = 50;
        private const int EmployeePhoneMaxLength = 15;

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

        public static bool IsDifferentRole(EmployeeRole currentRole, EmployeeRole newRole)
        {
            return currentRole != newRole;
        }

        public bool IsActive()
        {
            return Status == EmployeeStatus.Active;
        }

        public void UpdateDetails(
            string fullName,
            string? username,
            string? phone,
            string? address,
            DateOnly? dateOfBirth,
            EmployeeStatus? status,
            Guid? updatedBy = null,
            DateTime? updatedAt = null
        )
        {
            FullName = fullName;
            Username = NormalizeOptional(username);
            Phone = NormalizeOptional(phone);
            Address = NormalizeOptional(address);
            DateOfBirth = dateOfBirth;

            if (status.HasValue)
            {
                Status = status.Value;
            }

            UpdatedAt = updatedAt ?? DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        public void UpdateProfile(
            string fullName,
            string email,
            string? phone,
            string? address,
            DateOnly? dateOfBirth,
            Guid? updatedBy = null,
            DateTime? updatedAt = null
        )
        {
            FullName = fullName;
            Email = email;
            Phone = NormalizeOptional(phone);
            Address = NormalizeOptional(address);
            DateOfBirth = dateOfBirth;
            UpdatedAt = updatedAt ?? DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        public void ResetPassword(
            string passwordHash,
            Guid? updatedBy = null,
            DateTime? updatedAt = null
        )
        {
            PasswordHash = passwordHash;
            UpdatedAt = updatedAt ?? DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        public Employee ChangeRole(EmployeeRole newRole)
        {
            if (!IsActive())
            {
                throw new InvalidOperationException("Only active employees can change role.");
            }

            if (newRole == Role)
            {
                throw new InvalidOperationException("New role must be different.");
            }

            if (newRole == EmployeeRole.Manager)
            {
                throw new InvalidOperationException("Promoting to manager is not allowed.");
            }

            var timestamp = DateTime.UtcNow.Ticks;
            var originalEmail = Email;
            var originalUsername = Username;
            var originalPhone = Phone;
            Status = EmployeeStatus.Inactive;

            var suffix = $"_old_{timestamp}";
            if (originalEmail != null)
            {
                Email =
                    originalEmail.Length + suffix.Length > EmployeeEmailMaxLength
                        ? originalEmail.Substring(0, EmployeeEmailMaxLength - suffix.Length)
                            + suffix
                        : originalEmail + suffix;
            }

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
            if (!IsActive())
            {
                throw new InvalidOperationException("Only active employees can be deactivated.");
            }

            var shortId = Guid.NewGuid().ToString("N")[..8];
            var originalEmail = Email;
            var originalPhone = Phone ?? string.Empty;
            var originalUsername = Username ?? string.Empty;
            Status = EmployeeStatus.Inactive;

            var suffix = $"-del{shortId}";

            if (!string.IsNullOrEmpty(originalEmail))
            {
                Email =
                    originalEmail.Length + suffix.Length > EmployeeEmailMaxLength
                        ? originalEmail[..(EmployeeEmailMaxLength - suffix.Length)] + suffix
                        : originalEmail + suffix;
            }

            if (!string.IsNullOrEmpty(originalPhone))
            {
                Phone =
                    originalPhone.Length + suffix.Length > EmployeePhoneMaxLength
                        ? originalPhone[..(EmployeePhoneMaxLength - suffix.Length)] + suffix
                        : originalPhone + suffix;
            }

            if (!string.IsNullOrEmpty(originalUsername))
            {
                Username =
                    originalUsername.Length + suffix.Length > EmployeeUsernameMaxLength
                        ? originalUsername[..(EmployeeUsernameMaxLength - suffix.Length)] + suffix
                        : originalUsername + suffix;
            }

            UpdatedAt = DateTime.UtcNow;
            DeletedAt = DateTime.UtcNow;
            UpdatedBy = auditorId;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
