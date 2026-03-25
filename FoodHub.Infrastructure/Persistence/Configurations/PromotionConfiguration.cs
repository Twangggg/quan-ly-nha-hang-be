using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
    {
        public void Configure(EntityTypeBuilder<Promotion> builder)
        {
            builder.HasKey(p => p.PromotionId);

            builder.Property(p => p.Code).IsRequired().HasMaxLength(50);

            builder.HasIndex(p => p.Code).IsUnique();

            builder.Property(p => p.Value).HasPrecision(18, 2);

            builder.Property(p => p.MaxDiscount).HasPrecision(18, 2);

            builder.Property(p => p.MinOrderValue).HasPrecision(18, 2);

            builder
                .HasOne(p => p.Item)
                .WithMany()
                .HasForeignKey(p => p.ItemId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(p => p.IsActive).HasDefaultValue(true);

            builder.ToTable("Promotions");
        }
    }
}
