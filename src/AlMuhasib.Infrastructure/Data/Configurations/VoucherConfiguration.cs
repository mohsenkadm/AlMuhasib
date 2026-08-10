using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class VoucherConfiguration : IEntityTypeConfiguration<Voucher>
{
    public void Configure(EntityTypeBuilder<Voucher> builder)
    {
        builder.ToTable("Vouchers");

        builder.Property(v => v.VoucherNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(v => v.VoucherType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(v => v.Amount).HasPrecision(18, 2);
        builder.Property(v => v.BankFees).HasPrecision(18, 2);
        builder.Property(v => v.Notes).HasMaxLength(1000);

        builder.HasOne(v => v.Customer)
            .WithMany(c => c.Vouchers)
            .HasForeignKey(v => v.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Investor)
            .WithMany(i => i.Vouchers)
            .HasForeignKey(v => v.InvestorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.CashBox)
            .WithMany()
            .HasForeignKey(v => v.CashBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.BankAccount)
            .WithMany()
            .HasForeignKey(v => v.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Invoice)
            .WithMany()
            .HasForeignKey(v => v.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Installment)
            .WithMany()
            .HasForeignKey(v => v.InstallmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(v => v.ReconciledBy).HasMaxLength(100);

        builder.HasIndex(v => v.VoucherNumber).IsUnique();
        builder.HasIndex(v => v.Date);
        builder.HasIndex(v => v.VoucherType);
        builder.HasIndex(v => v.InvoiceId);
        builder.HasIndex(v => v.InstallmentId);
        builder.HasIndex(v => v.IsReconciled);
    }
}
