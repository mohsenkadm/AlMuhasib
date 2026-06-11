using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class WarehouseTransferConfiguration : IEntityTypeConfiguration<WarehouseTransfer>
{
    public void Configure(EntityTypeBuilder<WarehouseTransfer> builder)
    {
        builder.ToTable("WarehouseTransfers");

        builder.Property(t => t.TransferNumber).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Notes).HasMaxLength(500);

        builder.HasOne(t => t.FromWarehouse)
            .WithMany()
            .HasForeignKey(t => t.FromWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ToWarehouse)
            .WithMany()
            .HasForeignKey(t => t.ToWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Items)
            .WithOne(i => i.WarehouseTransfer)
            .HasForeignKey(i => i.WarehouseTransferId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WarehouseTransferItemConfiguration : IEntityTypeConfiguration<WarehouseTransferItem>
{
    public void Configure(EntityTypeBuilder<WarehouseTransferItem> builder)
    {
        builder.ToTable("WarehouseTransferItems");

        builder.Property(i => i.Quantity).HasPrecision(18, 2);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
