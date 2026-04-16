using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class InstallmentPlanConfiguration : IEntityTypeConfiguration<InstallmentPlan>
{
    public void Configure(EntityTypeBuilder<InstallmentPlan> builder)
    {
        builder.ToTable("InstallmentPlans");

        builder.Property(ip => ip.FileNumber).HasMaxLength(50);
        builder.Property(ip => ip.TotalAmount).HasPrecision(18, 2);
        builder.Property(ip => ip.InstallmentAmount).HasPrecision(18, 2);

        builder.HasOne(ip => ip.Invoice)
            .WithMany(i => i.InstallmentPlans)
            .HasForeignKey(ip => ip.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ip => ip.Customer)
            .WithMany(c => c.InstallmentPlans)
            .HasForeignKey(ip => ip.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ip => ip.FileNumber);
    }
}
