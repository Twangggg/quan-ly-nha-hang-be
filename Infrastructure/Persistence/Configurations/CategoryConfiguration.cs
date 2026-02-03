using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasKey(c => c.CategoryId);

            builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
            builder.HasIndex(c => c.Name).IsUnique();
            builder.Property(c => c.CategoryType)
                   .HasConversion<string>() // Store as string (NORMAL, SPECIAL_GROUP)
                   .HasMaxLength(30);
            builder.Property(c => c.CreatedAt).HasDefaultValueSql("now()");
        }
    }
}
