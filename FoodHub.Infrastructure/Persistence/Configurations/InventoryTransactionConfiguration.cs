using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class InventoryTransactionConfiguration
        : IEntityTypeConfiguration<InventoryTransaction>
    {
        public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
        {
            builder.ToTable("inventory_transactions");

            builder.HasKey(x => x.InventoryTransactionId);
            builder
                .Property(x => x.InventoryTransactionId)
                .HasColumnName("inventory_transaction_id");

            builder.Property(x => x.IngredientId).HasColumnName("ingredient_id").IsRequired();
            builder
                .Property(x => x.TransactionType)
                .HasColumnName("transaction_type")
                .IsRequired();
            builder.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(18, 2);
            builder.Property(x => x.UnitCost).HasColumnName("unit_cost").HasPrecision(18, 2);
            builder
                .Property(x => x.BalanceAfter)
                .HasColumnName("balance_after")
                .HasPrecision(18, 2);
            builder.Property(x => x.Reference).HasColumnName("reference").HasMaxLength(255);
            builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            builder.Property(x => x.CreatedBy).HasColumnName("created_by");
            builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

            builder.HasQueryFilter(x => x.DeletedAt == null);

            builder
                .HasOne(x => x.Ingredient)
                .WithMany(x => x.InventoryTransactions)
                .HasForeignKey(x => x.IngredientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.IngredientId);
            builder.HasIndex(x => x.TransactionType);
            builder.HasIndex(x => x.OccurredAt);
        }
    }
}
