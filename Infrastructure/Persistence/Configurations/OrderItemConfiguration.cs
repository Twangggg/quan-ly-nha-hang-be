using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.HasKey(oi => oi.OrderItemId);
            builder.Property(oi => oi.ItemCodeSnapshot).HasMaxLength(50).IsRequired();
            builder.Property(oi => oi.ItemNameSnapshot).HasMaxLength(150).IsRequired();
            builder.Property(oi => oi.StationSnapshot).HasMaxLength(20);

            builder.Property(oi => oi.UnitPriceSnapshot).HasColumnType("decimal(15,2)");
            builder.Property(oi => oi.ItemNote).HasMaxLength(255);
            builder.HasOne(oi => oi.Order)
                   .WithMany(o => o.OrderItems)
                   .HasForeignKey(oi => oi.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Property(oi => oi.CreatedAt).HasDefaultValueSql("now()");
        }
    }
}
