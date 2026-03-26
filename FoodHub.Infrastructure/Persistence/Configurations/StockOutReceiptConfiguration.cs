using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class StockOutReceiptConfiguration : IEntityTypeConfiguration<StockOutReceipt>
    {
        public void Configure(EntityTypeBuilder<StockOutReceipt> builder)
        {
            builder.ToTable("stock_out_receipts");

            builder.HasKey(x => x.StockOutReceiptId);
            builder.Property(x => x.StockOutReceiptId).HasColumnName("stock_out_receipt_id");

            builder
                .Property(x => x.ReceiptCode)
                .HasColumnName("receipt_code")
                .HasMaxLength(30)
                .IsRequired();
            builder.Property(x => x.ReceiptType).HasColumnName("receipt_type").IsRequired();
            builder.Property(x => x.StockOutDate).HasColumnName("stock_out_date").IsRequired();
            builder.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();
            builder.Property(x => x.TotalAmount).HasColumnName("total_amount").HasPrecision(18, 2);

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            builder.Property(x => x.CreatedBy).HasColumnName("created_by");
            builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

            builder.HasQueryFilter(x => x.DeletedAt == null);

            builder.HasIndex(x => x.ReceiptCode).IsUnique().HasFilter("deleted_at IS NULL");
            builder.HasIndex(x => x.StockOutDate);

            builder
                .HasMany(x => x.Items)
                .WithOne(x => x.StockOutReceipt)
                .HasForeignKey(x => x.StockOutReceiptId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
