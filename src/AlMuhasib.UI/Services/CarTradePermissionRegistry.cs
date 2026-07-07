using AlMuhasib.UI.ViewModels;
using AlMuhasib.UI.ViewModels.CarTrade;

namespace AlMuhasib.UI.Services;

public static class CarTradePermissionRegistry
{
    public const string Dashboard = "CarTradeDashboard";
    public const string CarTradeForm = "CarTradeForm";
    public const string CarTradeList = "CarTradeList";
    public const string CarTradeReports = "CarTradeReports";
    public const string CarTradePartyStatement = "CarTradePartyStatement";
    public const string Users = "Users";
    public const string Permissions = "Permissions";
    public const string PrintSettings = "PrintSettings";
    public const string Backup = "Backup";
    public const string CloudSync = "CloudSync";

    private static readonly Dictionary<Type, string> ViewModelToScreen = new()
    {
        [typeof(CarTradeDashboardViewModel)] = Dashboard,
        [typeof(CarTradeFormViewModel)] = CarTradeForm,
        [typeof(CarTradeListViewModel)] = CarTradeList,
        [typeof(CarTradeReportsViewModel)] = CarTradeReports,
        [typeof(CarTradePartyStatementViewModel)] = CarTradePartyStatement,
        [typeof(UsersViewModel)] = Users,
        [typeof(PermissionsViewModel)] = Permissions,
        [typeof(PrintLayoutSettingsViewModel)] = PrintSettings,
        [typeof(BackupRestoreViewModel)] = Backup,
        [typeof(CloudSyncSettingsViewModel)] = CloudSync,
        [typeof(SystemUpdateViewModel)] = ScreenPermissionRegistry.SystemUpdate
    };

    private static readonly Dictionary<string, Type> ScreenToDefaultViewModel = new()
    {
        [Dashboard] = typeof(CarTradeDashboardViewModel),
        [CarTradeForm] = typeof(CarTradeFormViewModel),
        [CarTradeList] = typeof(CarTradeListViewModel),
        [CarTradeReports] = typeof(CarTradeReportsViewModel),
        [CarTradePartyStatement] = typeof(CarTradePartyStatementViewModel),
        [Users] = typeof(UsersViewModel),
        [Permissions] = typeof(PermissionsViewModel),
        [PrintSettings] = typeof(PrintLayoutSettingsViewModel),
        [Backup] = typeof(BackupRestoreViewModel),
        [CloudSync] = typeof(CloudSyncSettingsViewModel),
        [ScreenPermissionRegistry.SystemUpdate] = typeof(SystemUpdateViewModel)
    };

    private static readonly Dictionary<string, string> Labels = new()
    {
        [Dashboard] = "لوحة التحكم",
        [CarTradeForm] = "عملية جديدة",
        [CarTradeList] = "العمليات",
        [CarTradeReports] = "التقارير",
        [CarTradePartyStatement] = "كشف حساب طرف",
        [Users] = "المستخدمون",
        [Permissions] = "الصلاحيات",
        [PrintSettings] = "إعدادات الطباعة",
        [Backup] = "النسخ الاحتياطي",
        [CloudSync] = "المزامنة السحابية",
        [ScreenPermissionRegistry.SystemUpdate] = "تحديث النظام"
    };

    public static string GetScreenName(Type viewModelType) =>
        ViewModelToScreen.TryGetValue(viewModelType, out var name) ? name : viewModelType.Name;

    public static Type? GetDefaultViewModelType(string screenName) =>
        ScreenToDefaultViewModel.TryGetValue(screenName, out var type) ? type : null;

    public static string GetLabel(string screenName) =>
        Labels.TryGetValue(screenName, out var label) ? label : screenName;
}
