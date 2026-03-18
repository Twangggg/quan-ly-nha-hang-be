using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
    {
        public void Configure(EntityTypeBuilder<Reservation> builder)
        {
            builder.ToTable("reservations");

            builder.HasKey(r => r.ReservationId);
            builder.Property(r => r.ReservationId).HasColumnName("reservation_id");

            builder.Property(r => r.CustomerName)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("customer_name");

            builder.Property(r => r.CustomerPhone)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("customer_phone");

            builder.Property(r => r.ReservationDate)
                .IsRequired()
                .HasColumnName("reservation_date");

            builder.Property(r => r.ReservationTime)
                .IsRequired()
                .HasColumnName("reservation_time");


            builder.Property(r => r.GuestCount)
                .IsRequired()
                .HasColumnName("guest_count");


            builder.Property(r => r.Note)
                .HasMaxLength(500)
                .HasColumnName("note");

            builder.Property(r => r.Status)
                .IsRequired()
                .HasColumnName("status");

            builder.Property(r => r.AreaId).HasColumnName("area_id");
            builder.Property(r => r.TableId).IsRequired().HasColumnName("table_id");

            builder.Property(r => r.CreatedAt).HasColumnName("created_at");
            builder.Property(r => r.CreatedBy).HasColumnName("created_by");
            builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
            builder.Property(r => r.UpdatedBy).HasColumnName("updated_by");
            builder.Property(r => r.DeletedAt).HasColumnName("deleted_at");

            builder.HasOne(r => r.Area)
                .WithMany()
                .HasForeignKey(r => r.AreaId)
                .HasConstraintName("fk_reservations_areas_area_id")
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(r => r.Table)
                .WithMany(t => t.Reservations)
                .HasForeignKey(r => r.TableId)
                .HasConstraintName("fk_reservations_tables_table_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(r => !r.DeletedAt.HasValue);
        }
    }
}
