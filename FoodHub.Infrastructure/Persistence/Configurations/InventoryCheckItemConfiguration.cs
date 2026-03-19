using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class InventoryCheckItemConfiguration : IEntityTypeConfiguration<InventoryCheckItem>
    {
        public void Configure(EntityTypeBuilder<InventoryCheckItem> builder)
        {
            builder.ToTable("inventory_check_items");

            builder.HasKey(x => x.InventoryCheckItemId);
            builder.Property(x => x.InventoryCheckItemId).HasColumnName("inventory_check_item_id");
            builder.Property(x => x.InventoryCheckId).HasColumnName("inventory_check_id").IsRequired();
            builder.Property(x => x.IngredientId).HasColumnName("ingredient_id").IsRequired();
            builder.Property(x => x.BookQuantity).HasColumnName("book_quantity").HasPrecision(18, 2);
            builder
                .Property(x => x.PhysicalQuantity)
                .HasColumnName("physical_quantity")
                .HasPrecision(18, 2);
            builder
                .Property(x => x.DifferenceQuantity)
                .HasColumnName("difference_quantity")
                .HasPrecision(18, 2);
            builder.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500);

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            builder.Property(x => x.CreatedBy).HasColumnName("created_by");
            builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

            builder.HasQueryFilter(x => x.DeletedAt == null);

            builder.HasIndex(x => x.InventoryCheckId);
            builder.HasIndex(x => x.IngredientId);

            builder
                .HasOne(x => x.Ingredient)
                .WithMany()
                .HasForeignKey(x => x.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
