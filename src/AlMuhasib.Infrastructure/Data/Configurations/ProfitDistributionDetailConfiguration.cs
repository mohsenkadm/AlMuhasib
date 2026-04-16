using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class ProfitDistributionDetailConfiguration : IEntityTypeConfiguration<ProfitDistributionDetail>
{
    public void Configure(EntityTypeBuilder<ProfitDistributionDetail> builder)
    {
        builder.ToTable("ProfitDistributionDetails");

        builder.Property(d => d.ProfitPercentage).HasPrecision(5, 2);
        builder.Property(d => d.Amount).HasPrecision(18, 2);

        builder.HasOne(d => d.ProfitDistribution)
            .WithMany(pd => pd.Details)
            .HasForeignKey(d => d.ProfitDistributionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Investor)
            .WithMany(i => i.ProfitDistributionDetails)
            .HasForeignKey(d => d.InvestorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
