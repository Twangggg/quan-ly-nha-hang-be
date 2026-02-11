using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class OrderAuditLogConfiguration : IEntityTypeConfiguration<OrderAuditLog>
    {
        public void Configure(EntityTypeBuilder<OrderAuditLog> builder)
        {
            builder.HasKey(l => l.LogId);
            builder.Property(l => l.Action).HasMaxLength(50).IsRequired();

            // Map JSONB columns
            builder.Property(l => l.OldValue).HasColumnType("jsonb");
            builder.Property(l => l.NewValue).HasColumnType("jsonb");

            builder.Property(l => l.ChangeReason).HasColumnType("text");
            builder.HasOne(l => l.Order)
                   .WithMany(o => o.OrderAuditLogs) // Assuming Order has AuditLogs collection
                   .HasForeignKey(l => l.OrderId);
            builder.HasOne(l => l.Employee)
                   .WithMany()
                   .HasForeignKey(l => l.EmployeeId);

            builder.Property(l => l.CreatedAt).HasDefaultValueSql("now()");
        }
    }
}
