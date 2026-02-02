using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
    {
        public void Configure(EntityTypeBuilder<MenuItem> builder)
        {
            builder.HasKey(m => m.MenuItemId);

            builder.Property(m => m.Code)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(m => m.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(m => m.Description)
                .HasMaxLength(500);

            builder.Property(m => m.Price)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(m => m.Cost)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(m => m.ImageUrl)
                .HasMaxLength(500);

            builder.Property(m => m.IsActive)
                .HasDefaultValue(true);

            builder.Property(m => m.IsOutOfStock)
                .HasDefaultValue(false);

            builder.Property(m => m.CreatedAt)
                .HasDefaultValueSql("now()");

            // Indexes
            builder.HasIndex(m => m.Code).IsUnique();
            builder.HasIndex(m => m.Name);
            builder.HasIndex(m => m.CategoryId);
            builder.HasIndex(m => m.IsActive);
            builder.HasIndex(m => m.IsOutOfStock);

            // Relationships
            builder.HasOne(m => m.Category)
                .WithMany(c => c.MenuItems)
                .HasForeignKey(m => m.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(m => m.OptionGroups)
                .WithOne(og => og.MenuItem)
                .HasForeignKey(og => og.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(m => m.SetMenuItems)
                .WithOne(smi => smi.MenuItem)
                .HasForeignKey(smi => smi.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
