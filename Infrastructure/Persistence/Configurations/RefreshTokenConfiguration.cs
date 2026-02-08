using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(x => x.Id);

            // Token là chuỗi Unique và rất quan trọng để tìm kiếm
            builder.Property(x => x.Token)
                   .IsRequired()
                   .HasMaxLength(200); // Base64 32 bytes ~ 44 chars, nhưng cứ cho dư giả

            builder.HasIndex(x => x.Token)
                   .IsUnique();

            builder.Property(x => x.Expires).IsRequired();
            builder.Property(x => x.IsRevoked).HasDefaultValue(false);

            builder.Property(x => x.CreatedAt)
                   .HasDefaultValueSql("now()");

            // Quan hệ với Employee
            builder.HasOne(x => x.Employee)
                   .WithMany(e => e.RefreshTokens)
                   .HasForeignKey(x => x.EmployeeId)
                   .OnDelete(DeleteBehavior.Cascade); // Xóa nhân viên -> Xóa hết token
        }
    }
}
