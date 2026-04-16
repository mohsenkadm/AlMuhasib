using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("BankAccounts");

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.AccountNumber)
            .HasMaxLength(50);

        builder.Property(b => b.Balance)
            .HasPrecision(18, 2);

        builder.HasIndex(b => b.AccountNumber)
            .IsUnique()
            .HasFilter("[AccountNumber] IS NOT NULL AND [IsDeleted] = 0");
    }
}
