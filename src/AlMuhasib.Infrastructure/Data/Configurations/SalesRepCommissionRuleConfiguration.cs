using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class SalesRepCommissionRuleConfiguration : IEntityTypeConfiguration<SalesRepCommissionRule>
{
    public void Configure(EntityTypeBuilder<SalesRepCommissionRule> builder)
    {
        builder.ToTable("SalesRepCommissionRules");

        builder.Property(x => x.CommissionType).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.Percentage).HasPrecision(18, 4);
        builder.Property(x => x.FixedAmount).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasOne(x => x.SalesRepresentative)
            .WithMany(r => r.CommissionRules)
            .HasForeignKey(x => x.SalesRepresentativeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SalesRepresentativeId);
        builder.HasIndex(x => new { x.SalesRepresentativeId, x.CommissionType, x.IsActive });
    }
}
