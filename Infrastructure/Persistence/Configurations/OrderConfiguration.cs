using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(o => o.OrderId);
            builder.Property(o => o.OrderCode).HasMaxLength(30).IsRequired();
            builder.HasIndex(o => o.OrderCode).IsUnique();
            builder.Property(o => o.OrderType).IsRequired(); // Int enum by default
            builder.Property(o => o.Status).IsRequired();    // Int enum by default
            builder.Property(o => o.TotalAmount).HasColumnType("decimal(15,2)");
            builder.Property(o => o.Note).HasColumnType("text");

            // Relationships
            builder.HasOne(o => o.CreatedByEmployee)
                   .WithMany()
                   .HasForeignKey(o => o.CreatedBy);

            builder.Property(o => o.CreatedAt).HasDefaultValueSql("now()");
        }
    }
}
