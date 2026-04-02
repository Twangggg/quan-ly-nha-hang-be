using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class OrderPaymentConfiguration : IEntityTypeConfiguration<OrderPayment>
    {
        public void Configure(EntityTypeBuilder<OrderPayment> builder)
        {
            builder.HasKey(op => op.OrderPaymentId);

            builder.Property(op => op.Amount).HasColumnType("decimal(15,2)").IsRequired();
            builder.Property(op => op.PaidAt).IsRequired();
            builder.Property(op => op.Note).HasMaxLength(500);

            // Relationships
            builder.HasOne(op => op.Order)
                   .WithMany(o => o.OrderPayments)
                   .HasForeignKey(op => op.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(op => op.PaymentMethodConfig)
                   .WithMany(pm => pm.OrderPayments)
                   .HasForeignKey(op => op.PaymentMethodConfigId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Audit
            builder.Property(op => op.CreatedAt).HasDefaultValueSql("now()");
            builder.Property(op => op.CreatedBy).HasColumnName("created_by");
            builder.Property(op => op.UpdatedAt).HasColumnName("updated_at");
            builder.Property(op => op.UpdatedBy).HasColumnName("updated_by");
            builder.Property(op => op.DeletedAt).HasColumnName("deleted_at");

            // Indexes
            builder.HasIndex(op => op.OrderId);
            builder.HasIndex(op => op.PaymentMethodConfigId);
            builder.HasIndex(op => op.PaidAt);
        }
    }
}
