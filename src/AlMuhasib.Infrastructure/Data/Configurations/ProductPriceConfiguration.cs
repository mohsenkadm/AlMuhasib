using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class ProductPriceConfiguration : IEntityTypeConfiguration<ProductPrice>
{
    public void Configure(EntityTypeBuilder<ProductPrice> builder)
    {
        builder.ToTable("ProductPrices");

        builder.Property(x => x.SalePrice).HasPrecision(18, 2);
        builder.Property(x => x.PurchasePrice).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.ProductId, x.PricingTypeId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.Product)
            .WithMany(p => p.ProductPrices)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PricingType)
            .WithMany(t => t.ProductPrices)
            .HasForeignKey(x => x.PricingTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
