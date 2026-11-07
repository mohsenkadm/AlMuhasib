using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.UI.Services;

/// <summary>
/// Builds and normalizes permission rows against <see cref="ScreenPermissionRegistry"/>.
/// </summary>
public static class PermissionCatalogHelper
{
    private static readonly string[] AdminRecoveryScreens = ["Users", "Permissions"];

    private static readonly HashSet<string> CashierGrantedScreens = new(StringComparer.Ordinal)
    {
        ScreenPermissionRegistry.Dashboard,
        ScreenPermissionRegistry.CashierPos,
        ScreenPermissionRegistry.BarcodePrinting,
        ScreenPermissionRegistry.DailyOperationsReport,
        ScreenPermissionRegistry.SalesReturn
    };

    /// <summary>
    /// صلاحيات نطاق الفروع — لا تُمنح تلقائياً مع الكتالوج الكامل أو «تحديد الكل».
    /// مصدر وصول الفروع = ربط المستخدم بالفروع في شاشة المستخدمين.
    /// </summary>
    public static bool IsExplicitBranchScopeScreen(string screenName) =>
        screenName is BranchPermissionScreens.ViewAllBranches
            or BranchPermissionScreens.ManageAllBranches;

    public static string GetRoleDisplayName(UserRole role) => role switch
    {
        UserRole.Admin => "مدير",
        UserRole.Cashier => "كاشير",
        _ => "مستخدم"
    };

    public static string GetRoleHeaderLabel(UserRole role) => role switch
    {
        UserRole.Admin => "مدير النظام",
        UserRole.Cashier => "كاشير",
        _ => "مستخدم"
    };

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

    private static Permission CreateCashierScreen(string screenName)
    {
        return screenName switch
        {
            ScreenPermissionRegistry.Dashboard => new Permission
            {
                ScreenName = screenName,
                CanView = true
            },
            ScreenPermissionRegistry.CashierPos => new Permission
            {
                ScreenName = screenName,
                CanView = true,
                CanAdd = true,
                CanPrint = true
            },
            ScreenPermissionRegistry.BarcodePrinting => new Permission
            {
                ScreenName = screenName,
                CanView = true,
                CanPrint = true
            },
            ScreenPermissionRegistry.DailyOperationsReport => new Permission
            {
                ScreenName = screenName,
                CanView = true,
                CanPrint = true
            },
            ScreenPermissionRegistry.SalesReturn => new Permission
            {
                ScreenName = screenName,
                CanView = true,
                CanAdd = true
            },
            _ => CreateDenied(screenName)
        };
    }

    private static IReadOnlyList<(string Name, string Label)> AllScreens =>
        ScreenPermissionRegistry.AllScreens;

    public static List<Permission> CreateFullCatalog() =>
        ScreenPermissionRegistry.AllScreens
            .Select(s => IsExplicitBranchScopeScreen(s.Name)
                ? CreateDenied(s.Name)
                : CreateFull(s.Name))
            .ToList();

    /// <summary>قالب صلاحيات دور الكاشير الافتراضي.</summary>
    public static List<Permission> CreateCashierCatalog() =>
        ScreenPermissionRegistry.AllScreens
            .Select(s => CashierGrantedScreens.Contains(s.Name)
                ? CreateCashierScreen(s.Name)
                : CreateDenied(s.Name))
            .ToList();

    /// <summary>
    /// Ensures login permissions match the registry. May grant admin recovery access
    /// or seed cashier defaults / migrate SaleInvoice → CashierPos.
    /// </summary>
    public static (List<Permission> Permissions, bool ShouldSave, string? InfoMessage, string? WarningMessage)
        NormalizeForLogin(UserRole role, List<Permission> existing)
    {
        var isAdmin = role == UserRole.Admin;
        var isCashier = role == UserRole.Cashier;

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

            if (isCashier)
            {
                return (
                    CreateCashierCatalog(),
                    true,
                    "تم تفعيل صلاحيات الكاشير الافتراضية (POS، طباعة باركودات، التقرير اليومي، مرتجع مبيعات).",
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
        string? info = null;

        // مستخدمو SaleInvoice الحاليون يحتفظون بـ POS عبر مفتاح CashierPos الجديد.
        if (byScreen.TryGetValue("SaleInvoice", out var saleInvoice)
            && saleInvoice.CanView
            && !byScreen.ContainsKey(ScreenPermissionRegistry.CashierPos))
        {
            byScreen[ScreenPermissionRegistry.CashierPos] = new Permission
            {
                ScreenName = ScreenPermissionRegistry.CashierPos,
                CanView = saleInvoice.CanView,
                CanAdd = saleInvoice.CanAdd,
                CanEdit = saleInvoice.CanEdit,
                CanDelete = saleInvoice.CanDelete,
                CanPrint = saleInvoice.CanPrint,
                CanExport = saleInvoice.CanExport,
                CanEditPrice = saleInvoice.CanEditPrice,
                IsViewOnly = saleInvoice.IsViewOnly
            };
            changed = true;
            info = "تمت إضافة صلاحية «بيع سريع (POS)» تلقائياً بناءً على صلاحية فاتورة المبيعات.";
        }

        // من لديهم تقارير عامة يحصلون على التقرير اليومي المنفصل إن لم يكن موجوداً.
        if (byScreen.TryGetValue(ScreenPermissionRegistry.Reports, out var reports)
            && reports.CanView
            && !byScreen.ContainsKey(ScreenPermissionRegistry.DailyOperationsReport))
        {
            byScreen[ScreenPermissionRegistry.DailyOperationsReport] = new Permission
            {
                ScreenName = ScreenPermissionRegistry.DailyOperationsReport,
                CanView = reports.CanView,
                CanPrint = reports.CanPrint,
                CanExport = reports.CanExport
            };
            changed = true;
        }

        foreach (var (name, _) in ScreenPermissionRegistry.AllScreens)
        {
            if (byScreen.ContainsKey(name))
                continue;

            byScreen[name] = isAdmin && !IsExplicitBranchScopeScreen(name)
                ? CreateFull(name)
                : CreateDenied(name);
            changed = true;
        }

        if (isAdmin && !CanViewScreen(byScreen.Values, "Users"))
        {
            foreach (var screen in AdminRecoveryScreens)
                byScreen[screen] = CreateFull(screen);

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
                info ?? "تمت إضافة شاشات جديدة إلى سجل الصلاحيات (بدون عرض حتى تحفظها من شاشة «الصلاحيات»).",
                null);
        }

        return (existing, false, null, null);
    }

    /// <summary>توافق مع الاستدعاءات القديمة.</summary>
    public static (List<Permission> Permissions, bool ShouldSave, string? InfoMessage, string? WarningMessage)
        NormalizeForLogin(bool isAdmin, List<Permission> existing) =>
        NormalizeForLogin(isAdmin ? UserRole.Admin : UserRole.User, existing);

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
