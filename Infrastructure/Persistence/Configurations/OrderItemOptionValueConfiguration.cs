using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class OrderItemOptionValueConfiguration : IEntityTypeConfiguration<OrderItemOptionValue>
    {
        public void Configure(EntityTypeBuilder<OrderItemOptionValue> builder)
        {
            builder.ToTable("order_item_option_values");

            builder.HasKey(e => e.OrderItemOptionValueId);
            builder.Property(e => e.OrderItemOptionValueId).HasColumnName("order_item_option_value_id");

            builder.Property(e => e.OrderItemOptionGroupId).HasColumnName("order_item_option_group_id");
            builder.Property(e => e.OptionItemId).HasColumnName("option_item_id");

            builder.Property(e => e.LabelSnapshot)
                .HasColumnName("label_snapshot")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.ExtraPriceSnapshot)
                .HasColumnName("extra_price_snapshot")
                .HasPrecision(12, 2);

            builder.Property(e => e.Quantity)
                .HasColumnName("quantity")
                .HasDefaultValue(1);

            builder.Property(e => e.Note)
                .HasColumnName("note")
                .HasMaxLength(255);

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            // Relationships
            builder.HasOne(e => e.OptionGroup)
                .WithMany(g => g.OptionValues)
                .HasForeignKey(e => e.OrderItemOptionGroupId)
                .HasConstraintName("fk_order_item_option_values_group_id")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.OptionItem)
                .WithMany()
                .HasForeignKey(e => e.OptionItemId)
                .HasConstraintName("fk_order_item_option_values_option_item_id")
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            builder.HasIndex(e => e.OrderItemOptionGroupId)
                .HasDatabaseName("idx_order_item_option_values_group_id");
        }
    }
}
