using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class OptionGroupConfiguration : IEntityTypeConfiguration<OptionGroup>
    {
        public void Configure(EntityTypeBuilder<OptionGroup> builder)
        {
            builder.HasKey(og => og.OptionGroupId);

            builder.Property(og => og.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(og => og.MinSelect)
                .HasDefaultValue(0);

            builder.Property(og => og.MaxSelect)
                .HasDefaultValue(1);

            builder.Property(og => og.IsRequired)
                .HasDefaultValue(false);

            builder.Property(og => og.CreatedAt)
                .HasDefaultValueSql("now()");

            // Indexes
            builder.HasIndex(og => og.MenuItemId);

            // Relationships
            builder.HasOne(og => og.MenuItem)
                .WithMany(m => m.OptionGroups)
                .HasForeignKey(og => og.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(og => og.OptionItems)
                .WithOne(oi => oi.OptionGroup)
                .HasForeignKey(oi => oi.OptionGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
