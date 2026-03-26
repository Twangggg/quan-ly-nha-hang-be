using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Cấu hình EF Core cho bảng shift_assignments.
    /// </summary>
    public class ShiftAssignmentConfiguration : IEntityTypeConfiguration<ShiftAssignment>
    {
        public void Configure(EntityTypeBuilder<ShiftAssignment> builder)
        {
            builder.ToTable("shift_assignments");

            builder.HasKey(a => a.ShiftAssignmentId);
            builder.Property(a => a.ShiftAssignmentId)
                .HasColumnName("shift_assignment_id");

            builder.Property(a => a.EmployeeId)
                .HasColumnName("employee_id")
                .IsRequired();

            builder.Property(a => a.ShiftId)
                .HasColumnName("shift_id")
                .IsRequired();

            builder.Property(a => a.AssignedDate)
                .HasColumnName("assigned_date")
                .HasColumnType("date")
                .IsRequired();

            builder.Property(a => a.Note)
                .HasColumnName("note")
                .HasMaxLength(500);

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
                .HasConstraintName("fk_shift_assignments_employee_id")
                .OnDelete(DeleteBehavior.Restrict);

            // FK: Shift
            builder.HasOne(a => a.Shift)
                .WithMany(s => s.ShiftAssignments)
                .HasForeignKey(a => a.ShiftId)
                .HasConstraintName("fk_shift_assignments_shift_id")
                .OnDelete(DeleteBehavior.Restrict);

            // Unique index: một nhân viên chỉ có một phân công cho cùng ca + ngày
            builder.HasIndex(a => new { a.EmployeeId, a.ShiftId, a.AssignedDate })
                .IsUnique()
                .HasFilter("deleted_at IS NULL")
                .HasDatabaseName("uq_shift_assignments_employee_shift_date");
        }
    }
}
