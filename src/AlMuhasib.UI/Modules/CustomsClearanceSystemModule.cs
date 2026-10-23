using AlMuhasib.Core.Enums;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using AlMuhasib.UI.ViewModels;
using AlMuhasib.UI.ViewModels.Customs;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Modules;

public sealed class CustomsClearanceSystemModule : ISystemModule
{
    public ApplicationSystemType SystemType => ApplicationSystemType.CustomsClearance;
    public string DisplayName => "التخليص الكمركي";
    public Type DashboardViewModelType => typeof(CustomsDashboardViewModel);
    public Type? SetupWizardViewModelType => null;

    public IReadOnlyList<(string Name, string Label)> PermissionScreens { get; } =
        CustomsPermissionRegistry.Screens;

    public IReadOnlyList<NavigationMenuItem> BuildMenuItems() =>
    [
        Item("لوحة التحكم", PackIconKind.ViewDashboard, typeof(CustomsDashboardViewModel), CustomsPermissionRegistry.Dashboard),
        Item("العملاء", PackIconKind.AccountGroup, typeof(CustomsCustomersViewModel), CustomsPermissionRegistry.Customers),
        Item("الفواتير", PackIconKind.FileDocumentOutline, typeof(CustomsInvoiceListViewModel), CustomsPermissionRegistry.Invoices),
        Item("وصولات القبض", PackIconKind.CashPlus, typeof(CustomsReceiptListViewModel), CustomsPermissionRegistry.Receipts),
        Item("كشف حساب", PackIconKind.FileDocumentOutline, typeof(CustomsStatementViewModel), CustomsPermissionRegistry.Statement),
        FlyoutGroup(
            key: "customs-reports",
            title: "التقارير",
            icon: PackIconKind.ChartBar,
            accent: "#0277BD",
            accentLight: "#E1F5FE",
            [
                ("التقرير اليومي", PackIconKind.CalendarToday, typeof(CustomsDailyReportViewModel), CustomsPermissionRegistry.Reports),
                ("التقرير الشهري", PackIconKind.CalendarMonth, typeof(CustomsMonthlyReportViewModel), CustomsPermissionRegistry.Reports),
                ("تقرير العملاء", PackIconKind.AccountGroup, typeof(CustomsCustomerReportViewModel), CustomsPermissionRegistry.Reports),
                ("تقرير الفواتير", PackIconKind.FileDocumentMultiple, typeof(CustomsInvoiceReportViewModel), CustomsPermissionRegistry.Reports),
                ("تقرير السيارات", PackIconKind.Car, typeof(CustomsVehiclesReportViewModel), CustomsPermissionRegistry.Reports),
                ("تقرير الديون", PackIconKind.CashClock, typeof(CustomsDebtsReportViewModel), CustomsPermissionRegistry.Reports),
                ("تقرير الربح", PackIconKind.ChartAreaspline, typeof(CustomsProfitReportViewModel), CustomsPermissionRegistry.ProfitReport),
            ]),
        Item("المستخدمون", PackIconKind.AccountMultiple, typeof(UsersViewModel), CustomsPermissionRegistry.Users),
        Item("الصلاحيات", PackIconKind.ShieldKey, typeof(PermissionsViewModel), CustomsPermissionRegistry.Permissions),
        Item("سجل العمليات", PackIconKind.History, typeof(AuditLogViewModel), CustomsPermissionRegistry.AuditLog),
        Item("إعدادات الطباعة", PackIconKind.PrinterSettings, typeof(PrintLayoutSettingsViewModel), CustomsPermissionRegistry.PrintSettings),
        Item("النسخ الاحتياطي", PackIconKind.DatabaseCog, typeof(BackupRestoreViewModel), CustomsPermissionRegistry.Backup),
        Item("ربط الحاسبات", PackIconKind.LanConnect, typeof(NetworkConnectionSettingsViewModel), ScreenPermissionRegistry.NetworkConnection),
        Item("المزامنة السحابية", PackIconKind.CloudSync, typeof(CloudSyncSettingsViewModel), CustomsPermissionRegistry.CloudSync),
        Item("تحديث النظام", PackIconKind.CloudDownload, typeof(SystemUpdateViewModel), ScreenPermissionRegistry.SystemUpdate),
        Item("تبديل النظام (مطور)", PackIconKind.DeveloperBoard, typeof(DeveloperSystemSwitchViewModel), ScreenPermissionRegistry.DeveloperSystem)
    ];

    public string GetScreenName(Type viewModelType) =>
        CustomsPermissionRegistry.GetScreenName(viewModelType);

    public string GetLabel(string screenName) =>
        CustomsPermissionRegistry.GetLabel(screenName);

    public Type? GetDefaultViewModelType(string screenName) =>
        CustomsPermissionRegistry.GetDefaultViewModelType(screenName);

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
            FlyoutItemLabel = "تقرير"
        };

        foreach (var child in children)
        {
            group.Children.Add(new NavigationMenuItem
            {
                Title = child.Title,
                Icon = child.Icon,
                ViewModelType = child.Vm,
                ScreenName = child.Screen,
                IsSubItem = true
            });
        }

        return group;
    }
}
