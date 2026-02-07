using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class OrderItemOptionGroupConfiguration : IEntityTypeConfiguration<OrderItemOptionGroup>
    {
        public void Configure(EntityTypeBuilder<OrderItemOptionGroup> builder)
        {
            builder.ToTable("order_item_option_groups");

            builder.HasKey(e => e.OrderItemOptionGroupId);
            builder.Property(e => e.OrderItemOptionGroupId).HasColumnName("order_item_option_group_id");

            builder.Property(e => e.OrderItemId).HasColumnName("order_item_id");

            builder.Property(e => e.GroupNameSnapshot)
                .HasColumnName("group_name_snapshot")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.GroupTypeSnapshot)
                .HasColumnName("group_type_snapshot")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(e => e.IsRequiredSnapshot)
                .HasColumnName("is_required_snapshot");

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            // Relationships
            builder.HasOne(e => e.OrderItem)
                .WithMany(o => o.OptionGroups)
                .HasForeignKey(e => e.OrderItemId)
                .HasConstraintName("fk_order_item_option_groups_order_item_id")
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(e => e.OrderItemId)
                .HasDatabaseName("idx_order_item_option_groups_order_item_id");
        }
    }
}
