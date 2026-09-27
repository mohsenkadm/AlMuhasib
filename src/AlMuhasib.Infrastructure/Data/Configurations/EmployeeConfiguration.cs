using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(20);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.JobTitle).HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.OpeningBalance).HasPrecision(18, 2);

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.Phone);
    }
}
