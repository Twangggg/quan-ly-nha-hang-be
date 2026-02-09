using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class SetMenuConfiguration : IEntityTypeConfiguration<SetMenu>
    {
        public void Configure(EntityTypeBuilder<SetMenu> builder)
        {
            builder.ToTable("set_menus");

            // Global Query Filter for Soft Delete
            builder.HasQueryFilter(e => e.DeletedAt == null);

            builder.HasKey(e => e.SetMenuId);
            builder.Property(e => e.SetMenuId).HasColumnName("set_menu_id");

            builder.Property(e => e.Code)
                .HasColumnName("code")
                .HasMaxLength(50)
                .IsRequired();

            builder.HasIndex(e => e.Code).IsUnique();

            builder.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(e => e.SetType)
                .HasColumnName("set_type")
                .HasConversion<string>();

            builder.Property(e => e.ImageUrl)
                .HasColumnName("image_url")
                .HasMaxLength(500);

            builder.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(500);

            builder.Property(e => e.Price)
                .HasColumnName("price")
                .HasPrecision(12, 2);

            builder.Property(e => e.CostPrice)
                .HasColumnName("cost_price")
                .HasPrecision(12, 2);

            builder.Property(e => e.IsOutOfStock).HasColumnName("is_out_of_stock");

            // Audit Properties from BaseEntity
            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            
            // Indexes for Optimization
            builder.HasIndex(e => e.SetType);
            builder.HasIndex(e => e.IsOutOfStock);
            builder.HasIndex(e => e.Price);
            builder.HasIndex(e => e.CreatedAt);
            
            // Composite indexes
            builder.HasIndex(e => new { e.SetType, e.IsOutOfStock });
            
            // Filtered index for available sets
            builder.HasIndex(e => e.IsOutOfStock)
                .HasFilter("deleted_at IS NULL");
        }
    }
}
