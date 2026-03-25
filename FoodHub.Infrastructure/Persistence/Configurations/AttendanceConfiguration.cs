using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Cấu hình EF Core cho bảng attendances.
    /// </summary>
    public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
    {
        public void Configure(EntityTypeBuilder<Attendance> builder)
        {
            builder.ToTable("attendances");

            builder.HasKey(a => a.AttendanceId);
            builder.Property(a => a.AttendanceId)
                .HasColumnName("attendance_id");

            builder.Property(a => a.EmployeeId)
                .HasColumnName("employee_id")
                .IsRequired();

            builder.Property(a => a.ShiftAssignmentId)
                .HasColumnName("shift_assignment_id");

            builder.Property(a => a.CheckInTime)
                .HasColumnName("check_in_time")
                .IsRequired();

            builder.Property(a => a.CheckOutTime)
                .HasColumnName("check_out_time");

            builder.Property(a => a.Note)
                .HasColumnName("note")
                .HasMaxLength(500);

            builder.Property(a => a.isLate)
                .HasColumnName("is_late");

            builder.Property(a => a.isEarlyLeave)
                .HasColumnName("is_early_leave");

            // BaseEntity audit columns
            builder.Property(a => a.CreatedAt).HasColumnName("created_at");
            builder.Property(a => a.CreatedBy).HasColumnName("created_by");
            builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
            builder.Property(a => a.UpdatedBy).HasColumnName("updated_by");
            builder.Property(a => a.DeletedAt).HasColumnName("deleted_at");

            // Soft delete global query filter
            builder.HasQueryFilter(a => a.DeletedAt == null);

            // FK: Employee
            builder.HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .HasConstraintName("fk_attendances_employee_id")
                .OnDelete(DeleteBehavior.Restrict);

            // FK: ShiftAssignment
            builder.HasOne(a => a.ShiftAssignment)
                .WithOne()
                .HasForeignKey<Attendance>(a => a.ShiftAssignmentId)
                .HasConstraintName("fk_attendances_shift_assignment_id")
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
