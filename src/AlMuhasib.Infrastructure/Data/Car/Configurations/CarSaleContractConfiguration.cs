using AlMuhasib.Core.Entities.Car;
using AlMuhasib.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Car.Configurations;

public class CarSaleContractConfiguration : IEntityTypeConfiguration<CarSaleContract>
{
    public void Configure(EntityTypeBuilder<CarSaleContract> builder)
    {
        builder.ToTable("CarSaleContracts");

        builder.Property(c => c.ContractNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => c.ContractNumber).IsUnique();

        builder.Property(c => c.SellerName).IsRequired().HasMaxLength(200);
        builder.Property(c => c.SellerAddress).HasMaxLength(500);
        builder.Property(c => c.SellerIdNumber).HasMaxLength(50);
        builder.Property(c => c.SellerPhone).HasMaxLength(50);

        builder.Property(c => c.BuyerName).IsRequired().HasMaxLength(200);
        builder.Property(c => c.BuyerAddress).HasMaxLength(500);
        builder.Property(c => c.BuyerIdNumber).HasMaxLength(50);
        builder.Property(c => c.BuyerPhone).HasMaxLength(50);

        builder.Property(c => c.AnnualOwnerName).HasMaxLength(200);
        builder.Property(c => c.AnnualOwnerAddress).HasMaxLength(500);

        builder.Property(c => c.PlateNumber).HasMaxLength(30);
        builder.Property(c => c.CarType).HasMaxLength(100);
        builder.Property(c => c.CarModel).HasMaxLength(100);
        builder.Property(c => c.CarColor).HasMaxLength(50);
        builder.Property(c => c.ChassisNumber).HasMaxLength(100);

        builder.Property(c => c.CarPrice).HasPrecision(18, 2);
        builder.Property(c => c.AmountReceived).HasPrecision(18, 2);
        builder.Property(c => c.RemainingAmount).HasPrecision(18, 2);
        builder.Property(c => c.CarPriceInWords).HasMaxLength(1000);
        builder.Property(c => c.Notes).HasMaxLength(2000);

        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(c => c.ContractDate);
        builder.HasIndex(c => c.Status);
    }
}

public class CarContractPaymentConfiguration : IEntityTypeConfiguration<CarContractPayment>
{
    public void Configure(EntityTypeBuilder<CarContractPayment> builder)
    {
        builder.ToTable("CarContractPayments");

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
