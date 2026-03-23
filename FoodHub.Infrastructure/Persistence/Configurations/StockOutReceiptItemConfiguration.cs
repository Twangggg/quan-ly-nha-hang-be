using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class StockOutReceiptItemConfiguration : IEntityTypeConfiguration<StockOutReceiptItem>
    {
        public void Configure(EntityTypeBuilder<StockOutReceiptItem> builder)
        {
            builder.ToTable("stock_out_receipt_items");

            builder.HasKey(x => x.StockOutReceiptItemId);
            builder
                .Property(x => x.StockOutReceiptItemId)
                .HasColumnName("stock_out_receipt_item_id");

            builder
                .Property(x => x.StockOutReceiptId)
                .HasColumnName("stock_out_receipt_id")
                .IsRequired();
            builder.Property(x => x.IngredientId).HasColumnName("ingredient_id").IsRequired();
            builder
                .Property(x => x.Quantity)
                .HasColumnName("quantity")
                .HasPrecision(18, 2)
                .IsRequired();
            builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2);
            builder
                .Property(x => x.LineAmount)
                .HasColumnName("line_amount")
                .HasPrecision(18, 2)
                .IsRequired();
            builder.Property(x => x.CostCalculatedAt).HasColumnName("cost_calculated_at");
            builder
                .Property(x => x.CostCalculationSource)
                .HasColumnName("cost_calculation_source")
                .HasDefaultValue(InventoryCostCalculationSource.Realtime);

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            builder.Property(x => x.CreatedBy).HasColumnName("created_by");
            builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

            builder.HasQueryFilter(x => x.DeletedAt == null);

            builder
                .HasIndex(x => new { x.StockOutReceiptId, x.IngredientId })
                .IsUnique()
                .HasFilter("deleted_at IS NULL");
            builder.HasIndex(x => x.IngredientId);

            builder
                .HasOne(x => x.Ingredient)
                .WithMany()
                .HasForeignKey(x => x.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
