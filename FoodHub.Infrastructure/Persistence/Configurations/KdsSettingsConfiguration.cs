using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class KdsSettingsConfiguration : IEntityTypeConfiguration<KdsSettings>
    {
        public void Configure(EntityTypeBuilder<KdsSettings> builder)
        {
            builder.ToTable("kds_settings");

            builder.HasKey(x => x.KdsSettingsId);
            builder.Property(x => x.KdsSettingsId).HasColumnName("kds_settings_id");

            builder
                .Property(x => x.SettingsKey)
                .HasColumnName("settings_key")
                .HasMaxLength(50)
                .IsRequired();

            builder
                .Property(x => x.SortMode)
                .HasColumnName("sort_mode")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder
                .Property(x => x.WaitTimePerMinute)
                .HasColumnName("wait_time_per_minute")
                .IsRequired();

            builder
                .Property(x => x.OrderPriorityBonus)
                .HasColumnName("order_priority_bonus")
                .IsRequired();

            builder
                .Property(x => x.ExpectedTimeWeight)
                .HasColumnName("expected_time_weight")
                .IsRequired();

            builder
                .Property(x => x.OverduePerMinute)
                .HasColumnName("overdue_per_minute")
                .IsRequired();

            builder
                .Property(x => x.CompletionBoostWeight)
                .HasColumnName("completion_boost_weight")
                .IsRequired();

            builder
                .Property(x => x.TakeawayBonus)
                .HasColumnName("takeaway_bonus")
                .IsRequired();

            builder
                .Property(x => x.DeliveryBonus)
                .HasColumnName("delivery_bonus")
                .IsRequired();

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            builder.Property(x => x.CreatedBy).HasColumnName("created_by");
            builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

            builder.HasMany(x => x.StationWipLimits).WithOne().HasForeignKey("KdsSettingsId");

            builder.HasQueryFilter(x => x.DeletedAt == null);
            builder.HasIndex(x => x.SettingsKey).IsUnique().HasFilter("deleted_at IS NULL");
        }
    }
}
