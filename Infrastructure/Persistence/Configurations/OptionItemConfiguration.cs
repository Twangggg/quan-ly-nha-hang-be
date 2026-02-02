using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class OptionItemConfiguration : IEntityTypeConfiguration<OptionItem>
    {
        public void Configure(EntityTypeBuilder<OptionItem> builder)
        {
            builder.HasKey(oi => oi.OptionItemId);

            builder.Property(oi => oi.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(oi => oi.AdditionalPrice)
                .HasPrecision(18, 2)
                .HasDefaultValue(0);

            builder.Property(oi => oi.IsActive)
                .HasDefaultValue(true);

            builder.Property(oi => oi.CreatedAt)
                .HasDefaultValueSql("now()");

            // Indexes
            builder.HasIndex(oi => oi.OptionGroupId);
            builder.HasIndex(oi => oi.IsActive);

            // Relationships
            builder.HasOne(oi => oi.OptionGroup)
                .WithMany(og => og.OptionItems)
                .HasForeignKey(oi => oi.OptionGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
