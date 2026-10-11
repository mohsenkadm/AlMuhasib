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
    public const string Expenses = "api/reports/expenses";
    public const string ProfitAndLoss = "api/reports/profit-and-loss";
    public const string CashBalancesSummary = "api/reports/cash-balances-summary";
    public const string CashFlow = "api/reports/cash-flow";
    public const string CustomersOverview = "api/reports/customers/overview";
    public const string DailyOperations = "api/reports/daily-operations";
    public const string InstallmentsOverdue = "api/reports/installments/overdue";
    public const string InstallmentsSummary = "api/reports/installments/summary";
    public const string OverdueCustomers = "api/reports/overdue-customers";
    public const string ReceivablesAging = "api/reports/receivables-aging";
    public const string MinimumQuantity = "api/reports/minimum-quantity";
    public const string StockHealth = "api/reports/stock-health";
    public const string TopProducts = "api/reports/products/top";
    public const string Purchases = "api/reports/purchases";
    public const string SuppliersOverview = "api/reports/suppliers/overview";
    public const string SalesByPaymentMethod = "api/reports/sales-by-payment-method";
    public const string PayablesAging = "api/reports/payables-aging";
    public const string CustomerCollections = "api/reports/customer-collections";
    public const string SupplierPayments = "api/reports/supplier-payments";
    public const string FinancialPositionSummary = "api/reports/financial-position-summary";
    public const string Cogs = "api/reports/cogs";
    public const string InventoryValuation = "api/reports/inventory-valuation";
    public const string Transfers = "api/reports/transfers";
    public const string GrossProfitMargin = "api/reports/gross-profit-margin";
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
        Expenses,
        ProfitAndLoss,
        CashBalancesSummary,
        CashFlow,
        CustomersOverview,
        DailyOperations,
        InstallmentsOverdue,
        InstallmentsSummary,
        OverdueCustomers,
        ReceivablesAging,
        MinimumQuantity,
        StockHealth,
        TopProducts,
        Purchases,
        SuppliersOverview,
        SalesByPaymentMethod,
        PayablesAging,
        CustomerCollections,
        SupplierPayments,
        FinancialPositionSummary,
        Cogs,
        InventoryValuation,
        Transfers,
        GrossProfitMargin,
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

    /// <summary>Paths forbidden for the bot (documented non-exhaustive write surface).</summary>
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
