using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class TableConfiguration : IEntityTypeConfiguration<Table>
    {
        public void Configure(EntityTypeBuilder<Table> builder)
        {
            // Map to "tables" table
            builder.ToTable("tables");

            builder.HasKey(t => t.TableId);
            builder.Property(t => t.TableId).HasColumnName("table_id");
            builder
                .Property(t => t.TableNumber)
                .HasMaxLength(20)
                .IsRequired()
                .HasColumnName("table_number");
            builder
                .Property(t => t.Capacity)
                .IsRequired()
                .HasDefaultValue(4)
                .HasColumnName("capacity");
            builder.Property(t => t.AreaId).IsRequired().HasColumnName("area_id");
            builder.Property(t => t.Status).IsRequired().HasColumnName("status");

            // Relationship to Area
            builder
                .HasOne(t => t.Area)
                .WithMany(c => c.Tables)
                .HasForeignKey(t => t.AreaId)
                .HasConstraintName("fk_tables_area_id")
                .OnDelete(DeleteBehavior.Restrict);

            // Audit columns (inherited)
            builder
                .Property(t => t.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(t => t.CreatedBy).HasColumnName("created_by");
            builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
            builder.Property(t => t.UpdatedBy).HasColumnName("updated_by");
            builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");

            // Global Query Filter for Soft Delete
            builder.HasQueryFilter(t => !t.DeletedAt.HasValue);

            // Indexes
            builder
                .HasIndex(t => new { t.TableNumber, t.AreaId })
                .IsUnique()
                .HasDatabaseName("idx_tables_table_number")
                .HasFilter("deleted_at IS NULL");
            builder.HasIndex(t => t.AreaId).HasDatabaseName("idx_tables_area_id");
            builder.HasIndex(t => t.CreatedAt).HasDatabaseName("idx_tables_created_at");
        }
    }
}
