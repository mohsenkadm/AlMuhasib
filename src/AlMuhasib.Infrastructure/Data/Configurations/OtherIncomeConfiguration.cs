using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class OtherIncomeConfiguration : IEntityTypeConfiguration<OtherIncome>
{
    public void Configure(EntityTypeBuilder<OtherIncome> builder)
    {
        builder.ToTable("OtherIncomes");

        builder.Property(e => e.IncomeNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(e => e.Currency)
            .HasConversion<string>()
            .HasMaxLength(10)
            .HasDefaultValue(AlMuhasib.Core.Enums.AccountingCurrency.IQD);

        builder.Property(e => e.FxRate).HasPrecision(18, 4);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasOne(e => e.OtherIncomeType)
            .WithMany(t => t.Incomes)
            .HasForeignKey(e => e.OtherIncomeTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CashBox)
            .WithMany()
            .HasForeignKey(e => e.CashBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Date);
        builder.HasIndex(e => new { e.BranchId, e.IncomeNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
