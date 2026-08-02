using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class LoyaltySettingsConfiguration : IEntityTypeConfiguration<LoyaltySettings>
{
    public void Configure(EntityTypeBuilder<LoyaltySettings> builder)
    {
        builder.ToTable("LoyaltySettings");

        builder.Property(x => x.PointsPerAmount).HasPrecision(18, 2);
        builder.Property(x => x.PointValueInCurrency).HasPrecision(18, 2);
        builder.Property(x => x.MinInvoiceAmountToEarn).HasPrecision(18, 2);
        builder.Property(x => x.MaxRedeemPercentOfInvoice).HasPrecision(5, 2);
    }
}
