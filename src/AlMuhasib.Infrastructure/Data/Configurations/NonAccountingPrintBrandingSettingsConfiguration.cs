using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

/// <summary>
/// إعدادات الطباعة للأنظمة غير المحاسبية — بدون BranchId
/// (الكيان يرث BranchScopedEntity للمحاسبة فقط).
/// </summary>
public sealed class NonAccountingPrintBrandingSettingsConfiguration
    : IEntityTypeConfiguration<PrintBrandingSettings>
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
        builder.Property(x => x.CompanyIdNumber).HasMaxLength(100);
        builder.Property(x => x.CompanyIdIssuer).HasMaxLength(200);
        builder.Property(x => x.FooterText).HasMaxLength(1000);
        builder.Property(x => x.HeaderImageContentType).HasMaxLength(50);
        builder.Property(x => x.FooterImageContentType).HasMaxLength(50);

        builder.Ignore(x => x.BranchId);
        builder.Ignore(x => x.Branch);
    }
}
