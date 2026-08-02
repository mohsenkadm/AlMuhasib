using AlMuhasib.UI.ViewModels;
using AlMuhasib.UI.ViewModels.Gold;

namespace AlMuhasib.UI.Services;

public static class GoldShopPermissionRegistry
{
    public const string Dashboard = "GoldDashboard";
    public const string MithqalPrices = "GoldMithqalPrices";
    public const string FxRates = "GoldFxRates";
    public const string Items = "GoldItems";
    public const string Stock = "GoldStock";
    public const string StockAdjustment = "GoldStockAdjustment";
    public const string SaleInvoice = "GoldSaleInvoice";
    public const string CreditSales = "GoldCreditSales";
    public const string Collection = "GoldCollection";
    public const string PurchaseInvoice = "GoldPurchaseInvoice";
    public const string Customers = "GoldCustomers";
    public const string CustomerStatement = "GoldCustomerStatement";
    public const string CashBoxes = "GoldCashBoxes";
    public const string Vouchers = "GoldVouchers";
    public const string Notifications = "GoldNotifications";
    public const string Settings = "GoldSettings";
    public const string StockReport = "GoldStockReport";
    public const string SalesReport = "GoldSalesReport";
    public const string CreditReport = "GoldCreditReport";
    public const string Users = "Users";
    public const string Permissions = "Permissions";
    public const string PrintSettings = "PrintSettings";
    public const string Backup = "Backup";
    public const string CloudSync = "CloudSync";

    public static IReadOnlyList<(string Name, string Label)> Screens { get; } =
    [
        (Dashboard, "لوحة التحكم"),
        (MithqalPrices, "أسعار المثقال"),
        (FxRates, "أسعار الصرف"),
        (Items, "أصناف الذهب"),
        (Stock, "المخزون"),
        (StockAdjustment, "تسوية مخزون"),
        (SaleInvoice, "فاتورة بيع"),
        (CreditSales, "مبيعات الآجل"),
        (Collection, "التحصيل"),
        (PurchaseInvoice, "فاتورة شراء"),
        (Customers, "الزبائن"),
        (CustomerStatement, "كشف حساب زبون"),
        (CashBoxes, "القاصات"),
        (Vouchers, "السندات"),
        (Notifications, "التنبيهات"),
        (Settings, "إعدادات الذهب"),
        (StockReport, "تقرير المخزون"),
        (SalesReport, "تقرير المبيعات"),
        (CreditReport, "تقرير الآجل"),
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
        [typeof(GoldDashboardViewModel)] = Dashboard,
        [typeof(GoldMithqalPricesViewModel)] = MithqalPrices,
        [typeof(GoldFxRatesViewModel)] = FxRates,
        [typeof(GoldItemsViewModel)] = Items,
        [typeof(GoldStockViewModel)] = Stock,
        [typeof(GoldStockAdjustmentViewModel)] = StockAdjustment,
        [typeof(GoldSaleInvoiceViewModel)] = SaleInvoice,
        [typeof(GoldCreditSalesViewModel)] = CreditSales,
        [typeof(GoldCollectionViewModel)] = Collection,
        [typeof(GoldPurchaseInvoiceViewModel)] = PurchaseInvoice,
        [typeof(GoldCustomersViewModel)] = Customers,
        [typeof(GoldCustomerStatementViewModel)] = CustomerStatement,
        [typeof(GoldCashBoxesViewModel)] = CashBoxes,
        [typeof(GoldVouchersViewModel)] = Vouchers,
        [typeof(GoldNotificationsViewModel)] = Notifications,
        [typeof(GoldSettingsViewModel)] = Settings,
        [typeof(GoldStockReportViewModel)] = StockReport,
        [typeof(GoldSalesReportViewModel)] = SalesReport,
        [typeof(GoldCreditReportViewModel)] = CreditReport,
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
        [Dashboard] = typeof(GoldDashboardViewModel),
        [MithqalPrices] = typeof(GoldMithqalPricesViewModel),
        [FxRates] = typeof(GoldFxRatesViewModel),
        [Items] = typeof(GoldItemsViewModel),
        [Stock] = typeof(GoldStockViewModel),
        [StockAdjustment] = typeof(GoldStockAdjustmentViewModel),
        [SaleInvoice] = typeof(GoldSaleInvoiceViewModel),
        [CreditSales] = typeof(GoldCreditSalesViewModel),
        [Collection] = typeof(GoldCollectionViewModel),
        [PurchaseInvoice] = typeof(GoldPurchaseInvoiceViewModel),
        [Customers] = typeof(GoldCustomersViewModel),
        [CustomerStatement] = typeof(GoldCustomerStatementViewModel),
        [CashBoxes] = typeof(GoldCashBoxesViewModel),
        [Vouchers] = typeof(GoldVouchersViewModel),
        [Notifications] = typeof(GoldNotificationsViewModel),
        [Settings] = typeof(GoldSettingsViewModel),
        [StockReport] = typeof(GoldStockReportViewModel),
        [SalesReport] = typeof(GoldSalesReportViewModel),
        [CreditReport] = typeof(GoldCreditReportViewModel),
        [Users] = typeof(UsersViewModel),
        [Permissions] = typeof(PermissionsViewModel),
        [PrintSettings] = typeof(PrintLayoutSettingsViewModel),
        [Backup] = typeof(BackupRestoreViewModel),
        [CloudSync] = typeof(CloudSyncSettingsViewModel),
        [ScreenPermissionRegistry.NetworkConnection] = typeof(NetworkConnectionSettingsViewModel),
        [ScreenPermissionRegistry.SystemUpdate] = typeof(SystemUpdateViewModel)
    };

    private static readonly Dictionary<string, string> Labels = Screens
        .ToDictionary(s => s.Name, s => s.Label, StringComparer.Ordinal);

    public static string GetScreenName(Type viewModelType) =>
        ViewModelToScreen.TryGetValue(viewModelType, out var name) ? name : viewModelType.Name;

    public static Type? GetDefaultViewModelType(string screenName) =>
        ScreenToDefaultViewModel.TryGetValue(screenName, out var type) ? type : null;

    public static string GetLabel(string screenName) =>
        Labels.TryGetValue(screenName, out var label) ? label : screenName;
}
