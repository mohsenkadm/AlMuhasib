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
    public const string Warehouses = "GoldWarehouses";
    public const string WarehouseTransfer = "GoldWarehouseTransfer";
    public const string SaleInvoice = "GoldSaleInvoice";
    public const string SaleReturn = "GoldSaleReturn";
    public const string CreditSales = "GoldCreditSales";
    public const string Collection = "GoldCollection";
    public const string ExchangeInvoice = "GoldExchangeInvoice";
    public const string PurchaseInvoice = "GoldPurchaseInvoice";
    public const string Suppliers = "GoldSuppliers";
    public const string Customers = "GoldCustomers";
    public const string CustomerStatement = "GoldCustomerStatement";
    public const string SupplierStatement = "GoldSupplierStatement";
    public const string SupplierPayment = "GoldSupplierPayment";
    public const string OpeningStock = "GoldOpeningStock";
    public const string OpeningCustomerBalance = "GoldOpeningCustomerBalance";
    public const string OpeningSupplierBalance = "GoldOpeningSupplierBalance";
    public const string CashBoxes = "GoldCashBoxes";
    public const string Vouchers = "GoldVouchers";
    public const string Expenses = "GoldExpenses";
    public const string ExpenseTypes = "GoldExpenseTypes";
    public const string Categories = "GoldCategories";
    public const string Notifications = "GoldNotifications";
    public const string Settings = "GoldSettings";
    public const string StockReport = "GoldStockReport";
    public const string SalesReport = "GoldSalesReport";
    public const string CreditReport = "GoldCreditReport";
    public const string AgingReport = "GoldAgingReport";
    public const string KaratMovementReport = "GoldKaratMovementReport";
    public const string ProfitabilityReport = "GoldProfitabilityReport";
    public const string AuditReport = "GoldAuditReport";
    public const string PurchasesReport = "GoldPurchasesReport";
    public const string CashBoxMovementReport = "GoldCashBoxMovementReport";
    public const string UserPerformanceReport = "GoldUserPerformanceReport";
    public const string DeletedInvoicesReport = "GoldDeletedInvoicesReport";
    public const string ExchangeReport = "GoldExchangeReport";
    public const string SaleReturnsReport = "GoldSaleReturnsReport";
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
        (Warehouses, "المخازن"),
        (WarehouseTransfer, "نقل مخازن"),
        (SaleInvoice, "فاتورة بيع"),
        (SaleReturn, "مرتجع بيع"),
        (CreditSales, "مبيعات الآجل"),
        (Collection, "التحصيل"),
        (ExchangeInvoice, "تبديل ذهب"),
        (PurchaseInvoice, "فاتورة شراء"),
        (Suppliers, "الموردون"),
        (SupplierStatement, "كشف حساب مورد"),
        (SupplierPayment, "تسديد الموردين"),
        (Customers, "الزبائن"),
        (CustomerStatement, "كشف حساب زبون"),
        (OpeningStock, "رصيد افتتاحي مخزون"),
        (OpeningCustomerBalance, "أرصدة الزبائن الافتتاحية"),
        (OpeningSupplierBalance, "أرصدة الموردين الافتتاحية"),
        (CashBoxes, "القاصات"),
        (Vouchers, "السندات"),
        (Expenses, "المصاريف"),
        (ExpenseTypes, "أنواع المصاريف"),
        (Categories, "تصنيفات الذهب"),
        (Notifications, "التنبيهات"),
        (Settings, "إعدادات الذهب"),
        (StockReport, "تقرير المخزون"),
        (SalesReport, "تقرير المبيعات"),
        (CreditReport, "تقرير الآجل"),
        (AgingReport, "أعمار ذمم الذهب"),
        (KaratMovementReport, "حركة العيارات"),
        (ProfitabilityReport, "ربحية الذهب"),
        (AuditReport, "سجل تدقيق الذهب"),
        (PurchasesReport, "تقرير المشتريات"),
        (CashBoxMovementReport, "حركة القاصات"),
        (UserPerformanceReport, "أداء المستخدمين"),
        (DeletedInvoicesReport, "الفواتير المحذوفة"),
        (ExchangeReport, "تقرير التبديل"),
        (SaleReturnsReport, "تقرير مرتجعات البيع"),
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
        [typeof(GoldWarehousesViewModel)] = Warehouses,
        [typeof(GoldWarehouseTransferViewModel)] = WarehouseTransfer,
        [typeof(GoldSaleInvoiceViewModel)] = SaleInvoice,
        [typeof(GoldSaleReturnViewModel)] = SaleReturn,
        [typeof(GoldCreditSalesViewModel)] = CreditSales,
        [typeof(GoldCollectionViewModel)] = Collection,
        [typeof(GoldExchangeInvoiceViewModel)] = ExchangeInvoice,
        [typeof(GoldPurchaseInvoiceViewModel)] = PurchaseInvoice,
        [typeof(GoldSuppliersViewModel)] = Suppliers,
        [typeof(GoldSupplierStatementViewModel)] = SupplierStatement,
        [typeof(GoldSupplierPaymentViewModel)] = SupplierPayment,
        [typeof(GoldCustomersViewModel)] = Customers,
        [typeof(GoldCustomerStatementViewModel)] = CustomerStatement,
        [typeof(GoldOpeningStockViewModel)] = OpeningStock,
        [typeof(GoldOpeningCustomerBalanceViewModel)] = OpeningCustomerBalance,
        [typeof(GoldOpeningSupplierBalanceViewModel)] = OpeningSupplierBalance,
        [typeof(GoldCashBoxesViewModel)] = CashBoxes,
        [typeof(GoldVouchersViewModel)] = Vouchers,
        [typeof(GoldExpensesViewModel)] = Expenses,
        [typeof(GoldExpenseTypesViewModel)] = ExpenseTypes,
        [typeof(GoldCategoriesViewModel)] = Categories,
        [typeof(GoldNotificationsViewModel)] = Notifications,
        [typeof(GoldSettingsViewModel)] = Settings,
        [typeof(GoldStockReportViewModel)] = StockReport,
        [typeof(GoldSalesReportViewModel)] = SalesReport,
        [typeof(GoldCreditReportViewModel)] = CreditReport,
        [typeof(GoldAgingReportViewModel)] = AgingReport,
        [typeof(GoldKaratMovementReportViewModel)] = KaratMovementReport,
        [typeof(GoldProfitabilityReportViewModel)] = ProfitabilityReport,
        [typeof(GoldAuditReportViewModel)] = AuditReport,
        [typeof(GoldPurchasesReportViewModel)] = PurchasesReport,
        [typeof(GoldCashBoxMovementReportViewModel)] = CashBoxMovementReport,
        [typeof(GoldUserPerformanceReportViewModel)] = UserPerformanceReport,
        [typeof(GoldDeletedInvoicesReportViewModel)] = DeletedInvoicesReport,
        [typeof(GoldExchangeReportViewModel)] = ExchangeReport,
        [typeof(GoldSaleReturnsReportViewModel)] = SaleReturnsReport,
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
        [Warehouses] = typeof(GoldWarehousesViewModel),
        [WarehouseTransfer] = typeof(GoldWarehouseTransferViewModel),
        [SaleInvoice] = typeof(GoldSaleInvoiceViewModel),
        [SaleReturn] = typeof(GoldSaleReturnViewModel),
        [CreditSales] = typeof(GoldCreditSalesViewModel),
        [Collection] = typeof(GoldCollectionViewModel),
        [ExchangeInvoice] = typeof(GoldExchangeInvoiceViewModel),
        [PurchaseInvoice] = typeof(GoldPurchaseInvoiceViewModel),
        [Suppliers] = typeof(GoldSuppliersViewModel),
        [SupplierStatement] = typeof(GoldSupplierStatementViewModel),
        [SupplierPayment] = typeof(GoldSupplierPaymentViewModel),
        [Customers] = typeof(GoldCustomersViewModel),
        [CustomerStatement] = typeof(GoldCustomerStatementViewModel),
        [OpeningStock] = typeof(GoldOpeningStockViewModel),
        [OpeningCustomerBalance] = typeof(GoldOpeningCustomerBalanceViewModel),
        [OpeningSupplierBalance] = typeof(GoldOpeningSupplierBalanceViewModel),
        [CashBoxes] = typeof(GoldCashBoxesViewModel),
        [Vouchers] = typeof(GoldVouchersViewModel),
        [Expenses] = typeof(GoldExpensesViewModel),
        [ExpenseTypes] = typeof(GoldExpenseTypesViewModel),
        [Categories] = typeof(GoldCategoriesViewModel),
        [Notifications] = typeof(GoldNotificationsViewModel),
        [Settings] = typeof(GoldSettingsViewModel),
        [StockReport] = typeof(GoldStockReportViewModel),
        [SalesReport] = typeof(GoldSalesReportViewModel),
        [CreditReport] = typeof(GoldCreditReportViewModel),
        [AgingReport] = typeof(GoldAgingReportViewModel),
        [KaratMovementReport] = typeof(GoldKaratMovementReportViewModel),
        [ProfitabilityReport] = typeof(GoldProfitabilityReportViewModel),
        [AuditReport] = typeof(GoldAuditReportViewModel),
        [PurchasesReport] = typeof(GoldPurchasesReportViewModel),
        [CashBoxMovementReport] = typeof(GoldCashBoxMovementReportViewModel),
        [UserPerformanceReport] = typeof(GoldUserPerformanceReportViewModel),
        [DeletedInvoicesReport] = typeof(GoldDeletedInvoicesReportViewModel),
        [ExchangeReport] = typeof(GoldExchangeReportViewModel),
        [SaleReturnsReport] = typeof(GoldSaleReturnsReportViewModel),
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
