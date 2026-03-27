using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class KdsStationWipLimitConfiguration : IEntityTypeConfiguration<KdsStationWipLimit>
    {
        public void Configure(EntityTypeBuilder<KdsStationWipLimit> builder)
        {
            builder.ToTable("kds_station_wip_limits");

            builder.HasKey(x => x.KdsStationWipLimitId);
            builder
                .Property(x => x.KdsStationWipLimitId)
                .HasColumnName("kds_station_wip_limit_id");

            builder.Property<Guid>("KdsSettingsId").HasColumnName("kds_settings_id").IsRequired();

            builder
                .Property(x => x.Station)
                .HasColumnName("station")
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(x => x.Limit).HasColumnName("limit").IsRequired();
            builder.Property(x => x.Enabled).HasColumnName("enabled").IsRequired();

            builder.HasIndex("KdsSettingsId", nameof(KdsStationWipLimit.Station)).IsUnique();
        }
    }
}
