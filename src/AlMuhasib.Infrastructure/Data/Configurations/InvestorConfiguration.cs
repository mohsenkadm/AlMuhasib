using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class InvestorConfiguration : IEntityTypeConfiguration<Investor>
{
    public void Configure(EntityTypeBuilder<Investor> builder)
    {
        builder.ToTable("Investors");

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Phone)
            .HasMaxLength(20);

        builder.Property(i => i.TotalDeposit)
            .HasPrecision(18, 2);

        builder.Property(i => i.OpeningBalance)
            .HasPrecision(18, 2);

        builder.Property(i => i.ProfitPercentage)
            .HasPrecision(5, 2);

        builder.HasIndex(i => i.Name);
    }
}
