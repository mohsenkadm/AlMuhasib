using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.Barcode)
            .HasMaxLength(50);

        builder.Property(p => p.ScientificName)
            .HasMaxLength(300);

        builder.Property(p => p.UsageInstructions)
            .HasMaxLength(2000);

        builder.Property(p => p.Weight)
            .HasPrecision(18, 4);

        builder.Property(p => p.WeightUnit)
            .HasMaxLength(20);

        builder.Property(p => p.DiscountValue)
            .HasPrecision(18, 2);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.Barcode)
            .IsUnique()
            .HasFilter("[Barcode] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasIndex(p => p.Name);
    }
}
