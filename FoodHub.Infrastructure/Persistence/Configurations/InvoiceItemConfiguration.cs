using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
    {
        public void Configure(EntityTypeBuilder<InvoiceItem> builder)
        {
            // Table mapping
            builder.ToTable("invoice_items");
            // Primary Key
            builder.HasKey(i => i.InvoiceItemId);

            // Properties
            builder.Property(i => i.InvoiceItemId).HasColumnName("invoice_item_id");
            builder.Property(i => i.InvoiceId).HasColumnName("invoice_id");
            // String properties
            builder.Property(i => i.ItemName).HasColumnName("item_name").HasMaxLength(255).IsRequired();
            builder.Property(i => i.Note).HasColumnName("note").HasMaxLength(1000);
            // Numeric properties
            builder.Property(i => i.Quantity).HasColumnName("quantity").IsRequired();
            builder.Property(i => i.UnitPrice).HasColumnName("unit_price").HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(i => i.TotalPrice).HasColumnName("total_price").HasColumnType("decimal(18,2)").IsRequired();

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
            builder.HasIndex(i => i.InvoiceId).HasDatabaseName("idx_invoice_items_invoice_id");
            builder.HasIndex(i => i.ItemName).HasDatabaseName("idx_invoice_items_item_name");
        }
    }
}
