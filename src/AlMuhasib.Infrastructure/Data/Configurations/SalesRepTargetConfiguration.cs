using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class SalesRepTargetConfiguration : IEntityTypeConfiguration<SalesRepTarget>
{
    public void Configure(EntityTypeBuilder<SalesRepTarget> builder)
    {
        builder.ToTable("SalesRepTargets");

        builder.Property(x => x.TargetAmount).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasOne(x => x.SalesRepresentative)
            .WithMany(r => r.Targets)
            .HasForeignKey(x => x.SalesRepresentativeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.SalesRepresentativeId);
        builder.HasIndex(x => new { x.SalesRepresentativeId, x.PeriodStart, x.PeriodEnd });
    }
}
