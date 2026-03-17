using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("audit_logs");

            builder.HasKey(x => x.LogId);

            builder.Property(x => x.EntityName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.EntityId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Action)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.OldValues)
                .HasColumnType("jsonb");

            builder.Property(x => x.NewValues)
                .HasColumnType("jsonb");

            builder.Property(x => x.ActorInfo)
                .HasMaxLength(500);

            builder.Property(x => x.CreatedAt)
                .IsRequired();
            
            builder.HasIndex(x => new { x.EntityName, x.EntityId });
            builder.HasIndex(x => x.CreatedAt);
        }
    }
}
