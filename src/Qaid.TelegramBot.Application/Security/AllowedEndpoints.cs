namespace Qaid.TelegramBot.Application.Security;

/// <summary>
/// Strict allow-list of Qaid cloud API paths the bot may call.
/// Write/mutate endpoints are intentionally absent.
/// </summary>
public static class AllowedEndpoints
{
    public const string Login = "api/auth/login";
    public const string Refresh = "api/auth/refresh";
    public const string SelectBranch = "api/auth/select-branch";
    public const string SyncStatus = "api/sync/status";

    public const string DailySales = "api/reports/daily-sales";
    public const string Sales = "api/reports/sales";
    public const string Expenses = "api/reports/expenses";
    public const string ProfitAndLoss = "api/reports/profit-and-loss";
    public const string Profit = "api/reports/profit";
    public const string ProfitComparison = "api/reports/profit/comparison";
    public const string OperatingProfit = "api/reports/operating-profit";
    public const string CashBalancesSummary = "api/reports/cash-balances-summary";
    public const string CashFlow = "api/reports/cash-flow";
    public const string CashBoxMovement = "api/reports/cash-box-movement";
    public const string BankAccountStatement = "api/reports/bank-account-statement";
    public const string CustomersOverview = "api/reports/customers/overview";
    public const string DailyOperations = "api/reports/daily-operations";
    public const string InstallmentsOverdue = "api/reports/installments/overdue";
    public const string InstallmentsSummary = "api/reports/installments/summary";
    public const string InstallmentsAging = "api/reports/installments/aging";
    public const string InstallmentsPaid = "api/reports/installments/paid";
    public const string InstallmentsUnpaid = "api/reports/installments/unpaid";
    public const string OpeningInstallmentBalances = "api/reports/opening-installment-balances";
    public const string CompanyFees = "api/reports/company-fees";
    public const string InstallmentSchedule = "api/reports/installment-schedule";
    public const string OverdueCustomers = "api/reports/overdue-customers";
    public const string ReceivablesAging = "api/reports/receivables-aging";
    public const string MinimumQuantity = "api/reports/minimum-quantity";
    public const string StockHealth = "api/reports/stock-health";
    public const string TopProducts = "api/reports/products/top";
    public const string ProductMargin = "api/reports/products/margin";
    public const string ProductMovement = "api/reports/products/movement";
    public const string Purchases = "api/reports/purchases";
    public const string SuppliersOverview = "api/reports/suppliers/overview";
    public const string SalesByPaymentMethod = "api/reports/sales-by-payment-method";
    public const string SalesByWarehouseUser = "api/reports/sales-by-warehouse-user";
    public const string PayablesAging = "api/reports/payables-aging";
    public const string CustomerCollections = "api/reports/customer-collections";
    public const string SupplierPayments = "api/reports/supplier-payments";
    public const string FinancialPositionSummary = "api/reports/financial-position-summary";
    public const string StatementOfFinancialPosition = "api/reports/statement-of-financial-position";
    public const string BalanceSheet = "api/reports/balance-sheet";
    public const string IncomeExpense = "api/reports/income-expense";
    public const string Cogs = "api/reports/cogs";
    public const string InventoryValuation = "api/reports/inventory-valuation";
    public const string InventoryReplenishment = "api/reports/inventory-replenishment";
    public const string Warehouse = "api/reports/warehouse";
    public const string StockTaking = "api/reports/stock-taking";
    public const string Transfers = "api/reports/transfers";
    public const string GrossProfitMargin = "api/reports/gross-profit-margin";
    public const string CapitalMovement = "api/reports/capital-movement";
    public const string InvestorProfitDistributions = "api/reports/investor-profit-distributions";

    public const string MasterCustomers = "api/master-data/customers";
    public const string MasterSuppliers = "api/master-data/suppliers";
    public const string CustomerStatement = "api/reports/statements/customer";
    public const string SupplierStatement = "api/reports/statements/supplier";
    public const string CarDashboard = "api/car/dashboard";
    public const string CarTradeDashboard = "api/car-trade/dashboard";
    public const string RealEstateDashboard = "api/real-estate/dashboard";
    public const string HotelRestaurantSummary = "api/hotel/restaurant/reports/summary";
    public const string GoldStock = "api/gold-shop/reports/stock";

    private static readonly HashSet<string> GetAllow = new(StringComparer.OrdinalIgnoreCase)
    {
        SyncStatus,
        DailySales,
        Sales,
        Expenses,
        ProfitAndLoss,
        Profit,
        ProfitComparison,
        OperatingProfit,
        CashBalancesSummary,
        CashFlow,
        CashBoxMovement,
        BankAccountStatement,
        CustomersOverview,
        DailyOperations,
        InstallmentsOverdue,
        InstallmentsSummary,
        InstallmentsAging,
        InstallmentsPaid,
        InstallmentsUnpaid,
        OpeningInstallmentBalances,
        CompanyFees,
        InstallmentSchedule,
        OverdueCustomers,
        ReceivablesAging,
        MinimumQuantity,
        StockHealth,
        TopProducts,
        ProductMargin,
        ProductMovement,
        Purchases,
        SuppliersOverview,
        SalesByPaymentMethod,
        SalesByWarehouseUser,
        PayablesAging,
        CustomerCollections,
        SupplierPayments,
        FinancialPositionSummary,
        StatementOfFinancialPosition,
        BalanceSheet,
        IncomeExpense,
        Cogs,
        InventoryValuation,
        InventoryReplenishment,
        Warehouse,
        StockTaking,
        Transfers,
        GrossProfitMargin,
        CapitalMovement,
        InvestorProfitDistributions,
        MasterCustomers,
        MasterSuppliers,
        CustomerStatement,
        SupplierStatement,
        CarDashboard,
        CarTradeDashboard,
        RealEstateDashboard,
        HotelRestaurantSummary,
        GoldStock
    };

    private static readonly HashSet<string> PostAllow = new(StringComparer.OrdinalIgnoreCase)
    {
        Login,
        Refresh,
        SelectBranch
    };

    public static readonly string[] ForbiddenWriteExamples =
    [
        "api/sync/push",
        "api/sync/pull",
        "api/mobile/invoices",
        "api/mobile/expenses",
        "api/mobile/vouchers",
        "api/admin/tenants"
    ];

    public static bool IsAllowed(HttpMethod method, string relativePath)
    {
        var path = Normalize(relativePath);
        if (method == HttpMethod.Get)
            return GetAllow.Contains(path);
        if (method == HttpMethod.Post)
            return PostAllow.Contains(path);
        return false;
    }

    public static void EnsureAllowed(HttpMethod method, string relativePath)
    {
        if (!IsAllowed(method, relativePath))
            throw new InvalidOperationException($"Endpoint not allowed for Telegram bot: {method} {relativePath}");
    }

    public static IReadOnlyCollection<string> AllAllowedGetPaths => GetAllow;

    private static string Normalize(string relativePath)
        => relativePath.Trim().TrimStart('/');
}
