using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class InventoryGroupConfiguration : IEntityTypeConfiguration<InventoryGroup>
    {
        public void Configure(EntityTypeBuilder<InventoryGroup> builder)
        {
            builder.ToTable("inventory_groups");

            builder.HasKey(e => e.InventoryGroupId);
            builder.Property(e => e.InventoryGroupId).HasColumnName("inventory_group_id");

            builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
            builder
                .Property(e => e.LowStockThreshold)
                .HasColumnName("low_stock_threshold")
                .HasPrecision(18, 2);
            builder.Property(e => e.ExpiryWarningDays).HasColumnName("expiry_warning_days");
            builder.Property(e => e.DefaultCostMethod).HasColumnName("default_cost_method");

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            builder.HasIndex(e => e.Name).IsUnique().HasFilter("deleted_at IS NULL");
        }
    }
}
