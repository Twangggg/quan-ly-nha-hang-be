using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class BrandingSettingsConfiguration : IEntityTypeConfiguration<BrandingSettings>
    {
        public void Configure(EntityTypeBuilder<BrandingSettings> builder)
        {
            builder.ToTable("branding_settings");

            builder.HasKey(x => x.BrandingSettingsId);
            builder.Property(x => x.BrandingSettingsId).HasColumnName("branding_settings_id");

            builder.Property(x => x.SettingsKey).HasColumnName("settings_key").HasMaxLength(50).IsRequired();
            builder.Property(x => x.RestaurantName).HasColumnName("restaurant_name").HasMaxLength(200).IsRequired();
            builder.Property(x => x.BranchName).HasColumnName("branch_name").HasMaxLength(200).IsRequired();
            builder.Property(x => x.Address).HasColumnName("address").HasMaxLength(500).IsRequired();
            builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(50).IsRequired();
            builder.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(10).IsRequired();
            builder.Property(x => x.DateFormat).HasColumnName("date_format").HasMaxLength(50).IsRequired();
            builder.Property(x => x.Timezone).HasColumnName("timezone").HasMaxLength(100).IsRequired();
            builder.Property(x => x.Language).HasColumnName("language").HasMaxLength(10).IsRequired();
            builder.Property(x => x.BillTitle).HasColumnName("bill_title").HasMaxLength(200).IsRequired();
            builder.Property(x => x.BillFooter).HasColumnName("bill_footer").HasMaxLength(500).IsRequired();
            builder.Property(x => x.KdsTitle).HasColumnName("kds_title").HasMaxLength(200).IsRequired();
            builder.Property(x => x.AppTitle).HasColumnName("app_title").HasMaxLength(200).IsRequired();
            builder.Property(x => x.LogoUrl).HasColumnName("logo_url").HasMaxLength(1000).IsRequired();

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
