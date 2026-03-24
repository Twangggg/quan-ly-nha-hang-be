using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
    {
        public void Configure(EntityTypeBuilder<Shift> builder)
        {
            builder.ToTable("shifts");

            builder.HasKey(s => s.ShiftId);
            builder.Property(s => s.ShiftId).HasColumnName("shift_id");

            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("name");

            builder.Property(s => s.StartTime)
                .IsRequired()
                .HasColumnName("start_time");

            builder.Property(s => s.EndTime)
                .IsRequired()
                .HasColumnName("end_time");

            builder.Property(s => s.Status)
                .IsRequired()
                .HasDefaultValue(ShiftStatus.Active)
                .HasColumnName("status");

            // Audit columns
            builder.Property(s => s.CreatedAt).HasColumnName("created_at");
            builder.Property(s => s.CreatedBy).HasColumnName("created_by");
            builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");
            builder.Property(s => s.UpdatedBy).HasColumnName("updated_by");
            builder.Property(s => s.DeletedAt).HasColumnName("deleted_at");

            // Soft-delete query filter
            builder.HasQueryFilter(s => !s.DeletedAt.HasValue);

            // Indexes
            builder.HasIndex(s => new { s.StartTime, s.EndTime })
                .HasFilter("deleted_at IS NULL")
                .IsUnique()
                .HasDatabaseName("idx_shifts_range");
        }
    }
}
