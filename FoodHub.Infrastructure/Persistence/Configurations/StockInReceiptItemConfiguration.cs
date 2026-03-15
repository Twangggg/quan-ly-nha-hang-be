using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class StockInReceiptItemConfiguration : IEntityTypeConfiguration<StockInReceiptItem>
    {
        public void Configure(EntityTypeBuilder<StockInReceiptItem> builder)
        {
            builder.ToTable("stock_in_receipt_items");

            builder.HasKey(x => x.StockInReceiptItemId);
            builder.Property(x => x.StockInReceiptItemId).HasColumnName("stock_in_receipt_item_id");

            builder.Property(x => x.StockInReceiptId).HasColumnName("stock_in_receipt_id").IsRequired();
            builder.Property(x => x.IngredientId).HasColumnName("ingredient_id").IsRequired();
            builder.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(18, 2);
            builder.Property(x => x.UnitCost).HasColumnName("unit_cost").HasPrecision(18, 2);
            builder.Property(x => x.LineAmount).HasColumnName("line_amount").HasPrecision(18, 2);
            builder.Property(x => x.ExpiryDate).HasColumnName("expiry_date");
            builder.Property(x => x.BatchCode).HasColumnName("batch_code").HasMaxLength(100);

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            builder.Property(x => x.CreatedBy).HasColumnName("created_by");
            builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

            builder.HasQueryFilter(x => x.DeletedAt == null);

            builder.HasIndex(x => new { x.StockInReceiptId, x.IngredientId }).IsUnique().HasFilter("deleted_at IS NULL");
            builder.HasIndex(x => x.IngredientId);

            builder
                .HasOne(x => x.Ingredient)
                .WithMany()
                .HasForeignKey(x => x.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
