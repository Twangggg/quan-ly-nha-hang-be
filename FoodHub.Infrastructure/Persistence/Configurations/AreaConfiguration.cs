using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class AreaConfiguration : IEntityTypeConfiguration<Area>
    {
        public void Configure(EntityTypeBuilder<Area> builder)
        {
            builder.ToTable("areas");

            builder.HasKey(a => a.AreaId);
            builder.Property(a => a.AreaId).HasColumnName("area_id");
            builder.Property(a => a.Name).IsRequired().HasMaxLength(100).HasColumnName("name");
            builder.Property(a => a.CodePrefix).IsRequired().HasMaxLength(10).HasColumnName("code_prefix");
            builder.Property(a => a.Status).IsRequired().HasColumnName("status");

            // Audit
            builder.Property(a => a.CreatedAt).HasColumnName("created_at");
            builder.Property(a => a.CreatedBy).HasColumnName("created_by");
            builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
            builder.Property(a => a.UpdatedBy).HasColumnName("updated_by");
            builder.Property(a => a.DeletedAt).HasColumnName("deleted_at");

            // Soft-delete filter
            builder.HasQueryFilter(a => !a.DeletedAt.HasValue);

            // Indexes
            builder.HasIndex(a => a.Name).HasDatabaseName("idx_areas_name");
            builder.HasIndex(a => a.CodePrefix).HasDatabaseName("idx_areas_code_prefix");
            builder.HasIndex(a => a.CreatedAt).HasDatabaseName("idx_areas_created_at");
        }
    }
}
