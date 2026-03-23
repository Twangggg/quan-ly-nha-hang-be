using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class StockOutReceiptItemLotAllocationConfiguration
        : IEntityTypeConfiguration<StockOutReceiptItemLotAllocation>
    {
        public void Configure(EntityTypeBuilder<StockOutReceiptItemLotAllocation> builder)
        {
            builder.ToTable("stock_out_receipt_item_lot_allocations");

            builder.HasKey(x => x.StockOutReceiptItemLotAllocationId);
            builder
                .Property(x => x.StockOutReceiptItemLotAllocationId)
                .HasColumnName("stock_out_receipt_item_lot_allocation_id");
            builder.Property(x => x.StockOutReceiptItemId).HasColumnName("stock_out_receipt_item_id").IsRequired();
            builder.Property(x => x.InventoryLotId).HasColumnName("inventory_lot_id").IsRequired();
            builder.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.UnitCost).HasColumnName("unit_cost").HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.LineCost).HasColumnName("line_cost").HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            builder.Property(x => x.CreatedBy).HasColumnName("created_by");
            builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

            builder.HasQueryFilter(x => x.DeletedAt == null);
            builder.HasIndex(x => x.StockOutReceiptItemId);
            builder.HasIndex(x => x.InventoryLotId);

            builder
                .HasOne(x => x.StockOutReceiptItem)
                .WithMany(x => x.LotAllocations)
                .HasForeignKey(x => x.StockOutReceiptItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne(x => x.InventoryLot)
                .WithMany()
                .HasForeignKey(x => x.InventoryLotId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
