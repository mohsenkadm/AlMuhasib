using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Car.Configurations;

/// <summary>
/// إعداد AuditLog لقاعدة عقود السيارات فقط — بدون BranchId/IpAddress (غير موجودة في مخطط Car).
/// لا تستخدم إعداد المحاسبة المشترك <see cref="Configurations.AuditLogConfiguration"/>.
/// </summary>
public sealed class CarAuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.Property(a => a.Action)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.EntityName)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(a => a.BranchId);
        builder.Ignore(a => a.Branch);
        builder.Ignore(a => a.IpAddress);
        builder.Ignore(a => a.DeviceInfo);

        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => new { a.EntityName, a.EntityId });
        builder.HasIndex(a => a.UserId);
    }
}
