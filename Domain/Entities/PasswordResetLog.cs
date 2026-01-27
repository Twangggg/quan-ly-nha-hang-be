namespace FoodHub.Domain.Entities
{
    public class PasswordResetLog
    {
        public Guid LogId { get; set; }

        // Khóa ngoại trỏ đến Employee
        public Guid TargetEmployeeId { get; set; }
        public virtual Employee TargetEmployee { get; set; } = null!;
        public Guid PerformedByEmployeeId { get; set; }
        public Employee PerformedByEmployee { get; set; } = null!;
        public string? Reason { get; set; }
        public DateTimeOffset ResetAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
