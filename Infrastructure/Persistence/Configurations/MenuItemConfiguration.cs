using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
    {
        public void Configure(EntityTypeBuilder<MenuItem> builder)
        {
            builder.ToTable("menu_items");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("menu_item_id");

            builder.Property(e => e.Code)
                .HasColumnName("code")
                .HasMaxLength(50)
                .IsRequired();
            
            builder.HasIndex(e => e.Code).IsUnique();

            builder.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(e => e.ImageUrl)
                .HasColumnName("image_url")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(500);

            builder.Property(e => e.CategoryId).HasColumnName("category_id");

            builder.Property(e => e.Station).HasColumnName("station");
            builder.Property(e => e.ExpectedTime).HasColumnName("expected_time");

            builder.Property(e => e.PriceDineIn)
                .HasColumnName("price_dine_in")
                .HasPrecision(12, 2);

            builder.Property(e => e.PriceTakeAway)
                .HasColumnName("price_take_away")
                .HasPrecision(12, 2);

            builder.Property(e => e.Cost)
                .HasColumnName("cost")
                .HasPrecision(12, 2);

            builder.Property(e => e.IsOutOfStock).HasColumnName("is_out_of_stock");

            // Relationships
            builder.HasOne(e => e.Category)
                .WithMany(c => c.MenuItems)
                .HasForeignKey(e => e.CategoryId)
                .HasConstraintName("fk_menu_items_category_id")
                .OnDelete(DeleteBehavior.Restrict);

            // BaseEntity
            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            builder.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            builder.HasQueryFilter(e => !e.IsDeleted);

            // Indexes
            builder.HasIndex(e => e.CategoryId).HasDatabaseName("idx_menu_items_category_id");
        }
    }
}
