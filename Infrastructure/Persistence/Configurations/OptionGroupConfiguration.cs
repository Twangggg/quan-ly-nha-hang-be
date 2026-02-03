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

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("option_group_id");

            builder.Property(e => e.MenuItemId).HasColumnName("menu_item_id");

            builder.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.Type).HasColumnName("type");
            builder.Property(e => e.IsRequired).HasColumnName("is_required");

            // Relationships
            builder.HasOne(e => e.MenuItem)
                .WithMany(m => m.OptionGroups)
                .HasForeignKey(e => e.MenuItemId)
                .HasConstraintName("fk_option_groups_menu_item_id")
                .OnDelete(DeleteBehavior.Cascade);

            // BaseEntity
            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            // Indexes
            builder.HasIndex(e => e.MenuItemId).HasDatabaseName("idx_option_groups_menu_item_id");
        }
    }
}
