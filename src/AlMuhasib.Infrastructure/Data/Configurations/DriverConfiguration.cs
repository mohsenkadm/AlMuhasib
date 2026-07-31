using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("Drivers");

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Phone)
            .HasMaxLength(20);

        builder.Property(d => d.Address)
            .HasMaxLength(500);

        builder.Property(d => d.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(d => d.Name);
    }
}
