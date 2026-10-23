using AlMuhasib.UI.ViewModels;
using AlMuhasib.UI.ViewModels.Customs;

namespace AlMuhasib.UI.Services;

public static class CustomsPermissionRegistry
{
    public const string Dashboard = "CustomsDashboard";
    public const string Customers = "CustomsCustomers";
    public const string Invoices = "CustomsInvoices";
    public const string Receipts = "CustomsReceipts";
    public const string Users = "Users";
    public const string Permissions = "Permissions";
    public const string PrintSettings = "PrintSettings";
    public const string Backup = "Backup";
    public const string CloudSync = "CloudSync";

    public static IReadOnlyList<(string Name, string Label)> Screens { get; } =
    [
        (Dashboard, "لوحة التحكم"),
        (Customers, "العملاء"),
        (Invoices, "الفواتير"),
        (Receipts, "وصولات القبض"),
        (Users, "المستخدمون"),
        (Permissions, "الصلاحيات"),
        (PrintSettings, "إعدادات الطباعة"),
        (Backup, "النسخ الاحتياطي"),
        (CloudSync, "المزامنة السحابية"),
        (ScreenPermissionRegistry.NetworkConnection, "ربط الحاسبات"),
        (ScreenPermissionRegistry.SystemUpdate, "تحديث النظام")
    ];

    private static readonly Dictionary<Type, string> ViewModelToScreen = new()
    {
        [typeof(CustomsDashboardViewModel)] = Dashboard,
        [typeof(CustomsCustomersViewModel)] = Customers,
        [typeof(CustomsCustomerFileViewModel)] = Customers,
        [typeof(CustomsInvoiceListViewModel)] = Invoices,
        [typeof(CustomsInvoiceFormViewModel)] = Invoices,
        [typeof(CustomsReceiptListViewModel)] = Receipts,
        [typeof(CustomsReceiptFormViewModel)] = Receipts,
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
        [Dashboard] = typeof(CustomsDashboardViewModel),
        [Customers] = typeof(CustomsCustomersViewModel),
        [Invoices] = typeof(CustomsInvoiceListViewModel),
        [Receipts] = typeof(CustomsReceiptListViewModel),
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
        [Customers] = "العملاء",
        [Invoices] = "الفواتير",
        [Receipts] = "وصولات القبض",
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
