using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("Transfers");

        builder.Property(t => t.FromType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.ToType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Property(t => t.Notes).HasMaxLength(1000);

        builder.HasIndex(t => t.Date);
    }
}
