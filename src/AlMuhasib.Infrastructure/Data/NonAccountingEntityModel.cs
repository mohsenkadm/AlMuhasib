using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Data;

/// <summary>
/// الأنظمة غير المحاسبية لا تملك جداول/أعمدة تعدد الفروع على الكيانات المشتركة.
/// </summary>
public static class NonAccountingEntityModel
{
    /// <summary>
    /// كيانات الجذر المشتركة المسموحة في قواعد الفندق/السيارات/الذهب/… (ليست مخطط المحاسبة).
    /// </summary>
    private static readonly HashSet<Type> SharedAllowedTypes =
    [
        typeof(User),
        typeof(Permission),
        typeof(AuditLog),
        typeof(PrintBrandingSettings),
        typeof(CloudSyncSettings),
        typeof(SyncState),
    ];

    /// <summary>
    /// يُستدعى في بداية OnModelCreating — قبل أي ApplyConfiguration.
    /// يمنع سحب مخطط المحاسبة عبر تنقلات User/Branch أو أي مسار اكتشاف آخر.
    /// </summary>
    public static void BlockAccountingGraphDiscovery(ModelBuilder modelBuilder)
    {
        // Ignore أنواع المحاسبة في مساحة الأسماء الجذرية أولاً (قبل Entity&lt;User&gt;).
        foreach (var type in typeof(BaseEntity).Assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition)
                continue;
            if (type.Namespace != "AlMuhasib.Core.Entities")
                continue;
            if (SharedAllowedTypes.Contains(type))
                continue;
            // قواعد مجردة / مساعدة ليست كيانات جداول
            if (type == typeof(BaseEntity) || type == typeof(BranchScopedEntity))
                continue;

            modelBuilder.Ignore(type);
        }

        modelBuilder.Entity<User>().Ignore(u => u.UserBranches);
        modelBuilder.Entity<User>().Ignore(u => u.Tasks);
        modelBuilder.Entity<User>().Ignore(u => u.Notes);
        modelBuilder.Entity<PrintBrandingSettings>().Ignore(e => e.BranchId);
        modelBuilder.Entity<PrintBrandingSettings>().Ignore(e => e.Branch);
        modelBuilder.Entity<AuditLog>().Ignore(e => e.BranchId);
        modelBuilder.Entity<AuditLog>().Ignore(e => e.Branch);
        modelBuilder.Entity<AuditLog>().Ignore(e => e.IpAddress);
        modelBuilder.Entity<AuditLog>().Ignore(e => e.DeviceInfo);
    }

    /// <summary>يُستدعى بعد ApplyConfiguration للكيانات المشتركة.</summary>
    public static void IgnoreSharedBranchProperties(ModelBuilder modelBuilder)
    {
        BlockAccountingGraphDiscovery(modelBuilder);
    }
}
