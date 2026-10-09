using System.Text.Json.Serialization;

namespace Qaid.TelegramBot.Application.Models;

public sealed class TenantLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class SelectBranchRequest
{
    public int BranchId { get; set; }
    public bool AllBranches { get; set; }
}

public sealed class BranchInfoDto
{
    public int BranchId { get; set; }
    public Guid SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class TenantLoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public int TenantId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public int ApplicationSystemType { get; set; }
    public bool IsMobileEnabled { get; set; }
    public DateTime? LicenseExpiresAt { get; set; }
    public DateTime? AccountExpiresAt { get; set; }
    public List<BranchInfoDto> AllowedBranches { get; set; } = [];
    public int? DefaultBranchId { get; set; }
    public bool RequiresBranchSelection { get; set; }
    public bool CanViewAllBranches { get; set; }
    public bool CanManageAllBranches { get; set; }
    public int? CurrentBranchId { get; set; }
}

public sealed class SelectBranchResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public int? CurrentBranchId { get; set; }
    public bool IsAllBranchesMode { get; set; }
    public BranchInfoDto? Branch { get; set; }
}

public sealed class ApiErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class SyncStatusResponse
{
    public DateTime? LastSyncAt { get; set; }
    public int PendingPushCount { get; set; }
    public bool IsLicensed { get; set; }
    public string? LicenseMessage { get; set; }
}

public sealed class DailySalesReportResult
{
    public decimal TotalSales { get; set; }
    public decimal TotalSalesUsd { get; set; }
    public int DayCount { get; set; }
    public int InvoiceCount { get; set; }
    public decimal AverageDaily { get; set; }
    public List<DailySalesRow> Rows { get; set; } = [];
}

public sealed class DailySalesRow
{
    public DateTime Date { get; set; }
    public int InvoiceCount { get; set; }
    public decimal CashSales { get; set; }
    public decimal CreditSales { get; set; }
    public decimal InstallmentSales { get; set; }
    public decimal TotalSales { get; set; }
}

public sealed class ExpensesReportResult
{
    public decimal TotalExpenses { get; set; }
    public decimal TodayExpenses { get; set; }
    public decimal MonthExpenses { get; set; }
    public string TopExpenseType { get; set; } = string.Empty;
    public List<ExpenseReportRow> Rows { get; set; } = [];
}

public sealed class ExpenseReportRow
{
    public DateTime Date { get; set; }
    public string ExpenseTypeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CashBoxName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class ProfitAndLossReportResult
{
    public decimal TotalSales { get; set; }
    public decimal TotalSalesUsd { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalExpensesUsd { get; set; }
    public decimal OperatingProfit { get; set; }
    public decimal NetProfit { get; set; }
    public decimal GrossMarginPercent { get; set; }
    public decimal NetMarginPercent { get; set; }
}

public sealed class CashBalancesSummaryReportResult
{
    public decimal CashBoxesTotal { get; set; }
    public decimal BanksTotal { get; set; }
    public decimal TotalLiquid { get; set; }
    public decimal CashBoxesTotalUsd { get; set; }
    public decimal BanksTotalUsd { get; set; }
    public decimal TotalLiquidUsd { get; set; }
    public int AccountCount { get; set; }
    public List<CashBalanceRow> Rows { get; set; } = [];
}

public sealed class CashBalanceRow
{
    public string AccountType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}

public sealed class CashFlowResult
{
    public decimal TotalIncoming { get; set; }
    public decimal TotalOutgoing { get; set; }
    public decimal NetFlow { get; set; }
    public decimal CurrentBalance { get; set; }
    public List<CashFlowRow> Rows { get; set; } = [];
}

public sealed class CashFlowRow
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Incoming { get; set; }
    public decimal Outgoing { get; set; }
}

public sealed class CustomersOverviewReportResult
{
    public decimal TotalSales { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal TotalOutstandingUsd { get; set; }
    public int CustomerCount { get; set; }
    public List<CustomerOverviewRow> Rows { get; set; } = [];
}

public sealed class CustomerOverviewRow
{
    public string CustomerName { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal SalesAmount { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal OutstandingBalance { get; set; }
}

public sealed class StoredSessionTokens
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
}

public sealed class TelegramLinkInfo
{
    public long TelegramUserId { get; set; }
    public int TenantId { get; set; }
    public int TenantAccountId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int? CurrentBranchId { get; set; }
    public bool IsActive { get; set; }
    public StoredSessionTokens Tokens { get; set; } = new();
}

public sealed class CreateLinkCodeRequest
{
    public int TenantId { get; set; }
    public int TenantAccountId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int? CurrentBranchId { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
}

public sealed class CreateLinkCodeResult
{
    public string PlainCode { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}

public enum ReportKind
{
    DailySales,
    Expenses,
    ProfitAndLoss,
    CashBalances,
    CashFlow,
    CustomersOverview,
    LastSync
}

public enum ReportPeriod
{
    Today,
    Yesterday,
    Last7Days,
    ThisMonth,
    LastMonth
}

public sealed class DateRange
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string Label { get; init; } = string.Empty;
}

/// <summary>JSON helper ignored by serializer when unknown props appear.</summary>
public sealed class IgnoreExtra
{
    [JsonExtensionData]
    public Dictionary<string, object?>? Extra { get; set; }
}
