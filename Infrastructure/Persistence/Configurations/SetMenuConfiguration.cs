using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class SetMenuConfiguration : IEntityTypeConfiguration<SetMenu>
    {
        public void Configure(EntityTypeBuilder<SetMenu> builder)
        {
            builder.HasKey(sm => sm.SetMenuId);

            builder.Property(sm => sm.Code)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(sm => sm.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(sm => sm.Description)
                .HasMaxLength(500);

            builder.Property(sm => sm.Price)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(sm => sm.ImageUrl)
                .HasMaxLength(500);

            builder.Property(sm => sm.IsActive)
                .HasDefaultValue(true);

            builder.Property(sm => sm.IsOutOfStock)
                .HasDefaultValue(false);

            builder.Property(sm => sm.CreatedAt)
                .HasDefaultValueSql("now()");

            // Indexes
            builder.HasIndex(sm => sm.Code).IsUnique();
            builder.HasIndex(sm => sm.Name);
            builder.HasIndex(sm =>  sm.IsActive);
            builder.HasIndex(sm => sm.IsOutOfStock);

            // Relationships
            builder.HasMany(sm => sm.SetMenuItems)
                .WithOne(smi => smi.SetMenu)
                .HasForeignKey(smi => smi.SetMenuId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
