using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
    {
        public void Configure(EntityTypeBuilder<Ingredient> builder)
        {
            builder.ToTable("ingredients");

            builder.HasKey(e => e.IngredientId);
            builder.Property(e => e.IngredientId).HasColumnName("ingredient_id");

            builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
            builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            builder
                .Property(e => e.BaseUnit)
                .HasColumnName("base_unit")
                .HasMaxLength(20)
                .IsRequired();
            builder
                .Property(e => e.CurrentStock)
                .HasColumnName("current_stock")
                .HasPrecision(18, 2);
            builder
                .Property(e => e.LowStockThreshold)
                .HasColumnName("low_stock_threshold")
                .HasPrecision(18, 2);
            builder.Property(e => e.CostPrice).HasColumnName("cost_price").HasPrecision(18, 2);
            builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
            builder.Property(e => e.IsActive).HasColumnName("is_active");

            // Audit Properties
            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            // Global Query Filter for Soft Delete (if we use BaseEntity pattern)
            builder.HasQueryFilter(e => e.DeletedAt == null);

            // Indexes
            builder.HasIndex(e => e.Code).IsUnique().HasFilter("deleted_at IS NULL");
            builder.HasIndex(e => e.Name).IsUnique().HasFilter("deleted_at IS NULL");
            builder.HasIndex(e => e.IsActive);
        }
    }
}
