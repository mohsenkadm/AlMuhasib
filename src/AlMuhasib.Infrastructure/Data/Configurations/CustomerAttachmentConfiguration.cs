using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class CustomerAttachmentConfiguration : IEntityTypeConfiguration<CustomerAttachment>
{
    public void Configure(EntityTypeBuilder<CustomerAttachment> builder)
    {
        builder.Property(a => a.FileName).HasMaxLength(500).IsRequired();
        builder.Property(a => a.FilePath).HasMaxLength(1000).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(500);

        builder.HasOne(a => a.Customer)
            .WithMany(c => c.Attachments)
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
