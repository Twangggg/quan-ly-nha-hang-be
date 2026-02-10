using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class OptionItemConfiguration : IEntityTypeConfiguration<OptionItem>
    {
        public void Configure(EntityTypeBuilder<OptionItem> builder)
        {
            builder.ToTable("option_items");

            // Global Query Filter for Soft Delete
            builder.HasQueryFilter(e => e.DeletedAt == null);

            builder.HasKey(e => e.OptionItemId);
            builder.Property(e => e.OptionItemId).HasColumnName("option_item_id");

            builder.Property(e => e.OptionGroupId).HasColumnName("option_group_id");

            builder.Property(e => e.Label)
                .HasColumnName("label")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.ExtraPrice)
                .HasColumnName("extra_price")
                .HasPrecision(12, 2);

            // Audit Properties from BaseEntity
            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            // Relationships
            builder.HasOne(e => e.OptionGroup)
                .WithMany(g => g.OptionItems)
                .HasForeignKey(e => e.OptionGroupId)
                .HasConstraintName("fk_option_items_option_group_id")
                .OnDelete(DeleteBehavior.Cascade);



            // Indexes
            builder.HasIndex(e => e.OptionGroupId).HasDatabaseName("idx_option_items_option_group_id");
            builder.HasIndex(e => e.ExtraPrice);
        }
    }
}
