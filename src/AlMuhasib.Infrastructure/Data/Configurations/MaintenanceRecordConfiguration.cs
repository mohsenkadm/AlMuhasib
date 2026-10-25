using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class MaintenanceRecordConfiguration : IEntityTypeConfiguration<MaintenanceRecord>
{
    public void Configure(EntityTypeBuilder<MaintenanceRecord> builder)
    {
        builder.ToTable("MaintenanceRecords");

        builder.Property(m => m.RecordNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.ProductName)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(m => m.SerialNumber)
            .HasMaxLength(100);

        builder.Property(m => m.PreviousSerialNumber)
            .HasMaxLength(100);

        builder.Property(m => m.Notes)
            .HasMaxLength(1000);

        builder.Property(m => m.CostAmount).HasPrecision(18, 2);
        builder.Property(m => m.FxRate).HasPrecision(18, 6);

        builder.HasIndex(m => new { m.BranchId, m.RecordNumber }).IsUnique();
        builder.HasIndex(m => m.Date);
        builder.HasIndex(m => m.CustomerId);
        builder.HasIndex(m => m.SerialNumber);
        builder.HasIndex(m => m.Status);

        builder.HasOne(m => m.Customer)
            .WithMany()
            .HasForeignKey(m => m.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Product)
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.CashBox)
            .WithMany()
            .HasForeignKey(m => m.CashBoxId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.Voucher)
            .WithMany()
            .HasForeignKey(m => m.VoucherId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
