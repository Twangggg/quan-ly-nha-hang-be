using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class InventoryLotConfiguration : IEntityTypeConfiguration<InventoryLot>
    {
        public void Configure(EntityTypeBuilder<InventoryLot> builder)
        {
            builder.ToTable("inventory_lots");

            builder.HasKey(x => x.InventoryLotId);
            builder.Property(x => x.InventoryLotId).HasColumnName("inventory_lot_id");
            builder.Property(x => x.IngredientId).HasColumnName("ingredient_id").IsRequired();
            builder.Property(x => x.StockInReceiptItemId).HasColumnName("stock_in_receipt_item_id");
            builder.Property(x => x.LotCode).HasColumnName("lot_code").HasMaxLength(100).IsRequired();
            builder.Property(x => x.ReceivedAt).HasColumnName("received_at").IsRequired();
            builder.Property(x => x.ExpiryDate).HasColumnName("expiry_date");
            builder.Property(x => x.UnitCost).HasColumnName("unit_cost").HasPrecision(18, 2);
            builder.Property(x => x.OriginalQuantity).HasColumnName("original_quantity").HasPrecision(18, 2);
            builder.Property(x => x.RemainingQuantity).HasColumnName("remaining_quantity").HasPrecision(18, 2);
            builder.Property(x => x.ReservedQuantity).HasColumnName("reserved_quantity").HasPrecision(18, 2);
            builder.Property(x => x.Status).HasColumnName("status").IsRequired();
            builder.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(500);

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            builder.Property(x => x.CreatedBy).HasColumnName("created_by");
            builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

            builder.HasQueryFilter(x => x.DeletedAt == null);

            builder.HasIndex(x => new { x.IngredientId, x.LotCode }).IsUnique().HasFilter("deleted_at IS NULL");
            builder.HasIndex(x => x.ExpiryDate);
            builder.HasIndex(x => new { x.IngredientId, x.Status, x.ExpiryDate });
            builder.HasIndex(x => new { x.IngredientId, x.RemainingQuantity });

            builder
                .HasOne(x => x.Ingredient)
                .WithMany()
                .HasForeignKey(x => x.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne(x => x.StockInReceiptItem)
                .WithMany(x => x.InventoryLots)
                .HasForeignKey(x => x.StockInReceiptItemId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
