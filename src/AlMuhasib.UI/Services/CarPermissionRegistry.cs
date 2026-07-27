using AlMuhasib.UI.ViewModels;
using AlMuhasib.UI.ViewModels.Car;

namespace AlMuhasib.UI.Services;

public static class CarPermissionRegistry
{
    public const string Dashboard = "CarDashboard";
    public const string CarContractForm = "CarContractForm";
    public const string CarContracts = "CarContracts";
    public const string CarContractReports = "CarContractReports";
    public const string Users = "Users";
    public const string Permissions = "Permissions";
    public const string PrintSettings = "PrintSettings";
    public const string Backup = "Backup";
    public const string CloudSync = "CloudSync";

    private static readonly Dictionary<Type, string> ViewModelToScreen = new()
    {
        [typeof(CarDashboardViewModel)] = Dashboard,
        [typeof(CarContractFormViewModel)] = CarContractForm,
        [typeof(CarContractsViewModel)] = CarContracts,
        [typeof(CarContractsReportViewModel)] = CarContractReports,
        [typeof(UsersViewModel)] = Users,
        [typeof(PermissionsViewModel)] = Permissions,
        [typeof(PrintLayoutSettingsViewModel)] = PrintSettings,
        [typeof(BackupRestoreViewModel)] = Backup,
        [typeof(CloudSyncSettingsViewModel)] = CloudSync,
        [typeof(NetworkConnectionSettingsViewModel)] = ScreenPermissionRegistry.NetworkConnection,
        [typeof(SystemUpdateViewModel)] = ScreenPermissionRegistry.SystemUpdate
    };

    private static readonly Dictionary<string, Type> ScreenToDefaultViewModel = new()
    {
        [Dashboard] = typeof(CarDashboardViewModel),
        [CarContractForm] = typeof(CarContractFormViewModel),
        [CarContracts] = typeof(CarContractsViewModel),
        [CarContractReports] = typeof(CarContractsReportViewModel),
        [Users] = typeof(UsersViewModel),
        [Permissions] = typeof(PermissionsViewModel),
        [PrintSettings] = typeof(PrintLayoutSettingsViewModel),
        [Backup] = typeof(BackupRestoreViewModel),
        [CloudSync] = typeof(CloudSyncSettingsViewModel),
        [ScreenPermissionRegistry.NetworkConnection] = typeof(NetworkConnectionSettingsViewModel),
        [ScreenPermissionRegistry.SystemUpdate] = typeof(SystemUpdateViewModel)
    };

    private static readonly Dictionary<string, string> Labels = new()
    {
        [Dashboard] = "لوحة التحكم",
        [CarContractForm] = "عقد جديد",
        [CarContracts] = "العقود",
        [CarContractReports] = "تقرير العقود",
        [Users] = "المستخدمون",
        [Permissions] = "الصلاحيات",
        [PrintSettings] = "إعدادات الطباعة",
        [Backup] = "النسخ الاحتياطي",
        [CloudSync] = "المزامنة السحابية",
        [ScreenPermissionRegistry.NetworkConnection] = "ربط الحاسبات",
        [ScreenPermissionRegistry.SystemUpdate] = "تحديث النظام"
    };

    public static string GetScreenName(Type viewModelType) =>
        ViewModelToScreen.TryGetValue(viewModelType, out var name) ? name : viewModelType.Name;

    public static Type? GetDefaultViewModelType(string screenName) =>
        ScreenToDefaultViewModel.TryGetValue(screenName, out var type) ? type : null;

    public static string GetLabel(string screenName) =>
        Labels.TryGetValue(screenName, out var label) ? label : screenName;
}
