using AlMuhasib.Core.Entities.RealEstate;
using AlMuhasib.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.RealEstate.Configurations;

public class RealEstateContractConfiguration : IEntityTypeConfiguration<RealEstateContract>
{
    public void Configure(EntityTypeBuilder<RealEstateContract> builder)
    {
        builder.ToTable("RealEstateContracts");

        builder.Property(c => c.ContractNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => c.ContractNumber).IsUnique();

        builder.Property(c => c.PropertyLocation).HasMaxLength(200);
        builder.Property(c => c.PropertyAddress).HasMaxLength(500);
        builder.Property(c => c.PropertyDescription).HasMaxLength(2000);
        builder.Property(c => c.PropertyAreaSqm).HasPrecision(18, 2);

        builder.Property(c => c.SellerName).IsRequired().HasMaxLength(200);
        builder.Property(c => c.SellerAddress).HasMaxLength(500);
        builder.Property(c => c.SellerIdNumber).HasMaxLength(50);
        builder.Property(c => c.SellerPhone).HasMaxLength(50);

        builder.Property(c => c.BuyerName).IsRequired().HasMaxLength(200);
        builder.Property(c => c.BuyerAddress).HasMaxLength(500);
        builder.Property(c => c.BuyerIdNumber).HasMaxLength(50);
        builder.Property(c => c.BuyerPhone).HasMaxLength(50);

        builder.Property(c => c.TotalPrice).HasPrecision(18, 2);
        builder.Property(c => c.DownPayment).HasPrecision(18, 2);
        builder.Property(c => c.AmountPaid).HasPrecision(18, 2);
        builder.Property(c => c.RemainingAmount).HasPrecision(18, 2);
        builder.Property(c => c.TotalPriceInWords).HasMaxLength(1000);
        builder.Property(c => c.WitnessOneName).HasMaxLength(200);
        builder.Property(c => c.WitnessTwoName).HasMaxLength(200);
        builder.Property(c => c.Notes).HasMaxLength(2000);

        builder.Property(c => c.ContractType).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.PropertyType).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.PaymentMode).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.DebtorParty).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(c => c.ContractDate);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.PaymentMode);
        builder.HasIndex(c => c.DueDate);
    }
}

public class RealEstateContractPaymentConfiguration : IEntityTypeConfiguration<RealEstateContractPayment>
{
    public void Configure(EntityTypeBuilder<RealEstateContractPayment> builder)
    {
        builder.ToTable("RealEstateContractPayments");

        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.RemainingBefore).HasPrecision(18, 2);
        builder.Property(p => p.RemainingAfter).HasPrecision(18, 2);
        builder.Property(p => p.Notes).HasMaxLength(1000);

        builder.HasOne(p => p.Contract)
            .WithMany(c => c.Payments)
            .HasForeignKey(p => p.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RealEstateContractClauseConfiguration : IEntityTypeConfiguration<RealEstateContractClause>
{
    public void Configure(EntityTypeBuilder<RealEstateContractClause> builder)
    {
        builder.ToTable("RealEstateContractClauses");

        builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Body).IsRequired().HasMaxLength(4000);

        builder.HasOne(c => c.Contract)
            .WithMany(c => c.Clauses)
            .HasForeignKey(c => c.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RealEstateClauseTemplateConfiguration : IEntityTypeConfiguration<RealEstateClauseTemplate>
{
    public void Configure(EntityTypeBuilder<RealEstateClauseTemplate> builder)
    {
        builder.ToTable("RealEstateClauseTemplates");

        builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Body).IsRequired().HasMaxLength(4000);
        builder.HasIndex(c => c.SortOrder);
    }
}

public class RealEstatePartyConfiguration : IEntityTypeConfiguration<RealEstateParty>
{
    public void Configure(EntityTypeBuilder<RealEstateParty> builder)
    {
        builder.ToTable("RealEstateParties");

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Phone).HasMaxLength(50);
        builder.Property(p => p.Address).HasMaxLength(500);
        builder.Property(p => p.IdNumber).HasMaxLength(50);
        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.Phone);
    }
}
