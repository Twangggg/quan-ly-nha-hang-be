using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class MenuItemIngredientConfiguration : IEntityTypeConfiguration<MenuItemIngredient>
    {
        public void Configure(EntityTypeBuilder<MenuItemIngredient> builder)
        {
            builder.ToTable("menu_item_ingredients");

            builder.HasKey(x => x.MenuItemIngredientId);
            builder.Property(x => x.MenuItemIngredientId).HasColumnName("menu_item_ingredient_id");
            builder.Property(x => x.MenuItemId).HasColumnName("menu_item_id").IsRequired();
            builder.Property(x => x.IngredientId).HasColumnName("ingredient_id").IsRequired();
            builder
                .Property(x => x.QuantityPerServing)
                .HasColumnName("quantity_per_serving")
                .HasPrecision(18, 4);

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            builder.Property(x => x.CreatedBy).HasColumnName("created_by");
            builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

            builder.HasQueryFilter(x => x.DeletedAt == null);

            builder
                .HasIndex(x => new { x.MenuItemId, x.IngredientId })
                .IsUnique()
                .HasFilter("deleted_at IS NULL");

            builder
                .HasOne(x => x.MenuItem)
                .WithMany(m => m.Ingredients)
                .HasForeignKey(x => x.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(x => x.Ingredient)
                .WithMany()
                .HasForeignKey(x => x.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
