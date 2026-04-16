using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class ProfitDistributionConfiguration : IEntityTypeConfiguration<ProfitDistribution>
{
    public void Configure(EntityTypeBuilder<ProfitDistribution> builder)
    {
        builder.ToTable("ProfitDistributions");

        builder.Property(pd => pd.TotalProfit).HasPrecision(18, 2);
        builder.Property(pd => pd.DistributedAmount).HasPrecision(18, 2);

        builder.HasIndex(pd => pd.Date);
    }
}
