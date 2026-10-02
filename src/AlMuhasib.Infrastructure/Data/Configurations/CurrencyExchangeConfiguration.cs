using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class CurrencyExchangeConfiguration : IEntityTypeConfiguration<CurrencyExchange>
{
    public void Configure(EntityTypeBuilder<CurrencyExchange> builder)
    {
        builder.ToTable("CurrencyExchanges");

        builder.Property(e => e.FromCurrency)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(e => e.ToCurrency)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(e => e.FromAmount).HasPrecision(18, 4);
        builder.Property(e => e.ToAmount).HasPrecision(18, 4);
        builder.Property(e => e.FxRate).HasPrecision(18, 4);
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasOne(e => e.FromCashBox)
            .WithMany()
            .HasForeignKey(e => e.FromCashBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ToCashBox)
            .WithMany()
            .HasForeignKey(e => e.ToCashBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Date);
        builder.HasIndex(e => e.FromCashBoxId);
        builder.HasIndex(e => e.ToCashBoxId);
    }
}
