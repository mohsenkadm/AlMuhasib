using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Phone)
            .HasMaxLength(20);

        builder.Property(c => c.Address)
            .HasMaxLength(500);

        builder.Property(c => c.FileNumber)
            .HasMaxLength(50);

        builder.Property(c => c.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(c => c.FileNumber)
            .IsUnique()
            .HasFilter("[FileNumber] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasIndex(c => c.Name);
    }
}
