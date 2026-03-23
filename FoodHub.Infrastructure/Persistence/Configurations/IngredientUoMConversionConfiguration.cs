using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class IngredientUoMConversionConfiguration
        : IEntityTypeConfiguration<IngredientUoMConversion>
    {
        public void Configure(EntityTypeBuilder<IngredientUoMConversion> builder)
        {
            builder.ToTable("ingredient_uom_conversions");

            builder.HasKey(x => x.IngredientUoMConversionId);
            builder
                .Property(x => x.IngredientUoMConversionId)
                .HasColumnName("ingredient_uom_conversion_id");
            builder.Property(x => x.IngredientId).HasColumnName("ingredient_id").IsRequired();
            builder
                .Property(x => x.FromUnit)
                .HasColumnName("from_unit")
                .HasMaxLength(20)
                .IsRequired();
            builder.Property(x => x.ToUnit).HasColumnName("to_unit").HasMaxLength(20).IsRequired();
            builder.Property(x => x.Factor).HasColumnName("factor").HasPrecision(18, 6);

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            builder.Property(x => x.CreatedBy).HasColumnName("created_by");
            builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

            builder.HasQueryFilter(x => x.DeletedAt == null);

            builder
                .HasIndex(x => new
                {
                    x.IngredientId,
                    x.FromUnit,
                    x.ToUnit,
                })
                .IsUnique()
                .HasFilter("deleted_at IS NULL");

            builder
                .HasOne(x => x.Ingredient)
                .WithMany(x => x.Conversions)
                .HasForeignKey(x => x.IngredientId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
