using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.HasKey(e => e.EmployeeId);

            builder.Property(e => e.EmployeeCode).IsRequired().HasMaxLength(10);
            builder.HasIndex(e => e.EmployeeCode).IsUnique();

            builder.Property(e => e.Username).HasMaxLength(50).IsRequired(false);
            builder.HasIndex(e => e.Username).IsUnique();

            builder.Property(e => e.Email).IsRequired().HasMaxLength(150);
            builder.HasIndex(e => e.Email).IsUnique();

            builder.Property(e => e.Phone).IsRequired(false).HasMaxLength(15);
            builder.HasIndex(e => e.Phone).IsUnique();

            builder.Property(e => e.FullName).IsRequired().HasMaxLength(100);

            builder.Property(e => e.Address).HasMaxLength(255);
            builder.Property(e => e.PasswordHash).IsRequired();

            // C?u hình Enum: Luu du?i d?ng s? (SmallInt) trong Postgres d? t?i uu
            builder.Property(x => x.Role)
                   .HasConversion<short>()
                   .IsRequired();

            builder.Property(x => x.Status)
                   .HasConversion<short>()
                   .HasDefaultValue(EmployeeStatus.Active)
                   .HasSentinel(EmployeeStatus.Inactive);

            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            // Global Query Filter: Hide soft-deleted employees
            builder.HasQueryFilter(x => x.DeletedAt == null);

            // Indexes for Optimization
            builder.HasIndex(e => e.Status);
            builder.HasIndex(e => e.Role);
            builder.HasIndex(e => e.CreatedAt);

            // Composite indexes for common queries
            builder.HasIndex(e => new { e.Status, e.Role });
            builder.HasIndex(e => new { e.Role, e.Status });

            // Filtered index for active employees
            builder.HasIndex(e => e.Status)
                .HasFilter("deleted_at IS NULL");
        }
    }
}
