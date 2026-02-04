using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class SetMenuItemConfiguration : IEntityTypeConfiguration<SetMenuItem>
    {
        public void Configure(EntityTypeBuilder<SetMenuItem> builder)
        {
            builder.ToTable("set_menu_items");

            builder.HasKey(e => e.SetMenuItemId);
            builder.Property(e => e.SetMenuItemId).HasColumnName("set_menu_item_id");

            builder.Property(e => e.SetMenuId).HasColumnName("set_menu_id");
            builder.Property(e => e.MenuItemId).HasColumnName("menu_item_id");

            builder.Property(e => e.Quantity).HasColumnName("quantity");

            // Relationships
            builder.HasOne(e => e.SetMenu)
                .WithMany(s => s.SetMenuItems)
                .HasForeignKey(e => e.SetMenuId)
                .HasConstraintName("fk_set_menu_items_set_menu_id")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.MenuItem)
                .WithMany(m => m.SetMenuItems)
                .HasForeignKey(e => e.MenuItemId)
                .HasConstraintName("fk_set_menu_items_menu_item_id")
                .OnDelete(DeleteBehavior.Restrict);



            // Indexes
            builder.HasIndex(e => e.SetMenuId).HasDatabaseName("idx_set_menu_items_set_menu_id");
            builder.HasIndex(e => e.MenuItemId).HasDatabaseName("idx_set_menu_items_menu_item_id");
        }
    }
}
