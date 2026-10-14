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

public sealed class DailyOperationsReportResult
{
    public decimal CashSales { get; set; }
    public decimal Expenses { get; set; }
    public decimal SalesReturns { get; set; }
    public decimal SupplierPayments { get; set; }
    public decimal Receipts { get; set; }
    public decimal NetAmount { get; set; }
    public int CashSalesInvoiceCount { get; set; }
    public int ExpenseCount { get; set; }
}

public sealed class OverdueResult
{
    public int OverdueCustomerCount { get; set; }
    public decimal TotalOverdueAmount { get; set; }
    public string TopOverdueCustomer { get; set; } = string.Empty;
    public int AverageOverdueDays { get; set; }
    public List<OverdueRow> Rows { get; set; } = [];
}

public sealed class OverdueRow
{
    public string CustomerName { get; set; } = string.Empty;
    public string PlanNumber { get; set; } = string.Empty;
    public decimal OverdueAmount { get; set; }
    public int OverdueDays { get; set; }
    public DateTime DueDate { get; set; }
}

public sealed class InstallmentsSummaryResult
{
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal UnpaidAmount { get; set; }
    public decimal OverdueAmount { get; set; }
    public int TotalCount { get; set; }
    public int PaidCount { get; set; }
    public int UnpaidCount { get; set; }
    public int OverdueCount { get; set; }
    public List<InstallmentSummaryRow> Rows { get; set; } = [];
}

public sealed class InstallmentSummaryRow
{
    public string CustomerName { get; set; } = string.Empty;
    public string PlanNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class OverdueCustomersReportResult
{
    public decimal TotalOverdue { get; set; }
    public decimal TotalOverdueUsd { get; set; }
    public int CustomerCount { get; set; }
    public int ItemCount { get; set; }
    public decimal AverageDaysOverdue { get; set; }
    public List<OverdueCustomerRow> Rows { get; set; } = [];
}

public sealed class OverdueCustomerRow
{
    public string CustomerName { get; set; } = string.Empty;
    public decimal OverdueAmount { get; set; }
    public int DaysOverdue { get; set; }
    public DateTime DueDate { get; set; }
}

public sealed class ReceivablesAgingReportResult
{
    public decimal TotalOutstanding { get; set; }
    public decimal TotalOutstandingUsd { get; set; }
    public int RowCount { get; set; }
    public int CustomerCount { get; set; }
    public List<AgingBucketSummary> Buckets { get; set; } = [];
    public List<ReceivablesAgingRow> Rows { get; set; } = [];
}

public sealed class AgingBucketSummary
{
    public string BucketName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

public sealed class ReceivablesAgingRow
{
    public string CustomerName { get; set; } = string.Empty;
    public decimal RemainingAmount { get; set; }
    public int DaysOverdue { get; set; }
    public string AgingBucket { get; set; } = string.Empty;
}

public sealed class MinimumQuantityReportResult
{
    public int TotalItems { get; set; }
    public int BelowMinimumCount { get; set; }
    public int AtMinimumCount { get; set; }
    public int AboveMinimumCount { get; set; }
    public decimal TotalShortage { get; set; }
    public List<MinimumQuantityRow> Rows { get; set; } = [];
}

public sealed class MinimumQuantityRow
{
    public string ProductName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public decimal CurrentQuantity { get; set; }
    public decimal MinQuantity { get; set; }
    /// <summary>0=تحت الحد، 1=عند الحد، 2=فوق الحد</summary>
    public int Status { get; set; }
}

public sealed class StockHealthReportResult
{
    public int LowStockCount { get; set; }
    public int DeadStockCount { get; set; }
    public decimal TotalDeadStockValue { get; set; }
    public List<StockHealthRow> Rows { get; set; } = [];
}

public sealed class StockHealthRow
{
    public string ProductName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal StockValue { get; set; }
    /// <summary>0=منخفض، 1=راكد</summary>
    public int Status { get; set; }
}

public sealed class TopProductsReportResult
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalQuantity { get; set; }
    public int ProductCount { get; set; }
    public List<TopProductRow> Rows { get; set; } = [];
}

public sealed class TopProductRow
{
    public int Rank { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public decimal SharePercent { get; set; }
}

public sealed class PurchasesReportResult
{
    public decimal TotalPurchases { get; set; }
    public decimal TotalPurchasesUsd { get; set; }
    public int InvoiceCount { get; set; }
    public decimal AverageInvoice { get; set; }
    public List<PurchasesReportRow> Rows { get; set; } = [];
}

public sealed class PurchasesReportRow
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal NetAmount { get; set; }
}

public sealed class SuppliersOverviewReportResult
{
    public decimal TotalPurchases { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal TotalOutstandingUsd { get; set; }
    public int SupplierCount { get; set; }
    public List<SupplierOverviewRow> Rows { get; set; } = [];
}

public sealed class SupplierOverviewRow
{
    public string SupplierName { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal PurchaseAmount { get; set; }
    public decimal OutstandingBalance { get; set; }
}

public sealed class SalesByPaymentMethodReportResult
{
    public decimal TotalSales { get; set; }
    public decimal CashSales { get; set; }
    public decimal CreditSales { get; set; }
    public decimal InstallmentSales { get; set; }
    public int InvoiceCount { get; set; }
    public List<SalesByPaymentMethodRow> Rows { get; set; } = [];
}

public sealed class SalesByPaymentMethodRow
{
    public string PaymentMethod { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal Amount { get; set; }
    public decimal SharePercent { get; set; }
}

public sealed class PayablesAgingReportResult
{
    public decimal TotalOutstanding { get; set; }
    public decimal TotalOutstandingUsd { get; set; }
    public int RowCount { get; set; }
    public int SupplierCount { get; set; }
    public List<AgingBucketSummary> Buckets { get; set; } = [];
    public List<PayablesAgingRow> Rows { get; set; } = [];
}

public sealed class PayablesAgingRow
{
    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal RemainingAmount { get; set; }
    public int DaysOverdue { get; set; }
    public string AgingBucket { get; set; } = string.Empty;
}

public sealed class CustomerCollectionsReportResult
{
    public decimal TotalCollected { get; set; }
    public decimal TotalCollectedUsd { get; set; }
    public decimal VoucherCollections { get; set; }
    public decimal InstallmentCollections { get; set; }
    public int RowCount { get; set; }
    public List<CustomerCollectionRow> Rows { get; set; } = [];
}

public sealed class CustomerCollectionRow
{
    public DateTime Date { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class SupplierPaymentsReportResult
{
    public decimal TotalPaid { get; set; }
    public decimal VoucherPayments { get; set; }
    public decimal CashPurchases { get; set; }
    public int RowCount { get; set; }
    public List<SupplierPaymentRow> Rows { get; set; } = [];
}

public sealed class SupplierPaymentRow
{
    public DateTime Date { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class FinancialPositionSummaryReportResult
{
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public decimal NetWorkingCapital { get; set; }
    public decimal Difference { get; set; }
    public bool IsBalanced { get; set; }
    public List<FinancialPositionLineRow> Rows { get; set; } = [];
}

public sealed class FinancialPositionLineRow
{
    public string Section { get; set; } = string.Empty;
    public string LineName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class CogsReportResult
{
    public decimal TotalCogs { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalRevenueUsd { get; set; }
    public decimal GrossProfit { get; set; }
    public int ProductCount { get; set; }
    public List<CogsReportRow> Rows { get; set; } = [];
}

public sealed class CogsReportRow
{
    public string ProductName { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public decimal CogsAmount { get; set; }
    public decimal Revenue { get; set; }
    public decimal GrossProfit { get; set; }
}

public sealed class InventoryValuationReportResult
{
    public decimal TotalValue { get; set; }
    public decimal TotalQuantity { get; set; }
    public int ProductCount { get; set; }
    public int WarehouseCount { get; set; }
    public List<InventoryValuationRow> Rows { get; set; } = [];
}

public sealed class InventoryValuationRow
{
    public string ProductName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal TotalValue { get; set; }
}

public sealed class TransfersReportResult
{
    public decimal TotalAmount { get; set; }
    public decimal TotalAmountUsd { get; set; }
    public int TransferCount { get; set; }
    public decimal AverageAmount { get; set; }
    public List<TransferReportRow> Rows { get; set; } = [];
}

public sealed class TransferReportRow
{
    public DateTime Date { get; set; }
    public string FromAccount { get; set; } = string.Empty;
    public string ToAccount { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class GrossProfitMarginReportResult
{
    public decimal TotalSales { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossMarginPercent { get; set; }
    public List<GrossProfitMarginRow> Rows { get; set; } = [];
}

public sealed class GrossProfitMarginRow
{
    public DateTime Date { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal MarginPercent { get; set; }
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
    public int ApplicationSystemType { get; set; }
    public bool IsActive { get; set; }
    public DateTime LinkedAtUtc { get; set; }
    public DateTime SessionExpiresAtUtc { get; set; }
    public StoredSessionTokens Tokens { get; set; } = new();
}

public sealed class LookupItemDto
{
    public int Id { get; set; }
    public Guid SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Extra { get; set; }
    public string? FileNumber { get; set; }
    public decimal? BalanceIqd { get; set; }
    public decimal? BalanceUsd { get; set; }
}

public sealed class CustomerStatementResult
{
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerFileNumber { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal Balance { get; set; }
    public decimal BalanceUsd { get; set; }
    public int TransactionCount { get; set; }
    public List<StatementRowDto> Rows { get; set; } = [];
}

public sealed class SupplierStatementResult
{
    public string SupplierName { get; set; } = string.Empty;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal Balance { get; set; }
    public decimal BalanceUsd { get; set; }
    public int InvoiceCount { get; set; }
    public List<StatementRowDto> Rows { get; set; } = [];
}

public sealed class StatementRowDto
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
}

public sealed class CarDashboardDto
{
    public int TodayContracts { get; set; }
    public int MonthContracts { get; set; }
    public int TotalContracts { get; set; }
    public int UnpaidContracts { get; set; }
    public decimal TotalCarValue { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal TotalRemaining { get; set; }
}

public sealed class CarTradeDashboardDto
{
    public int TodayTransactions { get; set; }
    public int MonthTransactions { get; set; }
    public int TotalTransactions { get; set; }
    public int UnpaidTransactions { get; set; }
    public int BuyCount { get; set; }
    public int SellCount { get; set; }
    public decimal TotalBuyValue { get; set; }
    public decimal TotalSellValue { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalRemaining { get; set; }
}

public sealed class RealEstateDashboardDto
{
    public int TodayContracts { get; set; }
    public int MonthContracts { get; set; }
    public int TotalContracts { get; set; }
    public int UnpaidContracts { get; set; }
    public int OverdueDebts { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal TotalRemaining { get; set; }
}

public sealed class HotelRestaurantSummaryDto
{
    public decimal Revenue { get; set; }
    public decimal Cogs { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal MarginPercent { get; set; }
    public int OrderCount { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public sealed class GoldStockReportDto
{
    public decimal TotalGrams { get; set; }
    public decimal TotalValue { get; set; }
    public int LowStockCount { get; set; }
}

public sealed class CreateLinkCodeRequest
{
    public int TenantId { get; set; }
    public int TenantAccountId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int? CurrentBranchId { get; set; }
    public int ApplicationSystemType { get; set; }
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

public enum ReportGroup
{
    SalesProfit = 1,
    Installments = 2,
    Partners = 3,
    InventoryFinance = 4,
    Financial = 5,
    Supervisory = 6,
    System = 7
}

public enum ReportKind
{
    DailySales = 0,
    Expenses = 1,
    ProfitAndLoss = 2,
    CashBalances = 3,
    CashFlow = 4,
    CustomersOverview = 5,
    LastSync = 6,
    DailyOperations = 7,
    InstallmentsOverdue = 8,
    InstallmentsSummary = 9,
    OverdueCustomers = 10,
    ReceivablesAging = 11,
    MinimumQuantity = 12,
    StockHealth = 13,
    TopProducts = 14,
    Purchases = 15,
    SuppliersOverview = 16,
    SalesByPaymentMethod = 17,
    PayablesAging = 18,
    CustomerCollections = 19,
    SupplierPayments = 20,
    FinancialPosition = 21,
    Cogs = 22,
    InventoryValuation = 23,
    Transfers = 24,
    GrossProfitMargin = 25,
    Sales = 26,
    Profit = 27,
    ProfitComparison = 28,
    ProductMargin = 29,
    SalesByWarehouseUser = 30,
    OperatingProfit = 31,
    InstallmentsAging = 32,
    InstallmentsPaid = 33,
    InstallmentsUnpaid = 34,
    OpeningInstallmentBalances = 35,
    CompanyFees = 36,
    InstallmentSchedule = 37,
    Warehouse = 38,
    ProductMovement = 39,
    InventoryReplenishment = 40,
    StockTaking = 41,
    IncomeExpense = 42,
    BalanceSheet = 43,
    StatementOfFinancialPosition = 44,
    CapitalMovement = 45,
    InvestorProfitDistributions = 46,
    CashBoxMovement = 47,
    BankAccountStatement = 48
}

public enum ReportPeriod
{
    Today,
    Yesterday,
    Last7Days,
    ThisMonth,
    LastMonth,
    ThisYear
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
