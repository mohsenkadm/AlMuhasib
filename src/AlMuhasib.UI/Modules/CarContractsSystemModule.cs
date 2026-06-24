using AlMuhasib.Core.Enums;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using AlMuhasib.UI.ViewModels;
using AlMuhasib.UI.ViewModels.Car;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Modules;

public sealed class CarContractsSystemModule : ISystemModule
{
    public ApplicationSystemType SystemType => ApplicationSystemType.CarContracts;
    public string DisplayName => "نظام عقود السيارات";
    public Type DashboardViewModelType => typeof(CarDashboardViewModel);
    public Type? SetupWizardViewModelType => null;

    public IReadOnlyList<(string Name, string Label)> PermissionScreens { get; } =
    [
        (CarPermissionRegistry.Dashboard, "لوحة التحكم"),
        (CarPermissionRegistry.CarContractForm, "عقد جديد"),
        (CarPermissionRegistry.CarContracts, "العقود"),
        (CarPermissionRegistry.CarContractReports, "تقرير العقود"),
        (CarPermissionRegistry.Users, "المستخدمون"),
        (CarPermissionRegistry.Permissions, "الصلاحيات"),
        (CarPermissionRegistry.PrintSettings, "إعدادات الطباعة"),
        (CarPermissionRegistry.Backup, "النسخ الاحتياطي"),
        (ScreenPermissionRegistry.SystemUpdate, "تحديث النظام")
    ];

    public IReadOnlyList<NavigationMenuItem> BuildMenuItems() =>
    [
        new NavigationMenuItem
        {
            Title = "لوحة التحكم",
            Icon = PackIconKind.ViewDashboard,
            ViewModelType = typeof(CarDashboardViewModel),
            ScreenName = CarPermissionRegistry.Dashboard
        },
        new NavigationMenuItem
        {
            Title = "عقد جديد",
            Icon = PackIconKind.FileDocumentPlus,
            ViewModelType = typeof(CarContractFormViewModel),
            ScreenName = CarPermissionRegistry.CarContractForm
        },
        new NavigationMenuItem
        {
            Title = "العقود",
            Icon = PackIconKind.FormatListBulleted,
            ViewModelType = typeof(CarContractsViewModel),
            ScreenName = CarPermissionRegistry.CarContracts
        },
        new NavigationMenuItem
        {
            Title = "تقرير العقود",
            Icon = PackIconKind.ChartBar,
            ViewModelType = typeof(CarContractsReportViewModel),
            ScreenName = CarPermissionRegistry.CarContractReports
        },
        new NavigationMenuItem
        {
            Title = "المستخدمون",
            Icon = PackIconKind.AccountMultiple,
            ViewModelType = typeof(UsersViewModel),
            ScreenName = CarPermissionRegistry.Users
        },
        new NavigationMenuItem
        {
            Title = "الصلاحيات",
            Icon = PackIconKind.ShieldKey,
            ViewModelType = typeof(PermissionsViewModel),
            ScreenName = CarPermissionRegistry.Permissions
        },
        new NavigationMenuItem
        {
            Title = "إعدادات الطباعة",
            Icon = PackIconKind.PrinterSettings,
            ViewModelType = typeof(PrintLayoutSettingsViewModel),
            ScreenName = CarPermissionRegistry.PrintSettings
        },
        new NavigationMenuItem
        {
            Title = "النسخ الاحتياطي",
            Icon = PackIconKind.DatabaseCog,
            ViewModelType = typeof(BackupRestoreViewModel),
            ScreenName = CarPermissionRegistry.Backup
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
        CarPermissionRegistry.GetScreenName(viewModelType);

    public string GetLabel(string screenName) =>
        CarPermissionRegistry.GetLabel(screenName);

    public Type? GetDefaultViewModelType(string screenName) =>
        CarPermissionRegistry.GetDefaultViewModelType(screenName);
}
