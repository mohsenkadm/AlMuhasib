using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.ToTable("InvoiceItems");

        builder.Property(ii => ii.ItemName)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(ii => ii.Quantity).HasPrecision(18, 4);
        builder.Property(ii => ii.UnitPrice).HasPrecision(18, 2);
        builder.Property(ii => ii.DiscountAmount).HasPrecision(18, 2);
        builder.Property(ii => ii.TotalPrice).HasPrecision(18, 2);

        builder.HasOne(ii => ii.Invoice)
            .WithMany(i => i.Items)
            .HasForeignKey(ii => ii.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ii => ii.Product)
            .WithMany(p => p.InvoiceItems)
            .HasForeignKey(ii => ii.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ii => ii.PricingType)
            .WithMany()
            .HasForeignKey(ii => ii.PricingTypeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
