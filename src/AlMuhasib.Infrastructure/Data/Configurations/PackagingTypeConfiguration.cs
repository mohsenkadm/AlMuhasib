using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class PackagingTypeConfiguration : IEntityTypeConfiguration<PackagingType>
{
    public void Configure(EntityTypeBuilder<PackagingType> builder)
    {
        builder.ToTable("PackagingTypes");

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(x => x.Name);
    }
}
