using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class PaymentMethodConfigConfiguration : IEntityTypeConfiguration<PaymentMethodConfig>
    {
        public void Configure(EntityTypeBuilder<PaymentMethodConfig> builder)
        {
            builder.HasKey(p => p.PaymentMethodConfigId);

            builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
            builder.HasIndex(p => p.Name).IsUnique();

            builder.Property(p => p.Type).IsRequired();
            builder.Property(p => p.IsActive).HasDefaultValue(true);
            builder.Property(p => p.IsDefault).HasDefaultValue(false);

            // Bank info
            builder.Property(p => p.BankName).HasMaxLength(100);
            builder.Property(p => p.BankBin).HasMaxLength(20);
            builder.Property(p => p.AccountNumber).HasMaxLength(50);
            builder.Property(p => p.AccountHolderName).HasMaxLength(100);

            // Audit
            builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
            builder.Property(p => p.CreatedBy).HasColumnName("created_by");
            builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            builder.Property(p => p.UpdatedBy).HasColumnName("updated_by");
            builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");

            builder.HasQueryFilter(p => p.DeletedAt == null);

            // Indexes
            builder.HasIndex(p => p.Type);
            builder.HasIndex(p => p.IsActive);
        }
    }
}
