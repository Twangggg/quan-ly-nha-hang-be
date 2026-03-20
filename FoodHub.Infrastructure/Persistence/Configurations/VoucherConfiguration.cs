using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class VoucherConfiguration : IEntityTypeConfiguration<Voucher>
    {
        public void Configure(EntityTypeBuilder<Voucher> builder)
        {
            // Configure the Voucher entity
            builder.ToTable("vouchers");
            builder.HasKey(v => v.VoucherId);

            // Configure properties
            builder.Property(v => v.VoucherId).HasColumnName("voucher_id").IsRequired();
            builder.Property(v => v.VoucherCode).HasColumnName("voucher_code").HasMaxLength(50).IsRequired();
            builder.Property(v => v.VoucherType).HasColumnName("voucher_type").IsRequired();
            builder.Property(v => v.DiscountValue).HasColumnName("discount_value").HasPrecision(18,2);
            builder.Property(v => v.MaxDiscount).HasColumnName("max_discount").HasPrecision(18,2);
            builder.Property(v => v.MinOrderValue).HasColumnName("min_order_value").HasPrecision(18,2);
            builder.Property(v => v.ItemtId).HasColumnName("item_id");
            builder.Property(v => v.FreeQuantity).HasColumnName("free_quantity");
            builder.Property(v => v.StartDate).HasColumnName("start_date").IsRequired();
            builder.Property(v => v.EndDate).HasColumnName("end_date").IsRequired();
            builder.Property(v => v.StartTime).HasColumnName("start_time");
            builder.Property(v => v.EndTime).HasColumnName("end_time");
            builder.Property(v => v.IsActive).HasColumnName("is_active").IsRequired();
            builder.Property(v => v.UsageLimit).HasColumnName("usage_limit");
            builder.Property(v => v.UsedCount).HasColumnName("used_count").IsRequired();

            // Configure audit properties
            builder.Property(v => v.CreatedAt).HasColumnName("created_at");
            builder.Property(v => v.CreatedBy).HasColumnName("created_by");
            builder.Property(v => v.UpdatedAt).HasColumnName("updated_at");
            builder.Property(v => v.UpdatedBy).HasColumnName("updated_by");
            builder.Property(v => v.DeletedAt).HasColumnName("deleted_at");

            // Configure relationships
            builder.HasOne(v => v.Item)
                   .WithMany()
                   .HasForeignKey(v => v.ItemtId)
                   .OnDelete(DeleteBehavior.SetNull);

            // Global query filter to exclude soft-deleted vouchers
            builder.HasQueryFilter(v => !v.DeletedAt.HasValue);

            // Indexes
            builder.HasIndex(v => v.VoucherCode).HasDatabaseName("idx_voucher_code").IsUnique();
        }
    }
}
