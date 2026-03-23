using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class MenuItemOptionGroupConfiguration : IEntityTypeConfiguration<MenuItemOptionGroup>
    {
        public void Configure(EntityTypeBuilder<MenuItemOptionGroup> builder)
        {
            builder.ToTable("menu_item_option_groups");

            builder.HasQueryFilter(e => e.DeletedAt == null);

            builder.HasKey(e => e.MenuItemOptionGroupId);
            builder.Property(e => e.MenuItemOptionGroupId).HasColumnName("menu_item_option_group_id");
            builder.Property(e => e.MenuItemId).HasColumnName("menu_item_id").IsRequired();
            builder.Property(e => e.OptionGroupId).HasColumnName("option_group_id").IsRequired();
            builder.Property(e => e.IsRequired).HasColumnName("is_required");
            builder.Property(e => e.MinSelect).HasColumnName("min_select");
            builder.Property(e => e.MaxSelect).HasColumnName("max_select");
            builder.Property(e => e.SortOrder).HasColumnName("sort_order");
            builder.Property(e => e.IsVisible).HasColumnName("is_visible");

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            builder
                .HasOne(e => e.MenuItem)
                .WithMany(m => m.MenuItemOptionGroups)
                .HasForeignKey(e => e.MenuItemId)
                .HasConstraintName("fk_menu_item_option_groups_menu_item_id")
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(e => e.OptionGroup)
                .WithMany(og => og.MenuItemOptionGroups)
                .HasForeignKey(e => e.OptionGroupId)
                .HasConstraintName("fk_menu_item_option_groups_option_group_id")
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasIndex(e => new { e.MenuItemId, e.OptionGroupId })
                .IsUnique()
                .HasFilter("deleted_at IS NULL");
            builder.HasIndex(e => e.MenuItemId).HasDatabaseName("idx_menu_item_option_groups_menu_item_id");
            builder.HasIndex(e => e.OptionGroupId).HasDatabaseName("idx_menu_item_option_groups_option_group_id");
        }
    }
}
