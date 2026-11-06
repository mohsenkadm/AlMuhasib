using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class VoucherAttachmentConfiguration : IEntityTypeConfiguration<VoucherAttachment>
{
    public void Configure(EntityTypeBuilder<VoucherAttachment> builder)
    {
        builder.ToTable("VoucherAttachments");

        builder.Property(a => a.FileName).HasMaxLength(500).IsRequired();
        builder.Property(a => a.FilePath).HasMaxLength(1000).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(500);

        builder.HasOne(a => a.Voucher)
            .WithMany(v => v.Attachments)
            .HasForeignKey(a => a.VoucherId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.VoucherId);
        builder.HasIndex(a => a.IsDeleted);
    }
}
