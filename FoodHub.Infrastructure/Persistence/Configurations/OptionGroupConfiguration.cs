using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class OptionGroupConfiguration : IEntityTypeConfiguration<OptionGroup>
    {
        public void Configure(EntityTypeBuilder<OptionGroup> builder)
        {
            builder.ToTable("option_groups");

            // Global Query Filter for Soft Delete
            builder.HasQueryFilter(e => e.DeletedAt == null);

            builder.HasKey(e => e.OptionGroupId);
            builder.Property(e => e.OptionGroupId).HasColumnName("option_group_id");

            builder.Property(e => e.MenuItemId).HasColumnName("menu_item_id");

            builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();

            builder.Property(e => e.OptionType).HasColumnName("type");
            builder.Property(e => e.IsRequired).HasColumnName("is_required");

            // Audit Properties from BaseEntity
            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            // Relationships
            builder
                .HasOne(e => e.MenuItem)
                .WithMany(m => m.OptionGroups)
                .HasForeignKey(e => e.MenuItemId)
                .HasConstraintName("fk_option_groups_menu_item_id")
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(e => e.MenuItemId).HasDatabaseName("idx_option_groups_menu_item_id");
            builder.HasIndex(e => e.OptionType);
            builder.HasIndex(e => e.IsRequired);

            // Composite index for common queries
            builder.HasIndex(e => new { e.MenuItemId, e.OptionType });
        }
    }
}
