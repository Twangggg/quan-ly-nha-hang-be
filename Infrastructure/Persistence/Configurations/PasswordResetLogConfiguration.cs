using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations
{
    public class PasswordResetLogConfiguration : IEntityTypeConfiguration<PasswordResetLog>
    {
        public void Configure(EntityTypeBuilder<PasswordResetLog> builder)
        {
            builder.HasKey(l => l.LogId);
            builder.HasOne(l => l.TargetEmployee)
                .WithMany(e => e.TargetLogs)
                .HasForeignKey(l => l.TargetEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(l => l.PerformedByEmployee)
                .WithMany(e => e.PerformedLogs)
                .HasForeignKey(l => l.PerformedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.Reason)
               .HasMaxLength(500);

            builder.Property(x => x.ResetAt)
                   .HasDefaultValueSql("now()");

            builder.HasIndex(x => x.TargetEmployeeId);
            builder.HasIndex(x => x.PerformedByEmployeeId);
            builder.HasIndex(x => x.ResetAt);
        }
    }
}
