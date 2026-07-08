using AlMuhasib.Core.Enums;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using AlMuhasib.UI.ViewModels;
using AlMuhasib.UI.ViewModels.CarTrade;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Modules;

public sealed class CarTradingSystemModule : ISystemModule
{
    public ApplicationSystemType SystemType => ApplicationSystemType.CarTrading;
    public string DisplayName => "نظام بيع وشراء السيارات";
    public Type DashboardViewModelType => typeof(CarTradeDashboardViewModel);
    public Type? SetupWizardViewModelType => null;

    public IReadOnlyList<(string Name, string Label)> PermissionScreens { get; } =
    [
        (CarTradePermissionRegistry.Dashboard, "لوحة التحكم"),
        (CarTradePermissionRegistry.CarTradeForm, "شراء سيارة"),
        (CarTradePermissionRegistry.CarTradeList, "العمليات"),
        (CarTradePermissionRegistry.CarTradeReports, "التقارير"),
        (CarTradePermissionRegistry.CarTradePartyStatement, "كشف الحساب"),
        (CarTradePermissionRegistry.Users, "المستخدمون"),
        (CarTradePermissionRegistry.Permissions, "الصلاحيات"),
        (CarTradePermissionRegistry.PrintSettings, "إعدادات الطباعة"),
        (CarTradePermissionRegistry.Backup, "النسخ الاحتياطي"),
        (CarTradePermissionRegistry.CloudSync, "المزامنة السحابية"),
        (ScreenPermissionRegistry.SystemUpdate, "تحديث النظام")
    ];

    public IReadOnlyList<NavigationMenuItem> BuildMenuItems() =>
    [
        new NavigationMenuItem
        {
            Title = "لوحة التحكم",
            Icon = PackIconKind.ViewDashboard,
            ViewModelType = typeof(CarTradeDashboardViewModel),
            ScreenName = CarTradePermissionRegistry.Dashboard
        },
        new NavigationMenuItem
        {
            Title = "شراء سيارة",
            Icon = PackIconKind.CarArrowRight,
            ViewModelType = typeof(CarTradeFormViewModel),
            ScreenName = CarTradePermissionRegistry.CarTradeForm
        },
        new NavigationMenuItem
        {
            Title = "العمليات",
            Icon = PackIconKind.FormatListBulleted,
            ViewModelType = typeof(CarTradeListViewModel),
            ScreenName = CarTradePermissionRegistry.CarTradeList
        },
        new NavigationMenuItem
        {
            Title = "التقارير",
            Icon = PackIconKind.ChartBar,
            ViewModelType = typeof(CarTradeReportsViewModel),
            ScreenName = CarTradePermissionRegistry.CarTradeReports
        },
        new NavigationMenuItem
        {
            Title = "كشف الحساب",
            Icon = PackIconKind.AccountCash,
            ViewModelType = typeof(CarTradePartyStatementViewModel),
            ScreenName = CarTradePermissionRegistry.CarTradePartyStatement
        },
        new NavigationMenuItem
        {
            Title = "المستخدمون",
            Icon = PackIconKind.AccountMultiple,
            ViewModelType = typeof(UsersViewModel),
            ScreenName = CarTradePermissionRegistry.Users
        },
        new NavigationMenuItem
        {
            Title = "الصلاحيات",
            Icon = PackIconKind.ShieldKey,
            ViewModelType = typeof(PermissionsViewModel),
            ScreenName = CarTradePermissionRegistry.Permissions
        },
        new NavigationMenuItem
        {
            Title = "إعدادات الطباعة",
            Icon = PackIconKind.PrinterSettings,
            ViewModelType = typeof(PrintLayoutSettingsViewModel),
            ScreenName = CarTradePermissionRegistry.PrintSettings
        },
        new NavigationMenuItem
        {
            Title = "النسخ الاحتياطي",
            Icon = PackIconKind.DatabaseCog,
            ViewModelType = typeof(BackupRestoreViewModel),
            ScreenName = CarTradePermissionRegistry.Backup
        },
        new NavigationMenuItem
        {
            Title = "المزامنة السحابية",
            Icon = PackIconKind.CloudSync,
            ViewModelType = typeof(CloudSyncSettingsViewModel),
            ScreenName = CarTradePermissionRegistry.CloudSync
        },
        new NavigationMenuItem
        {
            Title = "تحديث النظام",
            Icon = PackIconKind.CloudDownload,
            ViewModelType = typeof(SystemUpdateViewModel),
            ScreenName = ScreenPermissionRegistry.SystemUpdate
        },
        new NavigationMenuItem
        {
            Title = "تبديل النظام (مطور)",
            Icon = PackIconKind.DeveloperBoard,
            ViewModelType = typeof(DeveloperSystemSwitchViewModel),
            ScreenName = ScreenPermissionRegistry.DeveloperSystem
        }
    ];

    public string GetScreenName(Type viewModelType) =>
        CarTradePermissionRegistry.GetScreenName(viewModelType);

    public string GetLabel(string screenName) =>
        CarTradePermissionRegistry.GetLabel(screenName);

    public Type? GetDefaultViewModelType(string screenName) =>
        CarTradePermissionRegistry.GetDefaultViewModelType(screenName);
}
