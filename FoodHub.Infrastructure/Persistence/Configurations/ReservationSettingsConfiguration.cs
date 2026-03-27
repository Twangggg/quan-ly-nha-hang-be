using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class ReservationSettingsConfiguration : IEntityTypeConfiguration<ReservationSettings>
    {
        public void Configure(EntityTypeBuilder<ReservationSettings> builder)
        {
            builder.ToTable("reservation_settings");

            builder.HasKey(x => x.ReservationSettingsId);
            builder.Property(x => x.ReservationSettingsId).HasColumnName("reservation_settings_id");

            builder
                .Property(x => x.OpenTime)
                .HasColumnName("open_time")
                .HasColumnType("time without time zone")
                .IsRequired();

            builder
                .Property(x => x.CloseTime)
                .HasColumnName("close_time")
                .HasColumnType("time without time zone")
                .IsRequired();

            builder
                .Property(x => x.BreakEnabled)
                .HasColumnName("break_enabled")
                .HasDefaultValue(true)
                .IsRequired();

            builder
                .Property(x => x.BreakStart)
                .HasColumnName("break_start")
                .HasColumnType("time without time zone")
                .IsRequired();

            builder
                .Property(x => x.BreakEnd)
                .HasColumnName("break_end")
                .HasColumnType("time without time zone")
                .IsRequired();

            builder
                .Property(x => x.SettingsKey)
                .HasColumnName("settings_key")
                .HasMaxLength(50)
                .IsRequired();

            builder
                .Property(x => x.OverlapBufferMinutes)
                .HasColumnName("overlap_buffer_minutes")
                .IsRequired();

            builder
                .Property(x => x.MinLeadTimeMinutes)
                .HasColumnName("min_lead_time_minutes")
                .IsRequired();

            builder
                .Property(x => x.GracePeriodMinutes)
                .HasColumnName("grace_period_minutes")
                .HasDefaultValue(15)
                .IsRequired();

            builder
                .Property(x => x.UpcomingBufferMinutes)
                .HasColumnName("upcoming_buffer_minutes")
                .HasDefaultValue(30)
                .IsRequired();

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
