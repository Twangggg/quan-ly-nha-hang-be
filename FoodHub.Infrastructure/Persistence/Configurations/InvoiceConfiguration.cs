using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.ToTable("invoices");

            // Primary Key
            builder.HasKey(i => i.InvoiceId);

            // Properties
            builder.Property(i => i.InvoiceId).HasColumnName("invoice_id");
            builder.Property(i => i.OrderId).IsRequired().HasColumnName("order_id");
            // String properties
            builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50).HasColumnName("invoice_number");
            builder.Property(i => i.PaymentMethod).IsRequired().HasMaxLength(50).HasColumnName("payment_method");
            builder.Property(i => i.CashierName).IsRequired().HasMaxLength(100).HasColumnName("cashier_name");
            builder.Property(i => i.TableNumber).HasMaxLength(20).HasColumnName("table_number");
            // Decimal properties with precision
            builder.Property(i => i.SubTotal).HasPrecision(18, 2).HasColumnName("sub_total");
            builder.Property(i => i.TaxAmount).HasPrecision(18, 2).HasColumnName("tax_amount");
            builder.Property(i => i.DiscountAmount).HasPrecision(18, 2).HasColumnName("discount_amount");
            builder.Property(i => i.TotalAmount).HasPrecision(18, 2).HasColumnName("total_amount");
            builder.Property(i => i.AmountReceived).HasPrecision(18, 2).HasColumnName("amount_received");
            builder.Property(i => i.AmountReturned).HasPrecision(18, 2).HasColumnName("amount_returned");

            // Relationship to InvoiceItem
            builder
                .HasMany(i => i.Items)
                .WithOne(r => r.Invoice)
                .HasForeignKey(i => i.InvoiceId)
                .HasConstraintName("fk_invoice_items_invoice_id")
                .OnDelete(DeleteBehavior.Cascade);

            // Audit columns (inherited)
            builder
                .Property(i => i.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(i => i.CreatedBy).HasColumnName("created_by");
            builder.Property(i => i.UpdatedAt).HasColumnName("updated_at");
            builder.Property(i => i.UpdatedBy).HasColumnName("updated_by");
            builder.Property(i => i.DeletedAt).HasColumnName("deleted_at");

            // Global Query Filter for Soft Delete
            builder.HasQueryFilter(i => !i.DeletedAt.HasValue);

            // Indexes
            builder.HasIndex(i => i.InvoiceId).IsUnique().HasDatabaseName("idx_invoices_invoice_id").HasFilter("deleted_at IS NULL");
            builder.HasIndex(i => i.InvoiceNumber).IsUnique().HasDatabaseName("idx_invoices_invoice_number").HasFilter("deleted_at IS NULL");
            builder.HasIndex(i => i.OrderId).HasDatabaseName("idx_invoices_order_id").HasFilter("deleted_at IS NULL");
        }
    }
}
