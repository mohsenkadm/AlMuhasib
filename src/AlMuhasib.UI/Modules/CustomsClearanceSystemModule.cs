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
        new NavigationMenuItem
        {
            Title = "لوحة التحكم",
            Icon = PackIconKind.ViewDashboard,
            ViewModelType = typeof(CustomsDashboardViewModel),
            ScreenName = CustomsPermissionRegistry.Dashboard
        },
        new NavigationMenuItem
        {
            Title = "العملاء",
            Icon = PackIconKind.AccountGroup,
            ViewModelType = typeof(CustomsCustomersViewModel),
            ScreenName = CustomsPermissionRegistry.Customers
        },
        new NavigationMenuItem
        {
            Title = "المستخدمون",
            Icon = PackIconKind.AccountMultiple,
            ViewModelType = typeof(UsersViewModel),
            ScreenName = CustomsPermissionRegistry.Users
        },
        new NavigationMenuItem
        {
            Title = "الصلاحيات",
            Icon = PackIconKind.ShieldKey,
            ViewModelType = typeof(PermissionsViewModel),
            ScreenName = CustomsPermissionRegistry.Permissions
        },
        new NavigationMenuItem
        {
            Title = "إعدادات الطباعة",
            Icon = PackIconKind.PrinterSettings,
            ViewModelType = typeof(PrintLayoutSettingsViewModel),
            ScreenName = CustomsPermissionRegistry.PrintSettings
        },
        new NavigationMenuItem
        {
            Title = "النسخ الاحتياطي",
            Icon = PackIconKind.DatabaseCog,
            ViewModelType = typeof(BackupRestoreViewModel),
            ScreenName = CustomsPermissionRegistry.Backup
        },
        new NavigationMenuItem
        {
            Title = "ربط الحاسبات",
            Icon = PackIconKind.LanConnect,
            ViewModelType = typeof(NetworkConnectionSettingsViewModel),
            ScreenName = ScreenPermissionRegistry.NetworkConnection
        },
        new NavigationMenuItem
        {
            Title = "المزامنة السحابية",
            Icon = PackIconKind.CloudSync,
            ViewModelType = typeof(CloudSyncSettingsViewModel),
            ScreenName = CustomsPermissionRegistry.CloudSync
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
        CustomsPermissionRegistry.GetScreenName(viewModelType);

    public string GetLabel(string screenName) =>
        CustomsPermissionRegistry.GetLabel(screenName);

    public Type? GetDefaultViewModelType(string screenName) =>
        CustomsPermissionRegistry.GetDefaultViewModelType(screenName);
}
