using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class PricingTypeConfiguration : IEntityTypeConfiguration<PricingType>
{
    public void Configure(EntityTypeBuilder<PricingType> builder)
    {
        builder.ToTable("PricingTypes");

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(x => x.Name);
    }
}
