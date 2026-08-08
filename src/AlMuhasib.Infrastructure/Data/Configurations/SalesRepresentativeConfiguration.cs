using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class SalesRepresentativeConfiguration : IEntityTypeConfiguration<SalesRepresentative>
{
    public void Configure(EntityTypeBuilder<SalesRepresentative> builder)
    {
        builder.ToTable("SalesRepresentatives");

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(20);
        builder.Property(x => x.Region).HasMaxLength(200);
        builder.Property(x => x.CompensationNotes).HasMaxLength(1000);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.MonthlySalary).HasPrecision(18, 2);

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.Region);
    }
}
