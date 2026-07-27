using AlMuhasib.UI.ViewModels;
using AlMuhasib.UI.ViewModels.RealEstate;

namespace AlMuhasib.UI.Services;

public static class RealEstatePermissionRegistry
{
    public const string Dashboard = "RealEstateDashboard";
    public const string ContractForm = "RealEstateContractForm";
    public const string Contracts = "RealEstateContracts";
    public const string Debts = "RealEstateDebts";
    public const string Parties = "RealEstateParties";
    public const string Reports = "RealEstateReports";
    public const string ClauseTemplates = "RealEstateClauseTemplates";
    public const string Users = "Users";
    public const string Permissions = "Permissions";
    public const string PrintSettings = "PrintSettings";
    public const string Backup = "Backup";
    public const string CloudSync = "CloudSync";

    private static readonly Dictionary<Type, string> ViewModelToScreen = new()
    {
        [typeof(RealEstateDashboardViewModel)] = Dashboard,
        [typeof(RealEstateContractFormViewModel)] = ContractForm,
        [typeof(RealEstateContractsViewModel)] = Contracts,
        [typeof(RealEstateDebtsViewModel)] = Debts,
        [typeof(RealEstatePartiesViewModel)] = Parties,
        [typeof(RealEstateContractsReportViewModel)] = Reports,
        [typeof(RealEstateClauseTemplatesViewModel)] = ClauseTemplates,
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
        [Dashboard] = typeof(RealEstateDashboardViewModel),
        [ContractForm] = typeof(RealEstateContractFormViewModel),
        [Contracts] = typeof(RealEstateContractsViewModel),
        [Debts] = typeof(RealEstateDebtsViewModel),
        [Parties] = typeof(RealEstatePartiesViewModel),
        [Reports] = typeof(RealEstateContractsReportViewModel),
        [ClauseTemplates] = typeof(RealEstateClauseTemplatesViewModel),
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
        [ContractForm] = "عقد جديد",
        [Contracts] = "العقود",
        [Debts] = "كشف المدينين",
        [Parties] = "الزبائن",
        [Reports] = "التقارير",
        [ClauseTemplates] = "بنود العقد",
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
