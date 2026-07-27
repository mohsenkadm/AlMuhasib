using AlMuhasib.Core.Enums;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using AlMuhasib.UI.ViewModels;
using AlMuhasib.UI.ViewModels.RealEstate;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Modules;

public sealed class RealEstateContractsSystemModule : ISystemModule
{
    public ApplicationSystemType SystemType => ApplicationSystemType.RealEstateContracts;
    public string DisplayName => "نظام عقود العقارات";
    public Type DashboardViewModelType => typeof(RealEstateDashboardViewModel);
    public Type? SetupWizardViewModelType => null;

    public IReadOnlyList<(string Name, string Label)> PermissionScreens { get; } =
    [
        (RealEstatePermissionRegistry.Dashboard, "لوحة التحكم"),
        (RealEstatePermissionRegistry.ContractForm, "عقد جديد"),
        (RealEstatePermissionRegistry.Contracts, "العقود"),
        (RealEstatePermissionRegistry.Debts, "كشف المدينين"),
        (RealEstatePermissionRegistry.Parties, "الزبائن"),
        (RealEstatePermissionRegistry.Expenses, "المصاريف"),
        (RealEstatePermissionRegistry.Reports, "التقارير"),
        (RealEstatePermissionRegistry.ProfitReport, "تقرير الأرباح"),
        (RealEstatePermissionRegistry.ClauseTemplates, "بنود العقد"),
        (RealEstatePermissionRegistry.Users, "المستخدمون"),
        (RealEstatePermissionRegistry.Permissions, "الصلاحيات"),
        (RealEstatePermissionRegistry.PrintSettings, "إعدادات الطباعة"),
        (RealEstatePermissionRegistry.Backup, "النسخ الاحتياطي"),
        (RealEstatePermissionRegistry.CloudSync, "المزامنة السحابية"),
        (ScreenPermissionRegistry.NetworkConnection, "ربط الحاسبات"),
        (ScreenPermissionRegistry.SystemUpdate, "تحديث النظام")
    ];

    public IReadOnlyList<NavigationMenuItem> BuildMenuItems()
    {
        var items = new List<NavigationMenuItem>
        {
            Item("لوحة التحكم", PackIconKind.ViewDashboard, typeof(RealEstateDashboardViewModel), RealEstatePermissionRegistry.Dashboard),
            FlyoutGroup(
                key: "contracts",
                title: "العقود",
                icon: PackIconKind.FileDocumentMultiple,
                accent: "#1565C0",
                accentLight: "#E3F2FD",
                [
                    ("عقد جديد", PackIconKind.FileDocumentPlus, typeof(RealEstateContractFormViewModel), RealEstatePermissionRegistry.ContractForm),
                    ("قائمة العقود", PackIconKind.FormatListBulleted, typeof(RealEstateContractsViewModel), RealEstatePermissionRegistry.Contracts),
                ]),
            FlyoutGroup(
                key: "parties",
                title: "الأطراف والديون",
                icon: PackIconKind.AccountCash,
                accent: "#C62828",
                accentLight: "#FFEBEE",
                [
                    ("الزبائن", PackIconKind.AccountGroup, typeof(RealEstatePartiesViewModel), RealEstatePermissionRegistry.Parties),
                    ("كشف المدينين", PackIconKind.CashClock, typeof(RealEstateDebtsViewModel), RealEstatePermissionRegistry.Debts),
                ]),
            FlyoutGroup(
                key: "finance",
                title: "المصاريف والأرباح",
                icon: PackIconKind.CashMultiple,
                accent: "#6A1B9A",
                accentLight: "#F3E5F5",
                [
                    ("المصاريف", PackIconKind.CashMinus, typeof(RealEstateExpensesViewModel), RealEstatePermissionRegistry.Expenses),
                    ("تقرير الأرباح", PackIconKind.Finance, typeof(RealEstateProfitReportViewModel), RealEstatePermissionRegistry.ProfitReport),
                ]),
            FlyoutGroup(
                key: "reports",
                title: "التقارير",
                icon: PackIconKind.ChartBar,
                accent: "#2E7D32",
                accentLight: "#E8F5E9",
                [
                    ("تقرير العقود", PackIconKind.ChartBar, typeof(RealEstateContractsReportViewModel), RealEstatePermissionRegistry.Reports),
                    ("تقرير الأرباح", PackIconKind.ChartTimeline, typeof(RealEstateProfitReportViewModel), RealEstatePermissionRegistry.ProfitReport),
                ]),
            FlyoutGroup(
                key: "system",
                title: "النظام والإعدادات",
                icon: PackIconKind.CogOutline,
                accent: "#455A64",
                accentLight: "#ECEFF1",
                [
                    ("بنود العقد", PackIconKind.FormatListNumbered, typeof(RealEstateClauseTemplatesViewModel), RealEstatePermissionRegistry.ClauseTemplates),
                    ("المستخدمون", PackIconKind.AccountMultiple, typeof(UsersViewModel), RealEstatePermissionRegistry.Users),
                    ("الصلاحيات", PackIconKind.ShieldKey, typeof(PermissionsViewModel), RealEstatePermissionRegistry.Permissions),
                    ("إعدادات الطباعة", PackIconKind.PrinterSettings, typeof(PrintLayoutSettingsViewModel), RealEstatePermissionRegistry.PrintSettings),
                    ("النسخ الاحتياطي", PackIconKind.DatabaseCog, typeof(BackupRestoreViewModel), RealEstatePermissionRegistry.Backup),
                    ("المزامنة السحابية", PackIconKind.CloudSync, typeof(CloudSyncSettingsViewModel), RealEstatePermissionRegistry.CloudSync),
                    ("ربط الحاسبات", PackIconKind.LanConnect, typeof(NetworkConnectionSettingsViewModel), ScreenPermissionRegistry.NetworkConnection),
                    ("تحديث النظام", PackIconKind.CloudDownload, typeof(SystemUpdateViewModel), ScreenPermissionRegistry.SystemUpdate),
                    ("تبديل النظام (مطور)", PackIconKind.DeveloperBoard, typeof(DeveloperSystemSwitchViewModel), ScreenPermissionRegistry.DeveloperSystem),
                ])
        };

        if (items.Count > 0)
            items[0].IsSelected = true;

        return items;
    }

    public string GetScreenName(Type viewModelType) =>
        RealEstatePermissionRegistry.GetScreenName(viewModelType);

    public string GetLabel(string screenName) =>
        RealEstatePermissionRegistry.GetLabel(screenName);

    public Type? GetDefaultViewModelType(string screenName) =>
        RealEstatePermissionRegistry.GetDefaultViewModelType(screenName);

    private static NavigationMenuItem Item(string title, PackIconKind icon, Type viewModelType, string screenName) =>
        new()
        {
            Title = title,
            Icon = icon,
            ViewModelType = viewModelType,
            ScreenName = screenName
        };

    private static NavigationMenuItem FlyoutGroup(
        string key,
        string title,
        PackIconKind icon,
        string accent,
        string accentLight,
        (string Title, PackIconKind Icon, Type Vm, string Screen)[] children)
    {
        var group = new NavigationMenuItem
        {
            Title = title,
            Icon = icon,
            IsReportCategory = true,
            CategoryKey = key,
            ScreenName = $"MenuGroup:{key}",
            CategoryAccentColor = accent,
            CategoryAccentLightColor = accentLight,
            FlyoutItemLabel = "شاشة"
        };

        foreach (var child in children)
        {
            group.Children.Add(new NavigationMenuItem
            {
                Title = child.Title,
                Icon = child.Icon,
                ViewModelType = child.Vm,
                ScreenName = child.Screen
            });
        }

        return group;
    }
}
