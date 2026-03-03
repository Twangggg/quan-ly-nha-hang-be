using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class TableConfiguration : IEntityTypeConfiguration<Table>
    {
        public void Configure(EntityTypeBuilder<Table> builder)
        {
            builder.HasKey(t => t.TableId);
            builder.Property(t => t.TableNumber).HasMaxLength(20).IsRequired();
            builder.HasIndex(t => t.TableNumber).IsUnique();
            builder.Property(t => t.Status).IsRequired();

            // Default values
            builder.Property(t => t.Capacity).HasDefaultValue(4);

            // Audit Properties from BaseEntity
            builder.Property(t => t.CreatedAt).HasDefaultValueSql("now()");
            builder.Property(t => t.CreatedBy).HasColumnName("created_by");
            builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
            builder.Property(t => t.UpdatedBy).HasColumnName("updated_by");
            builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");

            // Global Query Filter for Soft Delete
            builder.HasQueryFilter(t => t.DeletedAt == null);
        }
    }
}
