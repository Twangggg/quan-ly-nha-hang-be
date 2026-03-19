using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class StockInReceiptConfiguration : IEntityTypeConfiguration<StockInReceipt>
    {
        public void Configure(EntityTypeBuilder<StockInReceipt> builder)
        {
            builder.ToTable("stock_in_receipts");

            builder.HasKey(x => x.StockInReceiptId);
            builder.Property(x => x.StockInReceiptId).HasColumnName("stock_in_receipt_id");

            builder.Property(x => x.ReceiptCode).HasColumnName("receipt_code").HasMaxLength(30).IsRequired();
            builder.Property(x => x.ReceiptType).HasColumnName("receipt_type").IsRequired();
            builder.Property(x => x.ReceivedAt).HasColumnName("received_at").IsRequired();
            builder.Property(x => x.Note).HasColumnName("note").HasMaxLength(500);
            builder.Property(x => x.TotalLines).HasColumnName("total_lines").IsRequired();
            builder.Property(x => x.TotalAmount).HasColumnName("total_amount").HasPrecision(18, 2);

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            builder.Property(x => x.CreatedBy).HasColumnName("created_by");
            builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

            builder.HasQueryFilter(x => x.DeletedAt == null);

            builder.HasIndex(x => x.ReceiptCode).IsUnique().HasFilter("deleted_at IS NULL");
            builder.HasIndex(x => x.ReceivedAt);

            builder
                .HasMany(x => x.Items)
                .WithOne(x => x.StockInReceipt)
                .HasForeignKey(x => x.StockInReceiptId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
