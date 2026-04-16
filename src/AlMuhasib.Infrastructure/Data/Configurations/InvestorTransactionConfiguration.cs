using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class InvestorTransactionConfiguration : IEntityTypeConfiguration<InvestorTransaction>
{
    public void Configure(EntityTypeBuilder<InvestorTransaction> builder)
    {
        builder.ToTable("InvestorTransactions");

        builder.Property(it => it.Type)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(it => it.Amount).HasPrecision(18, 2);
        builder.Property(it => it.Notes).HasMaxLength(1000);

        builder.HasOne(it => it.Investor)
            .WithMany(i => i.Transactions)
            .HasForeignKey(it => it.InvestorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(it => it.Date);
        builder.HasIndex(it => it.Type);
    }
}
