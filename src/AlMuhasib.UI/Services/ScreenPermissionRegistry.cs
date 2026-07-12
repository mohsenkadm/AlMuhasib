using AlMuhasib.UI.Modules;
using AlMuhasib.UI.ViewModels;
using AlMuhasib.UI.ViewModels.Car;

namespace AlMuhasib.UI.Services;

/// <summary>
/// Single source of truth for screen permission names and ViewModel mapping.
/// </summary>
public static class ScreenPermissionRegistry
{
    public const string Dashboard = "Dashboard";
    public const string DeveloperSystem = "DeveloperSystem";
    public const string SystemUpdate = "SystemUpdate";
    public const string Reports = "Reports";
    public const string BalanceSheet = "BalanceSheet";
    public const string NetworkConnection = "NetworkConnection";

    private static SystemModuleRegistry? _registry;

    public static void Initialize(SystemModuleRegistry registry) => _registry = registry;

    public static bool IsCarContracts => _registry?.IsCarContracts == true;

    public static bool IsCarTrading => _registry?.IsCarTrading == true;

    public static bool IsHotelManagement => _registry?.IsHotelManagement == true;

    public static IReadOnlyList<(string Name, string Label)> AllScreens =>
        IsHotelManagement
            ? HotelPermissionRegistryScreens
            : IsCarTrading
                ? CarTradePermissionRegistryScreens
            : IsCarContracts
                ? CarPermissionRegistryScreens
                : AccountingScreens;

    private static IReadOnlyList<(string Name, string Label)> CarPermissionRegistryScreens { get; } =
    [
        (CarPermissionRegistry.Dashboard, "لوحة التحكم"),
        (CarPermissionRegistry.CarContractForm, "عقد جديد"),
        (CarPermissionRegistry.CarContracts, "العقود"),
        (CarPermissionRegistry.CarContractReports, "تقرير العقود"),
        (CarPermissionRegistry.Users, "المستخدمون"),
        (CarPermissionRegistry.Permissions, "الصلاحيات"),
        (CarPermissionRegistry.PrintSettings, "إعدادات الطباعة"),
        (CarPermissionRegistry.Backup, "النسخ الاحتياطي"),
        (ScreenPermissionRegistry.NetworkConnection, "ربط الحاسبات"),
        (SystemUpdate, "تحديث النظام")
    ];

    private static IReadOnlyList<(string Name, string Label)> CarTradePermissionRegistryScreens { get; } =
    [
        (CarTradePermissionRegistry.Dashboard, "لوحة التحكم"),
        (CarTradePermissionRegistry.CarTradeForm, "عملية جديدة"),
        (CarTradePermissionRegistry.CarTradeList, "العمليات"),
        (CarTradePermissionRegistry.CarTradeReports, "التقارير"),
        (CarTradePermissionRegistry.CarTradePartyStatement, "كشف حساب طرف"),
        (CarTradePermissionRegistry.Users, "المستخدمون"),
        (CarTradePermissionRegistry.Permissions, "الصلاحيات"),
        (CarTradePermissionRegistry.PrintSettings, "إعدادات الطباعة"),
        (CarTradePermissionRegistry.Backup, "النسخ الاحتياطي"),
        (CarTradePermissionRegistry.CloudSync, "المزامنة السحابية"),
        (ScreenPermissionRegistry.NetworkConnection, "ربط الحاسبات"),
        (SystemUpdate, "تحديث النظام")
    ];

    private static IReadOnlyList<(string Name, string Label)> HotelPermissionRegistryScreens { get; } =
    [
        (HotelPermissionRegistry.Dashboard, "لوحة التحكم"),
        (HotelPermissionRegistry.Reservations, "الحجوزات"),
        (HotelPermissionRegistry.ReservationsCalendar, "تقويم الحجوزات"),
        (HotelPermissionRegistry.ReservationForm, "حجز جديد"),
        (HotelPermissionRegistry.CheckInOut, "تسجيل دخول/خروج"),
        (HotelPermissionRegistry.Rooms, "الغرف"),
        (HotelPermissionRegistry.RoomTypes, "أنواع الغرف"),
        (HotelPermissionRegistry.Floors, "الطوابق"),
        (HotelPermissionRegistry.Guests, "النزلاء"),
        (HotelPermissionRegistry.RatePlans, "خطط الأسعار"),
        (HotelPermissionRegistry.Housekeeping, "النظافة"),
        (HotelPermissionRegistry.RestaurantPos, "كاشير المطعم"),
        (HotelPermissionRegistry.RestaurantMenu, "قائمة المطعم"),
        (HotelPermissionRegistry.RestaurantInventory, "مخزون المطبخ"),
        (HotelPermissionRegistry.RestaurantTables, "طاولات الصالة"),
        (HotelPermissionRegistry.RestaurantReports, "تقارير المطعم"),
        (HotelPermissionRegistry.RestaurantKitchen, "شاشة المطبخ"),
        (HotelPermissionRegistry.HotelCash, "الصندوق"),
        (HotelPermissionRegistry.HotelExpenses, "المصاريف"),
        (HotelPermissionRegistry.HotelReports, "التقارير"),
        (HotelPermissionRegistry.Users, "المستخدمون"),
        (HotelPermissionRegistry.Permissions, "الصلاحيات"),
        (HotelPermissionRegistry.PrintSettings, "إعدادات الطباعة"),
        (HotelPermissionRegistry.Backup, "النسخ الاحتياطي"),
        (HotelPermissionRegistry.CloudSync, "المزامنة السحابية"),
        (ScreenPermissionRegistry.NetworkConnection, "ربط الحاسبات"),
        (SystemUpdate, "تحديث النظام")
    ];

    public static IReadOnlyList<(string Name, string Label)> AccountingScreens { get; } =
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
        (NetworkConnection, "ربط الحاسبات"),
        (SystemUpdate, "تحديث النظام"),
    ];

    public static string GetScreenName(Type viewModelType) =>
        IsHotelManagement
            ? HotelPermissionRegistry.GetScreenName(viewModelType)
            : IsCarTrading
                ? CarTradePermissionRegistry.GetScreenName(viewModelType)
            : IsCarContracts
                ? CarPermissionRegistry.GetScreenName(viewModelType)
                : GetAccountingScreenName(viewModelType);

    public static string GetAccountingScreenName(Type viewModelType) =>
        ViewModelToScreen.TryGetValue(viewModelType, out var name) ? name : viewModelType.Name;

    public static Type? GetDefaultViewModelType(string screenName) =>
        IsHotelManagement
            ? HotelPermissionRegistry.GetDefaultViewModelType(screenName)
            : IsCarTrading
                ? CarTradePermissionRegistry.GetDefaultViewModelType(screenName)
            : IsCarContracts
                ? CarPermissionRegistry.GetDefaultViewModelType(screenName)
                : GetAccountingDefaultViewModelType(screenName);

    public static Type? GetAccountingDefaultViewModelType(string screenName) =>
        ScreenToDefaultViewModel.TryGetValue(screenName, out var type) ? type : null;

    public static string GetLabel(string screenName) =>
        IsHotelManagement
            ? HotelPermissionRegistry.GetLabel(screenName)
            : IsCarTrading
                ? CarTradePermissionRegistry.GetLabel(screenName)
            : IsCarContracts
                ? CarPermissionRegistry.GetLabel(screenName)
                : GetAccountingLabel(screenName);

    public static string GetAccountingLabel(string screenName) =>
        AccountingScreens.FirstOrDefault(s => s.Name == screenName).Label ?? screenName;

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
        [typeof(InventoryReplenishmentReportViewModel)] = "Reports",
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
        [typeof(NetworkConnectionSettingsViewModel)] = NetworkConnection,
        [typeof(SystemUpdateViewModel)] = SystemUpdate,
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
        [NetworkConnection] = typeof(NetworkConnectionSettingsViewModel),
        [SystemUpdate] = typeof(SystemUpdateViewModel),
    };
}
