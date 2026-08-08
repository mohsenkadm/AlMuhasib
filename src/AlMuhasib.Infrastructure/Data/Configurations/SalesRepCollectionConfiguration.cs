using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class SalesRepCollectionConfiguration : IEntityTypeConfiguration<SalesRepCollection>
{
    public void Configure(EntityTypeBuilder<SalesRepCollection> builder)
    {
        builder.ToTable("SalesRepCollections");

        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.HandedOverAmount).HasPrecision(18, 2);
        builder.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.ReceiptNumber).HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.Ignore(x => x.PendingHandoverAmount);

        builder.HasOne(x => x.SalesRepresentative)
            .WithMany(r => r.Collections)
            .HasForeignKey(x => x.SalesRepresentativeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Invoice)
            .WithMany()
            .HasForeignKey(x => x.InvoiceId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SalesRepresentativeId);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.CollectionDate);
        builder.HasIndex(x => x.ReceiptNumber);
    }
}
