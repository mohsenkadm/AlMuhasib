using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class OtherIncomeTypeConfiguration : IEntityTypeConfiguration<OtherIncomeType>
{
    public void Configure(EntityTypeBuilder<OtherIncomeType> builder)
    {
        builder.ToTable("OtherIncomeTypes");

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(e => e.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
