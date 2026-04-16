using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class InstallmentConfiguration : IEntityTypeConfiguration<Installment>
{
    public void Configure(EntityTypeBuilder<Installment> builder)
    {
        builder.ToTable("Installments");

        builder.Property(i => i.Amount).HasPrecision(18, 2);
        builder.Property(i => i.PaidAmount).HasPrecision(18, 2);
        builder.Property(i => i.RemainingAmount).HasPrecision(18, 2);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(i => i.InstallmentPlan)
            .WithMany(ip => ip.Installments)
            .HasForeignKey(i => i.InstallmentPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.CashBox)
            .WithMany()
            .HasForeignKey(i => i.CashBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.DueDate);
        builder.HasIndex(i => i.Status);
    }
}
