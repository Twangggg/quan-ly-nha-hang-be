using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
    {
        public void Configure(EntityTypeBuilder<Reservation> builder)
        {
            builder.HasKey(r => r.ReservationId);
            builder.Property(r => r.ReservationId).ValueGeneratedOnAdd();
            builder.Property(r => r.CustomerName).IsRequired().HasMaxLength(100);
            builder.Property(r => r.CustomerPhone).IsRequired().HasMaxLength(20);
            builder.Property(r => r.ReservationDate).IsRequired();
            builder.Property(r => r.ReservationTime).IsRequired();
            builder.Property(r => r.PartyType).IsRequired();
            builder.Property(r => r.GuestCount).IsRequired();
            builder.Property(r => r.Note).HasMaxLength(500);
            builder.Property(r => r.Status).IsRequired();

            // Relationships
            builder.HasOne(r => r.Table)
                .WithMany(t => t.Reservations)
                .HasForeignKey(r => r.TableId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
