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

            // 1. Business Info
            builder.Property(x => x.LegalBusinessName).HasColumnName("legal_business_name").HasMaxLength(255).IsRequired();
            builder.Property(x => x.BrandName).HasColumnName("brand_name").HasMaxLength(255).IsRequired();
            builder.Property(x => x.TaxCode).HasColumnName("tax_code").HasMaxLength(20).IsRequired();
            builder.Property(x => x.BusinessRegistrationNumber).HasColumnName("business_registration_number").HasMaxLength(50).IsRequired();
            builder.Property(x => x.BranchCode).HasColumnName("branch_code").HasMaxLength(50).IsRequired();
            builder.Property(x => x.RestaurantCode).HasColumnName("restaurant_code").HasMaxLength(50).IsRequired();

            // 2. Contact Info
            builder.Property(x => x.Hotline).HasColumnName("hotline").HasMaxLength(50).IsRequired();
            builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            builder.Property(x => x.Website).HasColumnName("website").HasMaxLength(500).IsRequired();
            builder.Property(x => x.Facebook).HasColumnName("facebook").HasMaxLength(500).IsRequired();
            builder.Property(x => x.ZaloOa).HasColumnName("zalo_oa").HasMaxLength(500).IsRequired();
            builder.Property(x => x.Instagram).HasColumnName("instagram").HasMaxLength(500).IsRequired();

            // 3. Address
            builder.Property(x => x.Country).HasColumnName("country").HasMaxLength(100).IsRequired();
            builder.Property(x => x.ProvinceCity).HasColumnName("province_city").HasMaxLength(100).IsRequired();
            builder.Property(x => x.District).HasColumnName("district").HasMaxLength(100).IsRequired();
            builder.Property(x => x.Ward).HasColumnName("ward").HasMaxLength(100).IsRequired();
            builder.Property(x => x.StreetAddress).HasColumnName("street_address").HasMaxLength(255).IsRequired();
            builder.Property(x => x.PostalCode).HasColumnName("postal_code").HasMaxLength(20).IsRequired();
            builder.Property(x => x.GoogleMapUrl).HasColumnName("google_map_url").HasMaxLength(1000).IsRequired();

            // 4. Images
            builder.Property(x => x.CoverImageUrl).HasColumnName("cover_image_url").HasMaxLength(1000).IsRequired();
            builder.Property(x => x.QrPaymentImageUrl).HasColumnName("qr_payment_image_url").HasMaxLength(1000).IsRequired();
            builder.Property(x => x.FaviconUrl).HasColumnName("favicon_url").HasMaxLength(1000).IsRequired();

            // 5. Invoice Settings
            builder.Property(x => x.VatPercentage).HasColumnName("vat_percentage").HasColumnType("decimal(5,2)").IsRequired();

            // 6. Time Settings
            builder.Property(x => x.TimeFormat).HasColumnName("time_format").HasMaxLength(50).IsRequired();

            // 7. Operating Info
            builder.Property(x => x.OpeningTime).HasColumnName("opening_time").HasMaxLength(50).IsRequired();
            builder.Property(x => x.ClosingTime).HasColumnName("closing_time").HasMaxLength(50).IsRequired();
            builder.Property(x => x.WorkingDays).HasColumnName("working_days").HasMaxLength(255).IsRequired();

            // 8. System Config
            builder.Property(x => x.EnableOrdering).HasColumnName("enable_ordering").IsRequired();
            builder.Property(x => x.EnableDelivery).HasColumnName("enable_delivery").IsRequired();
            builder.Property(x => x.EnableTakeAway).HasColumnName("enable_take_away").IsRequired();
            builder.Property(x => x.EnableReservation).HasColumnName("enable_reservation").IsRequired();

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            builder.Property(x => x.CreatedBy).HasColumnName("created_by");
            builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

            builder.HasQueryFilter(x => x.DeletedAt == null);
            builder.HasIndex(x => x.SettingsKey).IsUnique().HasFilter("deleted_at IS NULL");
            builder.HasIndex(x => x.RestaurantCode).IsUnique().HasFilter("deleted_at IS NULL");
            builder.HasIndex(x => x.Email).HasFilter("deleted_at IS NULL");
            builder.HasIndex(x => x.TaxCode).HasFilter("deleted_at IS NULL");
        }
    }
}
