using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class InventoryCheckConfiguration : IEntityTypeConfiguration<InventoryCheck>
    {
        public void Configure(EntityTypeBuilder<InventoryCheck> builder)
        {
            builder.ToTable("inventory_checks");

            builder.HasKey(x => x.InventoryCheckId);
            builder.Property(x => x.InventoryCheckId).HasColumnName("inventory_check_id");
            builder.Property(x => x.CheckDate).HasColumnName("check_date").IsRequired();
            builder.Property(x => x.Status).HasColumnName("status").IsRequired();
            builder.Property(x => x.ProcessedAt).HasColumnName("processed_at");

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            builder.Property(x => x.CreatedBy).HasColumnName("created_by");
            builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

            builder.HasQueryFilter(x => x.DeletedAt == null);

            builder.HasIndex(x => x.CheckDate);
            builder.HasIndex(x => x.Status);

            builder
                .HasMany(x => x.Items)
                .WithOne(x => x.InventoryCheck)
                .HasForeignKey(x => x.InventoryCheckId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
