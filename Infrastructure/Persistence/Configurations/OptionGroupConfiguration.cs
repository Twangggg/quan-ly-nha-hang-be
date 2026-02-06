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

            builder.HasKey(e => e.OptionGroupId);
            builder.Property(e => e.OptionGroupId).HasColumnName("option_group_id");

            builder.Property(e => e.MenuItemId).HasColumnName("menu_item_id");

            builder.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.OptionType).HasColumnName("type");
            builder.Property(e => e.IsRequired).HasColumnName("is_required");

            // Audit Properties
            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            // Relationships
            builder.HasOne(e => e.MenuItem)
                .WithMany(m => m.OptionGroups)
                .HasForeignKey(e => e.MenuItemId)
                .HasConstraintName("fk_option_groups_menu_item_id")
                .OnDelete(DeleteBehavior.Cascade);



            // Indexes
            builder.HasIndex(e => e.MenuItemId).HasDatabaseName("idx_option_groups_menu_item_id");
        }
    }
}
