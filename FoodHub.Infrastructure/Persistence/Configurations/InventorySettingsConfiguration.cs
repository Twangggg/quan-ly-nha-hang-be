using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class InventorySettingsConfiguration : IEntityTypeConfiguration<InventorySettings>
    {
        public void Configure(EntityTypeBuilder<InventorySettings> builder)
        {
            builder.ToTable("inventory_settings");

            builder.HasKey(x => x.InventorySettingsId);
            builder.Property(x => x.InventorySettingsId).HasColumnName("inventory_settings_id");

            builder
                .Property(x => x.SettingsKey)
                .HasColumnName("settings_key")
                .HasMaxLength(50)
                .IsRequired();

            builder
                .Property(x => x.ExpiryWarningDays)
                .HasColumnName("expiry_warning_days")
                .IsRequired();

            builder
                .Property(x => x.DefaultLowStockThreshold)
                .HasColumnName("default_low_stock_threshold")
                .HasPrecision(18, 2)
                .IsRequired();

            builder
                .Property(x => x.AutoDeductOnCompleted)
                .HasColumnName("auto_deduct_on_completed")
                .IsRequired();

            builder.Property(x => x.CostMethod).HasColumnName("cost_method").IsRequired();

            builder
                .Property(x => x.MaxCostRecalcDays)
                .HasColumnName("max_cost_recalc_days")
                .IsRequired();

            builder
                .Property(x => x.OpeningStockImportCooldownHours)
                .HasColumnName("opening_stock_import_cooldown_hours")
                .IsRequired();

            builder
                .Property(x => x.OpeningStockStatus)
                .HasColumnName("opening_stock_status")
                .IsRequired();

            builder.Property(x => x.LockedAt).HasColumnName("locked_at");
            builder
                .Property(x => x.LastOpeningStockImportedAt)
                .HasColumnName("last_opening_stock_imported_at");

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            builder.Property(x => x.CreatedBy).HasColumnName("created_by");
            builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

            builder.HasQueryFilter(x => x.DeletedAt == null);

            builder.HasIndex(x => x.SettingsKey).IsUnique().HasFilter("deleted_at IS NULL");
        }
    }
}
