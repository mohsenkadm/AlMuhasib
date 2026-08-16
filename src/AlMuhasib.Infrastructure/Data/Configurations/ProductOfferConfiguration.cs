using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class ProductOfferConfiguration : IEntityTypeConfiguration<ProductOffer>
{
    public void Configure(EntityTypeBuilder<ProductOffer> builder)
    {
        builder.ToTable("ProductOffers");

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.TriggerQuantity).HasPrecision(18, 3);
        builder.Property(x => x.GiftQuantity).HasPrecision(18, 3);

        builder.HasOne(x => x.TriggerProduct)
            .WithMany()
            .HasForeignKey(x => x.TriggerProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.GiftProduct)
            .WithMany()
            .HasForeignKey(x => x.GiftProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.TriggerProductId);
        builder.HasIndex(x => x.IsActive);
    }
}
