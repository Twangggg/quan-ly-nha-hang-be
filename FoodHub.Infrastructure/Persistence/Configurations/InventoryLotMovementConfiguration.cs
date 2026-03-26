using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class InventoryLotMovementConfiguration : IEntityTypeConfiguration<InventoryLotMovement>
    {
        public void Configure(EntityTypeBuilder<InventoryLotMovement> builder)
        {
            builder.ToTable("inventory_lot_movements");

            builder.HasKey(x => x.InventoryLotMovementId);
            builder.Property(x => x.InventoryLotMovementId).HasColumnName("inventory_lot_movement_id");
            builder.Property(x => x.InventoryLotId).HasColumnName("inventory_lot_id").IsRequired();
            builder.Property(x => x.TransactionType).HasColumnName("transaction_type").IsRequired();
            builder.Property(x => x.QuantityDelta).HasColumnName("quantity_delta").HasPrecision(18, 2);
            builder.Property(x => x.BalanceAfter).HasColumnName("balance_after").HasPrecision(18, 2);
            builder.Property(x => x.ReferenceType).HasColumnName("reference_type").HasMaxLength(50).IsRequired();
            builder.Property(x => x.ReferenceId).HasColumnName("reference_id");
            builder.Property(x => x.ReferenceCode).HasColumnName("reference_code").HasMaxLength(100);
            builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();
            builder.Property(x => x.UnitCost).HasColumnName("unit_cost").HasPrecision(18, 2);
            builder.Property(x => x.Note).HasColumnName("note").HasMaxLength(500);

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            builder.Property(x => x.CreatedBy).HasColumnName("created_by");
            builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

            builder.HasQueryFilter(x => x.DeletedAt == null);

            builder.HasIndex(x => new { x.InventoryLotId, x.OccurredAt });
            builder.HasIndex(x => new { x.ReferenceId, x.ReferenceType });

            builder
                .HasOne(x => x.InventoryLot)
                .WithMany(x => x.Movements)
                .HasForeignKey(x => x.InventoryLotId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
