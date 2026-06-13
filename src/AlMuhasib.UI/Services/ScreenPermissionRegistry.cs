using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Services;

/// <summary>
/// Single source of truth for screen permission names and ViewModel mapping.
/// </summary>
public static class ScreenPermissionRegistry
{
    public const string Dashboard = "Dashboard";
    public const string Reports = "Reports";
    public const string BalanceSheet = "BalanceSheet";

    public static IReadOnlyList<(string Name, string Label)> AllScreens { get; } =
    [
        (Dashboard, "لوحة التحكم"),
        ("Products", "المنتجات"),
        ("Categories", "تصنيفات المنتجات"),
        ("Customers", "العملاء"),
        ("Suppliers", "الموردون"),
        ("PurchaseInvoice", "فاتورة مشتريات"),
        ("SaleInvoice", "فاتورة مبيعات"),
        ("InstallmentInvoice", "فاتورة أقساط"),
        ("Installments", "الأقساط ولوحة التحصيل"),
        ("OpeningInstallments", "أرصدة الأقساط الافتتاحية"),
        ("Vouchers", "السندات"),
        ("Expenses", "المصاريف"),
        ("CashAndBank", "القاصات والمصرف"),
        ("Investors", "المستثمرون"),
        ("OpeningInvestors", "أرصدة المستثمرين الافتتاحية"),
        ("Warehouses", "المخازن ونقل المخازن"),
        ("OpeningStock", "الأرصدة الافتتاحية"),
        ("StockAdjustment", "تسوية مخزنية"),
        ("Reports", "التقارير"),
        ("BalanceSheet", "موازنة يومية"),
        ("Capital", "رأس المال"),
        ("AuditLog", "سجل العمليات"),
        ("Users", "المستخدمون"),
        ("Permissions", "الصلاحيات"),
        ("DataImport", "معالج النقل / استيراد البيانات"),
        ("BusinessFeatures", "إعدادات الميزات"),
        ("PrintSettings", "إعدادات الطباعة"),
        ("Backup", "النسخ الاحتياطي"),
        ("CloudSync", "المزامنة السحابية"),
    ];

    private static readonly Dictionary<Type, string> ViewModelToScreen = new()
    {
        [typeof(DashboardViewModel)] = Dashboard,
        [typeof(ProductsViewModel)] = "Products",
        [typeof(CategoriesViewModel)] = "Categories",
        [typeof(CustomersViewModel)] = "Customers",
        [typeof(SuppliersViewModel)] = "Suppliers",
        [typeof(PurchaseInvoiceViewModel)] = "PurchaseInvoice",
        [typeof(SalesInvoiceViewModel)] = "SaleInvoice",
        [typeof(PosQuickSaleViewModel)] = "SaleInvoice",
        [typeof(InstallmentInvoiceViewModel)] = "InstallmentInvoice",
        [typeof(CollectionDashboardViewModel)] = "Installments",
        [typeof(InstallmentsViewModel)] = "Installments",
        [typeof(OpeningInstallmentBalanceViewModel)] = "OpeningInstallments",
        [typeof(VouchersViewModel)] = "Vouchers",
        [typeof(ExpenseViewModel)] = "Expenses",
        [typeof(CashBankViewModel)] = "CashAndBank",
        [typeof(InvestorsViewModel)] = "Investors",
        [typeof(OpeningInvestorsViewModel)] = "OpeningInvestors",
        [typeof(WarehousesViewModel)] = "Warehouses",
        [typeof(WarehouseTransferViewModel)] = "Warehouses",
        [typeof(OpeningStockViewModel)] = "OpeningStock",
        [typeof(StockAdjustmentViewModel)] = "StockAdjustment",
        [typeof(SalesReportViewModel)] = "Reports",
        [typeof(PurchasesReportViewModel)] = "Reports",
        [typeof(ProfitReportViewModel)] = "Reports",
        [typeof(TopProductsReportViewModel)] = "Reports",
        [typeof(ProductProfitMarginReportViewModel)] = "Reports",
        [typeof(InstallmentAgingReportViewModel)] = "Reports",
        [typeof(InstallmentsReportViewModel)] = "Reports",
        [typeof(InstallmentDetailReportViewModel)] = "Reports",
        [typeof(PaidInstallmentsReportViewModel)] = "Reports",
        [typeof(UnpaidInstallmentsReportViewModel)] = "Reports",
        [typeof(OverdueReportViewModel)] = "Reports",
        [typeof(CustomersOverviewReportViewModel)] = "Reports",
        [typeof(SuppliersOverviewReportViewModel)] = "Reports",
        [typeof(ProfitComparisonReportViewModel)] = "Reports",
        [typeof(ProductMovementReportViewModel)] = "Reports",
        [typeof(CustomerStatementViewModel)] = "Reports",
        [typeof(SupplierStatementViewModel)] = "Reports",
        [typeof(ExpensesReportViewModel)] = "Reports",
        [typeof(IncomeExpenseReportViewModel)] = "Reports",
        [typeof(WarehouseReportViewModel)] = "Reports",
        [typeof(StockHealthReportViewModel)] = "Reports",
        [typeof(InvestorsReportViewModel)] = "Reports",
        [typeof(CashFlowReportViewModel)] = "Reports",
        [typeof(BalanceSheetViewModel)] = "BalanceSheet",
        [typeof(CapitalAdjustmentViewModel)] = "Capital",
        [typeof(AuditLogViewModel)] = "AuditLog",
        [typeof(UsersViewModel)] = "Users",
        [typeof(PermissionsViewModel)] = "Permissions",
        [typeof(MigrationWizardViewModel)] = "DataImport",
        [typeof(BusinessFeaturesSettingsViewModel)] = "BusinessFeatures",
        [typeof(PrintLayoutSettingsViewModel)] = "PrintSettings",
        [typeof(BackupRestoreViewModel)] = "Backup",
        [typeof(CloudSyncSettingsViewModel)] = "CloudSync",
    };

    private static readonly Dictionary<string, Type> ScreenToDefaultViewModel = new()
    {
        [Dashboard] = typeof(DashboardViewModel),
        ["Products"] = typeof(ProductsViewModel),
        ["Categories"] = typeof(CategoriesViewModel),
        ["Customers"] = typeof(CustomersViewModel),
        ["Suppliers"] = typeof(SuppliersViewModel),
        ["PurchaseInvoice"] = typeof(PurchaseInvoiceViewModel),
        ["SaleInvoice"] = typeof(SalesInvoiceViewModel),
        ["InstallmentInvoice"] = typeof(InstallmentInvoiceViewModel),
        ["Installments"] = typeof(InstallmentsViewModel),
        ["OpeningInstallments"] = typeof(OpeningInstallmentBalanceViewModel),
        ["Vouchers"] = typeof(VouchersViewModel),
        ["Expenses"] = typeof(ExpenseViewModel),
        ["CashAndBank"] = typeof(CashBankViewModel),
        ["Investors"] = typeof(InvestorsViewModel),
        ["OpeningInvestors"] = typeof(OpeningInvestorsViewModel),
        ["Warehouses"] = typeof(WarehousesViewModel),
        ["OpeningStock"] = typeof(OpeningStockViewModel),
        ["StockAdjustment"] = typeof(StockAdjustmentViewModel),
        ["Reports"] = typeof(SalesReportViewModel),
        ["BalanceSheet"] = typeof(BalanceSheetViewModel),
        ["Capital"] = typeof(CapitalAdjustmentViewModel),
        ["AuditLog"] = typeof(AuditLogViewModel),
        ["Users"] = typeof(UsersViewModel),
        ["Permissions"] = typeof(PermissionsViewModel),
        ["DataImport"] = typeof(MigrationWizardViewModel),
        ["BusinessFeatures"] = typeof(BusinessFeaturesSettingsViewModel),
        ["PrintSettings"] = typeof(PrintLayoutSettingsViewModel),
        ["Backup"] = typeof(BackupRestoreViewModel),
        ["CloudSync"] = typeof(CloudSyncSettingsViewModel),
    };

    public static string GetScreenName(Type viewModelType) =>
        ViewModelToScreen.TryGetValue(viewModelType, out var name) ? name : viewModelType.Name;

    public static Type? GetDefaultViewModelType(string screenName) =>
        ScreenToDefaultViewModel.TryGetValue(screenName, out var type) ? type : null;

    public static string GetLabel(string screenName) =>
        AllScreens.FirstOrDefault(s => s.Name == screenName).Label ?? screenName;
}
