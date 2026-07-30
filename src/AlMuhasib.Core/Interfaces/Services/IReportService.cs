using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IReportService
{
    // Sales & Purchases
    Task<SalesReportResult> GetSalesReportAsync(DateTime? from, DateTime? to, int? customerId, PaymentMethod? method, int? warehouseId = null);
    Task<PurchasesReportResult> GetPurchasesReportAsync(DateTime? from, DateTime? to, int? supplierId, int? warehouseId, PaymentMethod? method = null);

    // Profit
    Task<ProfitReportResult> GetProfitReportAsync(DateTime? from, DateTime? to);
    Task<List<MonthlyProfitRow>> GetMonthlyProfitAsync(DateTime? from, DateTime? to);
    Task<List<ProfitInvoiceDetailRow>> GetProfitInvoiceDetailsAsync(DateTime? from, DateTime? to);

    // Installments
    Task<InstallmentsSummaryResult> GetInstallmentsSummaryAsync(DateTime? from, DateTime? to, int? customerId, string? status);
    Task<InstallmentDetailResult> GetInstallmentDetailAsync(int customerId);
    Task<PaidInstallmentsResult> GetPaidInstallmentsAsync(DateTime? from, DateTime? to, int? customerId, int? cashBoxId);
    Task<UnpaidInstallmentsResult> GetUnpaidInstallmentsAsync(DateTime? from, DateTime? to, int? customerId);
    Task<OverdueResult> GetOverdueReportAsync(DateTime asOfDate, int? minDaysOverdue, int? customerId);

    // Statements
    Task<CustomerStatementResult> GetCustomerStatementAsync(int customerId, DateTime? from = null, DateTime? to = null);
    Task<SupplierStatementResult> GetSupplierStatementAsync(int supplierId, DateTime? from = null, DateTime? to = null);
    Task<InvestorStatementResult> GetInvestorStatementAsync(int investorId, DateTime? from = null, DateTime? to = null);

    // Expenses
    Task<ExpensesReportResult> GetExpensesReportAsync(DateTime? from, DateTime? to, int? expenseTypeId, int? cashBoxId);

    // Income & Expense
    Task<IncomeExpenseResult> GetIncomeExpenseReportAsync(DateTime? from, DateTime? to);

    // Warehouse
    Task<List<WarehouseStockRow>> GetWarehouseReportAsync(int? warehouseId, bool includeZero = false);

    // Investors
    Task<InvestorsReportResult> GetInvestorsReportAsync(int? investorId, DateTime? from, DateTime? to);

    // Cash Flow
    Task<CashFlowResult> GetCashFlowReportAsync(int? cashBoxId, DateTime? from, DateTime? to);

    // Balance Sheet
    Task<BalanceSheetResult> GetBalanceSheetAsync(DateTime date);

    // Products & collections (Phase 2)
    Task<TopProductsReportResult> GetTopProductsReportAsync(
        DateTime? from, DateTime? to, int? warehouseId, int topCount = 30, bool sortByRevenueDescending = true);

    Task<ProductProfitMarginReportResult> GetProductProfitMarginReportAsync(
        DateTime? from, DateTime? to, int? warehouseId);

    Task<MaterialNetProfitReportResult> GetMaterialNetProfitReportAsync(
        DateTime? from, DateTime? to, int? warehouseId, bool ascending = false, int? topN = null);

    Task<CustomerNetProfitReportResult> GetCustomerNetProfitReportAsync(
        DateTime? from, DateTime? to, bool ascending = false, int? topN = null);

    Task<InstallmentAgingReportResult> GetInstallmentAgingReportAsync(DateTime asOfDate, int? customerId);

    Task<CustomersOverviewReportResult> GetCustomersOverviewReportAsync(DateTime? from, DateTime? to);

    Task<SuppliersOverviewReportResult> GetSuppliersOverviewReportAsync(DateTime? from, DateTime? to);

    Task<ProfitComparisonResult> GetProfitComparisonAsync(DateTime? from, DateTime? to);

    Task<ProductMovementReportResult> GetProductMovementReportAsync(
        DateTime? from, DateTime? to, int? warehouseId, int? productId);

    Task<StockHealthReportResult> GetStockHealthReportAsync(
        int? warehouseId, decimal lowStockThreshold, int deadStockDays, StockHealthFilter filter = StockHealthFilter.All);

    Task<InventoryReplenishmentReportResult> GetInventoryReplenishmentReportAsync(
        DateTime? from,
        DateTime? to,
        int? warehouseId,
        decimal minimumStock,
        InventoryReplenishmentFilter filter = InventoryReplenishmentFilter.All);

    Task<MinimumQuantityReportResult> GetMinimumQuantityReportAsync(
        int? warehouseId,
        int? categoryId,
        MinimumQuantityFilter filter = MinimumQuantityFilter.All,
        string? search = null);

    Task<ExpiryReportResult> GetExpiryReportAsync(
        int? warehouseId = null,
        int? productId = null,
        string? productSearch = null,
        DateTime? expiryFrom = null,
        DateTime? expiryTo = null,
        ExpiryStatusFilter statusFilter = ExpiryStatusFilter.All,
        bool hideZeroQuantity = true,
        int nearExpiryCriticalDays = 30,
        int nearExpiryWarningDays = 90);
}

public enum StockHealthFilter
{
    All,
    LowStockOnly,
    DeadStockOnly
}

public enum StockHealthStatus
{
    LowStock,
    DeadStock
}

public enum InventoryReplenishmentFilter
{
    All,
    NeedsReplenishmentOnly
}

public enum InventoryReplenishmentStatus
{
    Sufficient,
    NeedsReorder,
    Critical
}

public enum ExpiryStatusFilter
{
    All,
    Expired,
    Within30Days,
    Within90Days,
    Valid
}

public enum ExpiryBatchStatus
{
    Expired,
    Critical,
    Warning,
    Valid,
    NoExpiry
}

// ══════════════════════════════════════════════════════════════
// SALES
// ══════════════════════════════════════════════════════════════

public class SalesReportResult
{
    public decimal TotalSales { get; set; }
    public decimal CashSales { get; set; }
    public decimal CreditSales { get; set; }
    public decimal InstallmentSales { get; set; }
    public int InvoiceCount { get; set; }
    public decimal AverageInvoice { get; set; }
    public decimal TodaySales { get; set; }
    public decimal TotalCompanyFees { get; set; }
    public List<SalesReportRow> Rows { get; set; } = [];
    public List<DailyAmountPoint> DailyChart { get; set; } = [];
}

public class SalesReportRow
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal Discount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal CompanyFeeAmount { get; set; }
    /// <summary>تاريخ استحقاق الدفع الآجل — فارغ للنقدي والأقساط</summary>
    public DateTime? CreditDueDate { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public bool IsCreditPaid { get; set; }
    public bool IsCredit => PaymentMethod == "آجل";
}

public class DailyAmountPoint
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
}

// ══════════════════════════════════════════════════════════════
// PURCHASES
// ══════════════════════════════════════════════════════════════

public class PurchasesReportResult
{
    public decimal TotalPurchases { get; set; }
    public int InvoiceCount { get; set; }
    public decimal AverageInvoice { get; set; }
    public decimal TodayPurchases { get; set; }
    public List<PurchasesReportRow> Rows { get; set; } = [];
    public List<DailyAmountPoint> DailyChart { get; set; } = [];
    public List<NameAmountPoint> BySupplierChart { get; set; } = [];
}

public class PurchasesReportRow
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal Discount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public bool IsCreditPaid { get; set; }
    public bool IsCredit => PaymentMethod == "آجل";
}

public class NameAmountPoint
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

// ══════════════════════════════════════════════════════════════
// PROFIT
// ══════════════════════════════════════════════════════════════

public class ProfitReportResult
{
    public decimal TotalSales { get; set; }
    public decimal TotalPurchases { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalBankFees { get; set; }
    public decimal DistributedProfits { get; set; }
    public decimal NetProfit { get; set; }
    public decimal ProfitMargin { get; set; }
}

public class MonthlyProfitRow
{
    public string Month { get; set; } = string.Empty;
    public decimal Sales { get; set; }
    public decimal Purchases { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal Expenses { get; set; }
    public decimal NetProfit { get; set; }
    public decimal ProfitMargin { get; set; }
}

public class ProfitInvoiceDetailRow
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string InvoiceTypeLabel { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal MarginPercent { get; set; }
}

// ══════════════════════════════════════════════════════════════
// INSTALLMENTS
// ══════════════════════════════════════════════════════════════

public class InstallmentsSummaryResult
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
    public List<NameAmountPoint> StatusChart { get; set; } = [];
    public List<DailyAmountPoint> MonthlyCollectionChart { get; set; } = [];
}

public class InstallmentSummaryRow
{
    public string CustomerName { get; set; } = string.Empty;
    public string PlanNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int InstallmentCount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class InstallmentDetailResult
{
    public int PlanCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal CollectionRate { get; set; }
    public decimal AverageInstallment { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<InstallmentDetailRow> Rows { get; set; } = [];
    public List<DailyAmountPoint> MonthlyDueChart { get; set; } = [];
}

public class InstallmentDetailRow
{
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PlanNumber { get; set; } = string.Empty;
}

public class PaidInstallmentsResult
{
    public decimal TotalPaid { get; set; }
    public int PaidCount { get; set; }
    public decimal AveragePaymentDays { get; set; }
    public decimal MaxPaid { get; set; }
    public List<PaidInstallmentRow> Rows { get; set; } = [];
    public List<DailyAmountPoint> MonthlyChart { get; set; } = [];
    public List<NameAmountPoint> ByCashBoxChart { get; set; } = [];
}

public class PaidInstallmentRow
{
    public string CustomerName { get; set; } = string.Empty;
    public string PlanNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string CashBoxName { get; set; } = string.Empty;
}

public class UnpaidInstallmentsResult
{
    public decimal TotalUnpaid { get; set; }
    public int UnpaidCount { get; set; }
    public int CustomerCount { get; set; }
    public int OldestOverdueDays { get; set; }
    public List<UnpaidInstallmentRow> Rows { get; set; } = [];
    public List<NameAmountPoint> ByCustomerChart { get; set; } = [];
}

public class UnpaidInstallmentRow
{
    public int InstallmentId { get; set; }
    public int InvoiceId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PlanNumber { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int OverdueDays { get; set; }
}

public class OverdueResult
{
    public int OverdueCustomerCount { get; set; }
    public decimal TotalOverdueAmount { get; set; }
    public string TopOverdueCustomer { get; set; } = string.Empty;
    public int AverageOverdueDays { get; set; }
    public List<OverdueRow> Rows { get; set; } = [];
    public List<NameAmountPoint> TopCustomersChart { get; set; } = [];
    public List<NameAmountPoint> OverdueBucketChart { get; set; } = [];
}

public class OverdueRow
{
    public int InstallmentId { get; set; }
    public int InvoiceId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PlanNumber { get; set; } = string.Empty;
    public decimal OverdueAmount { get; set; }
    public int OverdueDays { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public DateTime DueDate { get; set; }
}

// ══════════════════════════════════════════════════════════════
// STATEMENTS
// ══════════════════════════════════════════════════════════════

public class CustomerStatementResult
{
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal Balance { get; set; }
    public int TransactionCount { get; set; }
    public List<CustomerStatementRow> Rows { get; set; } = [];
}

public class CustomerStatementRow
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
}

public class SupplierStatementResult
{
    public string SupplierName { get; set; } = string.Empty;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal Balance { get; set; }
    public int InvoiceCount { get; set; }
    public List<SupplierStatementRow> Rows { get; set; } = [];
}

public class SupplierStatementRow
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
}

public class InvestorStatementResult
{
    public string InvestorName { get; set; } = string.Empty;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal Balance { get; set; }
    public int TransactionCount { get; set; }
    public List<InvestorStatementRow> Rows { get; set; } = [];
}

public class InvestorStatementRow
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
}

// ══════════════════════════════════════════════════════════════
// EXPENSES
// ══════════════════════════════════════════════════════════════

public class ExpensesReportResult
{
    public decimal TotalExpenses { get; set; }
    public decimal TodayExpenses { get; set; }
    public decimal MonthExpenses { get; set; }
    public string TopExpenseType { get; set; } = string.Empty;
    public List<ExpenseReportRow> Rows { get; set; } = [];
    public List<NameAmountPoint> ByTypeChart { get; set; } = [];
    public List<DailyAmountPoint> DailyChart { get; set; } = [];
}

public class ExpenseReportRow
{
    public DateTime Date { get; set; }
    public string ExpenseTypeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CashBoxName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}

// ══════════════════════════════════════════════════════════════
// INCOME & EXPENSE
// ══════════════════════════════════════════════════════════════

public class IncomeExpenseResult
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetResult { get; set; }
    public decimal ExpenseRate { get; set; }
    public List<IncomeExpenseRow> Rows { get; set; } = [];
    public List<MonthlyIncomeExpensePoint> MonthlyChart { get; set; } = [];
}

public class IncomeExpenseRow
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Section { get; set; } = string.Empty;
}

public class MonthlyIncomeExpensePoint
{
    public string Month { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
}

// ══════════════════════════════════════════════════════════════
// WAREHOUSE
// ══════════════════════════════════════════════════════════════

public class WarehouseStockRow
{
    public string ProductName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public decimal TotalValue { get; set; }
}

// ══════════════════════════════════════════════════════════════
// INVESTORS
// ══════════════════════════════════════════════════════════════

public class InvestorsReportResult
{
    public decimal TotalInvestments { get; set; }
    public decimal TotalDistributed { get; set; }
    public int InvestorCount { get; set; }
    public DateTime? LastDistributionDate { get; set; }
    public List<InvestorReportRow> Rows { get; set; } = [];
    public List<NameAmountPoint> SharesChart { get; set; } = [];
    public List<NameAmountPoint> DistributedChart { get; set; } = [];
}

public class InvestorReportRow
{
    public string InvestorName { get; set; } = string.Empty;
    public decimal TotalDeposit { get; set; }
    public decimal EligibleDeposit { get; set; }
    public decimal ProfitPercentage { get; set; }
    public decimal TotalDistributed { get; set; }
    public DateTime? LastWithdrawal { get; set; }
}

// ══════════════════════════════════════════════════════════════
// CASH FLOW
// ══════════════════════════════════════════════════════════════

public class CashFlowResult
{
    public decimal TotalIncoming { get; set; }
    public decimal TotalOutgoing { get; set; }
    public decimal NetFlow { get; set; }
    public decimal CurrentBalance { get; set; }
    public List<CashFlowRow> Rows { get; set; } = [];
    public List<DailyAmountPoint> DailyIncomingChart { get; set; } = [];
    public List<DailyAmountPoint> DailyOutgoingChart { get; set; } = [];
}

public class CashFlowRow
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Incoming { get; set; }
    public decimal Outgoing { get; set; }
    public decimal Balance { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}

// ══════════════════════════════════════════════════════════════
// BALANCE SHEET
// ══════════════════════════════════════════════════════════════

public class BalanceSheetResult
{
    public decimal Capital { get; set; }
    public decimal Adjustments { get; set; }
    public decimal AccumulatedProfits { get; set; }
    public decimal EquityTotal { get; set; }

    public decimal ProfitOpeningBalance { get; set; }
    public decimal SalesTotal { get; set; }
    public decimal CostOfSales { get; set; }
    public decimal SalesProfit { get; set; }
    public decimal ExpensesTotal { get; set; }

    public decimal SupplierPayables { get; set; }
    public decimal InvestorDeposits { get; set; }
    public decimal LiabilitiesTotal { get; set; }

    public decimal EquityAndLiabilitiesTotal { get; set; }

    public decimal CashBoxesTotal { get; set; }
    public List<BalanceSheetCashBoxRow> CashBoxes { get; set; } = [];
    public decimal BanksTotal { get; set; }
    public List<BalanceSheetBankRow> Banks { get; set; } = [];
    public decimal CustomerDebts { get; set; }
    public decimal InventoryValue { get; set; }
    public decimal InstallmentReceivables { get; set; }
    public decimal AssetsTotal { get; set; }

    public decimal Difference { get; set; }
    public bool IsBalanced { get; set; }
}

public class BalanceSheetCashBoxRow
{
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}

public class BalanceSheetBankRow
{
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}

// ══════════════════════════════════════════════════════════════
// TOP PRODUCTS & PROFIT MARGIN
// ══════════════════════════════════════════════════════════════

public class TopProductsReportResult
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalQuantity { get; set; }
    public int ProductCount { get; set; }
    public List<TopProductRow> Rows { get; set; } = [];
    public List<NameAmountPoint> Chart { get; set; } = [];
}

public class TopProductRow
{
    public int Rank { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public decimal SharePercent { get; set; }
}

public class ProductProfitMarginReportResult
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalGrossProfit { get; set; }
    public decimal AverageMarginPercent { get; set; }
    public List<ProductProfitMarginRow> Rows { get; set; } = [];
}

public class ProductProfitMarginRow
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal MarginPercent { get; set; }
}

// ══════════════════════════════════════════════════════════════
// MATERIAL NET PROFIT
// ══════════════════════════════════════════════════════════════

public class MaterialNetProfitReportResult
{
    public decimal TotalNetProfit { get; set; }
    public decimal TotalStockValue { get; set; }
    public int ProductCount { get; set; }
    public decimal AverageMarginPercent { get; set; }
    public List<MaterialNetProfitRow> Rows { get; set; } = [];
    public List<NameAmountPoint> Chart { get; set; } = [];
}

public class MaterialNetProfitRow
{
    public int Rank { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal StockQuantity { get; set; }
    public decimal StockValue { get; set; }
    public decimal QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal NetProfit { get; set; }
    public decimal MarginPercent { get; set; }
}

// ══════════════════════════════════════════════════════════════
// CUSTOMER NET PROFIT
// ══════════════════════════════════════════════════════════════

public class CustomerNetProfitReportResult
{
    public decimal TotalNetProfit { get; set; }
    public decimal TotalOutstanding { get; set; }
    public int CustomerCount { get; set; }
    public decimal AverageMarginPercent { get; set; }
    public List<CustomerNetProfitRow> Rows { get; set; } = [];
    public List<NameAmountPoint> Chart { get; set; } = [];
}

public class CustomerNetProfitRow
{
    public int Rank { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal SalesAmount { get; set; }
    public decimal Cost { get; set; }
    public decimal NetProfit { get; set; }
    public decimal MarginPercent { get; set; }
    public decimal OutstandingBalance { get; set; }
}

// ══════════════════════════════════════════════════════════════
// INSTALLMENT AGING
// ══════════════════════════════════════════════════════════════

public class InstallmentAgingReportResult
{
    public decimal TotalOutstanding { get; set; }
    public int InstallmentCount { get; set; }
    public int CustomerCount { get; set; }
    public List<InstallmentAgingBucketSummary> Buckets { get; set; } = [];
    public List<InstallmentAgingRow> Rows { get; set; } = [];
}

public class InstallmentAgingBucketSummary
{
    public string BucketName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

public class InstallmentAgingRow
{
    public int InstallmentId { get; set; }
    public int InvoiceId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PlanNumber { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int DaysOverdue { get; set; }
    public string AgingBucket { get; set; } = string.Empty;
}

// ══════════════════════════════════════════════════════════════
// CUSTOMERS OVERVIEW
// ══════════════════════════════════════════════════════════════

public class CustomersOverviewReportResult
{
    public decimal TotalSales { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalOutstanding { get; set; }
    public int CustomerCount { get; set; }
    public List<CustomerOverviewRow> Rows { get; set; } = [];
}

public class CustomerOverviewRow
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal SalesAmount { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal OutstandingBalance { get; set; }
}

public class SuppliersOverviewReportResult
{
    public decimal TotalPurchases { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
    public int SupplierCount { get; set; }
    public List<SupplierOverviewRow> Rows { get; set; } = [];
}

public class SupplierOverviewRow
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal PurchaseAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingBalance { get; set; }
}

public class ProfitComparisonResult
{
    public DateTime CurrentFrom { get; set; }
    public DateTime CurrentTo { get; set; }
    public DateTime PreviousFrom { get; set; }
    public DateTime PreviousTo { get; set; }
    public ProfitReportResult Current { get; set; } = new();
    public ProfitReportResult Previous { get; set; } = new();
    public decimal SalesChangePercent { get; set; }
    public decimal GrossProfitChangePercent { get; set; }
    public decimal NetProfitChangePercent { get; set; }
}

public class ProductMovementReportResult
{
    public decimal TotalQuantityIn { get; set; }
    public decimal TotalQuantityOut { get; set; }
    public int ProductCount { get; set; }
    public List<ProductMovementRow> Rows { get; set; } = [];
}

public class ProductMovementRow
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal QuantityIn { get; set; }
    public decimal QuantityOut { get; set; }
    public decimal NetQuantity => QuantityIn - QuantityOut;
}

public class StockHealthReportResult
{
    public int LowStockCount { get; set; }
    public int DeadStockCount { get; set; }
    public decimal TotalDeadStockValue { get; set; }
    public List<StockHealthRow> Rows { get; set; } = [];
}

public class StockHealthRow
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public decimal StockValue { get; set; }
    public StockHealthStatus Status { get; set; }
    public int? DaysSinceLastSale { get; set; }
    public DateTime? LastSaleDate { get; set; }
    public string StatusDisplay => Status == StockHealthStatus.DeadStock ? "راكد" : "منخفض";
}

public class InventoryReplenishmentReportResult
{
    public int TotalProducts { get; set; }
    public decimal TotalCurrentQuantity { get; set; }
    public decimal TotalSoldQuantity { get; set; }
    public decimal TotalSuggestedOrderQuantity { get; set; }
    public int ItemsNeedingReplenishment { get; set; }
    public decimal TotalStockValue { get; set; }
    public decimal EstimatedOrderValue { get; set; }
    public List<InventoryReplenishmentRow> Rows { get; set; } = [];
    public List<NameAmountPoint> StatusChart { get; set; } = [];
    public List<NameAmountPoint> ReorderChart { get; set; } = [];
    public List<InventoryReplenishmentRow> StockVsSoldChart { get; set; } = [];
}

public class InventoryReplenishmentRow
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal CurrentQuantity { get; set; }
    public decimal QuantitySold { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal SuggestedOrderQuantity { get; set; }
    public decimal AverageCost { get; set; }
    public decimal StockValue { get; set; }
    public decimal EstimatedOrderValue { get; set; }
    public InventoryReplenishmentStatus Status { get; set; }

    public string StatusDisplay => Status switch
    {
        InventoryReplenishmentStatus.Critical => "حرج",
        InventoryReplenishmentStatus.NeedsReorder => "يحتاج توريد",
        _ => "كافٍ"
    };
}

public enum MinimumQuantityFilter
{
    All,
    BelowMinimum,
    AtMinimum,
    AboveMinimum
}

public enum MinimumQuantityStatus
{
    BelowMinimum,
    AtMinimum,
    AboveMinimum
}

public class MinimumQuantityReportResult
{
    public int TotalItems { get; set; }
    public int BelowMinimumCount { get; set; }
    public int AtMinimumCount { get; set; }
    public int AboveMinimumCount { get; set; }
    public decimal TotalShortage { get; set; }
    public List<MinimumQuantityRow> Rows { get; set; } = [];
}

public class MinimumQuantityRow
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal CurrentQuantity { get; set; }
    public decimal MinQuantity { get; set; }
    public decimal Difference => CurrentQuantity - MinQuantity;
    public MinimumQuantityStatus Status { get; set; }

    public string StatusDisplay => Status switch
    {
        MinimumQuantityStatus.BelowMinimum => "تحت الحد",
        MinimumQuantityStatus.AtMinimum => "مساوٍ للحد",
        _ => "فوق الحد"
    };
}

public class ExpiryReportResult
{
    public int ExpiredCount { get; set; }
    public int CriticalCount { get; set; }
    public int WarningCount { get; set; }
    public decimal AffectedQuantity { get; set; }
    public List<ExpiryReportRow> Rows { get; set; } = [];
}

public class ExpiryReportRow
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductBarcode { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal Quantity { get; set; }
    public int? DaysRemaining { get; set; }
    public ExpiryBatchStatus Status { get; set; }

    public string BatchNumberDisplay => string.IsNullOrWhiteSpace(BatchNumber) ? "بدون" : BatchNumber;
    public string ExpiryDateDisplay => ExpiryDate?.ToString("yyyy/MM/dd") ?? "—";
    public string DaysRemainingDisplay => DaysRemaining?.ToString() ?? "—";

    public string StatusDisplay => Status switch
    {
        ExpiryBatchStatus.Expired => "منتهي",
        ExpiryBatchStatus.Critical => "قريب (30 يوم)",
        ExpiryBatchStatus.Warning => "تحذير (90 يوم)",
        ExpiryBatchStatus.Valid => "صالح",
        _ => "بدون تاريخ"
    };
}
