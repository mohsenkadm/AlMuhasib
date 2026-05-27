using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class PrintBrandingSettingsConfiguration : IEntityTypeConfiguration<PrintBrandingSettings>
{
    public void Configure(EntityTypeBuilder<PrintBrandingSettings> builder)
    {
        builder.ToTable("PrintBrandingSettings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();

        builder.Property(x => x.CompanyName).HasMaxLength(200);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.PhonePrimary).HasMaxLength(50);
        builder.Property(x => x.PhoneSecondary).HasMaxLength(50);
        builder.Property(x => x.Email).HasMaxLength(120);
        builder.Property(x => x.Details).HasMaxLength(1000);
        builder.Property(x => x.FooterText).HasMaxLength(1000);
        builder.Property(x => x.HeaderImageContentType).HasMaxLength(50);
        builder.Property(x => x.FooterImageContentType).HasMaxLength(50);
    }
}
