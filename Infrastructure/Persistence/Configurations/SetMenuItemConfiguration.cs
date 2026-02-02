using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class SetMenuItemConfiguration : IEntityTypeConfiguration<SetMenuItem>
    {
        public void Configure(EntityTypeBuilder<SetMenuItem> builder)
        {
            builder.HasKey(smi => smi.SetMenuItemId);

            builder.Property(smi => smi.Quantity)
                .IsRequired();

            builder.Property(smi => smi.CreatedAt)
                .HasDefaultValueSql("now()");

            // Composite index for uniqueness
            builder.HasIndex(smi => new { smi.SetMenuId, smi.MenuItemId }).IsUnique();

            // Relationships
            builder.HasOne(smi => smi.SetMenu)
                .WithMany(sm => sm.SetMenuItems)
                .HasForeignKey(smi => smi.SetMenuId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(smi => smi.MenuItem)
                .WithMany(m => m.SetMenuItems)
                .HasForeignKey(smi => smi.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
