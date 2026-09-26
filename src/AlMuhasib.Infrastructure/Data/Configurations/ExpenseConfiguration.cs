using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");

        builder.Property(e => e.Currency)
            .HasConversion<string>()
            .HasMaxLength(10)
            .HasDefaultValue(AlMuhasib.Core.Enums.AccountingCurrency.IQD);

        builder.Property(e => e.FxRate).HasPrecision(18, 4);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasOne(e => e.ExpenseType)
            .WithMany(et => et.Expenses)
            .HasForeignKey(e => e.ExpenseTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CashBox)
            .WithMany()
            .HasForeignKey(e => e.CashBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Date);
    }
}
