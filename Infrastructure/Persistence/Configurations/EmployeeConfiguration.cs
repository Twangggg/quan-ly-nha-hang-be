using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FoodHub.Domain.Enums;

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

            // Cấu hình Enum: Lưu dưới dạng số (SmallInt) trong Postgres để tối ưu
            builder.Property(x => x.Role)
                   .HasConversion<short>()
                   .IsRequired();

            builder.Property(x => x.Status)
                   .HasConversion<short>()
                   .HasDefaultValue(EmployeeStatus.Active)
                   .HasSentinel(EmployeeStatus.Inactive);

            builder.Property(x => x.CreatedAt)
                   .HasDefaultValueSql("now()");

            builder.HasIndex(x => x.Role);
            builder.HasIndex(x => x.Status);

            // Global Query Filter: Hide soft-deleted employees
            builder.HasQueryFilter(x => x.DeleteAt == null);
        }
    }
}
