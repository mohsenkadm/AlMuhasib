using AlMuhasib.Core.Entities;

namespace AlMuhasib.UI.Services;

/// <summary>
/// Builds and normalizes permission rows against <see cref="ScreenPermissionRegistry"/>.
/// </summary>
public static class PermissionCatalogHelper
{
    private static readonly string[] AdminRecoveryScreens = ["Users", "Permissions"];

    public static Permission CreateFull(string screenName) => new()
    {
        ScreenName = screenName,
        CanView = true,
        CanAdd = true,
        CanEdit = true,
        CanDelete = true,
        CanPrint = true,
        CanExport = true,
        CanEditPrice = true,
        IsViewOnly = false
    };

    public static Permission CreateDenied(string screenName)
    {
        var dashboardScreen = AllScreens.FirstOrDefault().Name ?? ScreenPermissionRegistry.Dashboard;
        return new Permission
        {
            ScreenName = screenName,
            CanView = screenName == dashboardScreen,
            CanAdd = false,
            CanEdit = false,
            CanDelete = false,
            CanPrint = false,
            CanExport = false,
            CanEditPrice = false,
            IsViewOnly = false
        };
    }

    private static IReadOnlyList<(string Name, string Label)> AllScreens =>
        ScreenPermissionRegistry.AllScreens;

    public static List<Permission> CreateFullCatalog() =>
        ScreenPermissionRegistry.AllScreens.Select(s => CreateFull(s.Name)).ToList();

    /// <summary>
    /// Ensures login permissions match the registry. May grant admin recovery access.
    /// </summary>
    public static (List<Permission> Permissions, bool ShouldSave, string? InfoMessage, string? WarningMessage)
        NormalizeForLogin(bool isAdmin, List<Permission> existing)
    {
        if (existing.Count == 0)
        {
            if (isAdmin)
            {
                return (
                    CreateFullCatalog(),
                    true,
                    "تم تفعيل صلاحيات كاملة تلقائياً لهذا المدير. يمكنك تخصيصها من شاشة «الصلاحيات».",
                    null);
            }

            return (
                existing,
                false,
                null,
                "لا توجد صلاحيات محفوظة لهذا المستخدم. اطلب من المدير ضبط الصلاحيات من شاشة «الصلاحيات».");
        }

        var byScreen = existing.ToDictionary(p => p.ScreenName, StringComparer.Ordinal);
        var changed = false;

        foreach (var (name, _) in ScreenPermissionRegistry.AllScreens)
        {
            if (byScreen.ContainsKey(name))
                continue;

            byScreen[name] = isAdmin ? CreateFull(name) : CreateDenied(name);
            changed = true;
        }

        if (isAdmin && !CanViewScreen(existing, "Users"))
        {
            foreach (var screen in AdminRecoveryScreens)
                byScreen[screen] = CreateFull(screen);

            changed = true;
            return (
                byScreen.Values.ToList(),
                true,
                "تم تفعيل صلاحيات «المستخدمون» و«الصلاحيات» تلقائياً لتمكين إدارة النظام.",
                null);
        }

        if (changed)
        {
            return (
                byScreen.Values.ToList(),
                true,
                "تمت إضافة شاشات جديدة إلى سجل الصلاحيات (بدون عرض حتى تحفظها من شاشة «الصلاحيات»).",
                null);
        }

        return (existing, false, null, null);
    }

    public static PermissionCoverageReport AnalyzeCoverage(IEnumerable<Permission> permissions)
    {
        var registryNames = ScreenPermissionRegistry.AllScreens
            .Select(s => s.Name)
            .ToHashSet(StringComparer.Ordinal);

        var savedNames = permissions
            .Select(p => p.ScreenName)
            .ToHashSet(StringComparer.Ordinal);

        return new PermissionCoverageReport
        {
            RegistryCount = registryNames.Count,
            SavedCount = savedNames.Count,
            MissingInDatabase = registryNames.Except(savedNames).OrderBy(n => n).ToList(),
            UnknownInDatabase = savedNames.Except(registryNames).OrderBy(n => n).ToList()
        };
    }

    private static bool CanViewScreen(IEnumerable<Permission> permissions, string screenName) =>
        permissions.Any(p =>
            string.Equals(p.ScreenName, screenName, StringComparison.Ordinal) && p.CanView);
}

public sealed class PermissionCoverageReport
{
    public int RegistryCount { get; init; }
    public int SavedCount { get; init; }
    public IReadOnlyList<string> MissingInDatabase { get; init; } = [];
    public IReadOnlyList<string> UnknownInDatabase { get; init; } = [];

    public bool IsComplete => MissingInDatabase.Count == 0 && UnknownInDatabase.Count == 0;

    public string ToDisplayMessage(string userDisplayName)
    {
        var lines = new List<string>
        {
            $"المستخدم: {userDisplayName}",
            $"الشاشات المرجعية في النظام: {RegistryCount}",
            $"التسميات المحفوظة في قاعدة البيانات: {SavedCount}",
            ""
        };

        if (IsComplete)
        {
            lines.Add("النتيجة: متطابقة بالكامل — كل الشاشات موجودة بأسماء صحيحة.");
            return string.Join(Environment.NewLine, lines);
        }

        if (MissingInDatabase.Count > 0)
        {
            lines.Add($"شاشات ناقصة في قاعدة البيانات ({MissingInDatabase.Count}):");
            foreach (var name in MissingInDatabase)
                lines.Add($"  • {name} — {ScreenPermissionRegistry.GetLabel(name)}");
            lines.Add("");
        }

        if (UnknownInDatabase.Count > 0)
        {
            lines.Add($"تسميات قديمة/غير معروفة في قاعدة البيانات ({UnknownInDatabase.Count}):");
            foreach (var name in UnknownInDatabase)
                lines.Add($"  • {name}");
            lines.Add("");
            lines.Add("احفظ الصلاحيات من هذه الشاشة لإزالة التسميات القديمة.");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
