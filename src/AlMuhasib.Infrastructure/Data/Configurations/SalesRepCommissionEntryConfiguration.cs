using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class SalesRepCommissionEntryConfiguration : IEntityTypeConfiguration<SalesRepCommissionEntry>
{
    public void Configure(EntityTypeBuilder<SalesRepCommissionEntry> builder)
    {
        builder.ToTable("SalesRepCommissionEntries");

        builder.Property(x => x.CommissionType).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.BaseAmount).HasPrecision(18, 2);
        builder.Property(x => x.CommissionAmount).HasPrecision(18, 2);
        builder.Property(x => x.PaidAmount).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.Ignore(x => x.UnpaidAmount);

        builder.HasOne(x => x.SalesRepresentative)
            .WithMany(r => r.CommissionEntries)
            .HasForeignKey(x => x.SalesRepresentativeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Invoice)
            .WithMany()
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SalesRepresentativeId);
        builder.HasIndex(x => x.InvoiceId);
        builder.HasIndex(x => new { x.SalesRepresentativeId, x.InvoiceDate });
        builder.HasIndex(x => new { x.SalesRepresentativeId, x.Status });
    }
}
