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

    private static readonly HashSet<string> GetAllow = new(StringComparer.OrdinalIgnoreCase)
    {
        SyncStatus,
        DailySales,
        Expenses,
        ProfitAndLoss,
        CashBalancesSummary,
        CashFlow,
        CustomersOverview
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
