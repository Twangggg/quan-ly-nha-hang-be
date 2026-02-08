using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(o => o.OrderId);
            builder.Property(o => o.OrderCode).HasMaxLength(30).IsRequired();
            builder.HasIndex(o => o.OrderCode).IsUnique();
            builder.Property(o => o.OrderType).IsRequired(); 
            builder.Property(o => o.Status).IsRequired();    
            builder.Property(o => o.TotalAmount).HasColumnType("decimal(15,2)");
            builder.Property(o => o.Note).HasColumnType("text");

            // Relationships
            builder.HasOne(o => o.CreatedByEmployee)
                   .WithMany()
                   .HasForeignKey(o => o.CreatedBy);

            builder.Property(o => o.CreatedAt).HasDefaultValueSql("now()");
            
            // Audit Properties from BaseEntity
            builder.Property(o => o.CreatedBy).HasColumnName("created_by");
            builder.Property(o => o.UpdatedAt).HasColumnName("updated_at");
            builder.Property(o => o.UpdatedBy).HasColumnName("updated_by");
            builder.Property(o => o.DeletedAt).HasColumnName("deleted_at");

            // Global Query Filter for Soft Delete
            builder.HasQueryFilter(o => o.DeletedAt == null);

            // Indexes for Optimization
            builder.HasIndex(o => new { o.Status, o.CreatedAt });
            builder.HasIndex(o => o.OrderCode).IsUnique();
        }
    }
}
