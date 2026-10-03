using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Data;

/// <summary>
/// الأنظمة غير المحاسبية لا تملك جداول/أعمدة تعدد الفروع على الكيانات المشتركة.
/// يجب استدعاء هذا بعد ApplyConfiguration للكيانات المشتركة.
/// </summary>
public static class NonAccountingEntityModel
{
    public static void IgnoreSharedBranchProperties(ModelBuilder modelBuilder)
    {
        // يجب تجاهل التنقل Branch أيضاً وإلا يعيد EF إنشاء ظلّ BranchId بعد Ignore للخاصية.
        modelBuilder.Entity<PrintBrandingSettings>().Ignore(e => e.BranchId);
        modelBuilder.Entity<PrintBrandingSettings>().Ignore(e => e.Branch);

        modelBuilder.Entity<AuditLog>().Ignore(e => e.BranchId);
        modelBuilder.Entity<AuditLog>().Ignore(e => e.Branch);

        // أعمدة أُضيفت للمحاسبة فقط — غير موجودة في مخططات الأنظمة الأخرى
        modelBuilder.Entity<AuditLog>().Ignore(e => e.IpAddress);
        modelBuilder.Entity<AuditLog>().Ignore(e => e.DeviceInfo);

        modelBuilder.Ignore<Branch>();
        modelBuilder.Ignore<UserBranch>();
    }
}
