using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class CapitalEntryConfiguration : IEntityTypeConfiguration<CapitalEntry>
{
    public void Configure(EntityTypeBuilder<CapitalEntry> builder)
    {
        builder.ToTable("CapitalEntries");

        builder.Property(ce => ce.Amount).HasPrecision(18, 2);
        builder.Property(ce => ce.Notes).HasMaxLength(1000);

        builder.Property(ce => ce.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(ce => ce.Date);
    }
}
