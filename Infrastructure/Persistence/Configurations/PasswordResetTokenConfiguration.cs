using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
    {
        public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
        {
            builder.HasKey(t => t.TokenId);

            builder.Property(t => t.TokenHash)
                   .IsRequired()
                   .HasMaxLength(64); // SHA256 hash is 64 characters in hex

            builder.HasIndex(t => t.TokenHash)
                   .IsUnique();

            builder.Property(t => t.ExpiresAt)
                   .IsRequired();

            builder.HasIndex(t => t.ExpiresAt);

            builder.Property(t => t.UsedAt);

            builder.Property(t => t.CreatedAt)
                   .HasDefaultValueSql("now()");

            // Foreign key relationship
            builder.HasOne(t => t.Employee)
                   .WithMany()
                   .HasForeignKey(t => t.EmployeeId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(t => t.EmployeeId);
        }
    }
}
