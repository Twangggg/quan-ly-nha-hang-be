using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("categories");

            // Global Query Filter for Soft Delete
            builder.HasQueryFilter(e => e.DeletedAt == null);

            builder.HasKey(e => e.CategoryId);
            builder.Property(e => e.CategoryId).HasColumnName("category_id");

            builder.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            builder.HasIndex(e => e.Name).IsUnique();

            builder.Property(e => e.CategoryType).HasColumnName("type");

            builder.Property(e => e.IsActive).HasColumnName("is_active");

            // Audit Properties
            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            // Indexes for Optimization
            builder.HasIndex(e => e.CategoryType);
            builder.HasIndex(e => e.IsActive);
            builder.HasIndex(e => new { e.CategoryType, e.IsActive });

            // Filtered index for active categories
            builder.HasIndex(e => e.IsActive)
                .HasFilter("deleted_at IS NULL");
        }
    }
}