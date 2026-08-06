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

    // ── New extended reports ──────────────────────────────────────
    Task<InvestorProfitDistributionsReportResult> GetInvestorProfitDistributionsReportAsync(DateTime? from, DateTime? to, int? investorId);
    Task<CapitalMovementReportResult> GetCapitalMovementReportAsync(DateTime? from, DateTime? to);
    Task<OpeningInstallmentBalancesReportResult> GetOpeningInstallmentBalancesReportAsync(DateTime? from, DateTime? to, int? customerId);
    Task<CompanyFeeReportResult> GetCompanyFeeReportAsync(DateTime? from, DateTime? to, int? customerId);
    Task<InstallmentScheduleReportResult> GetInstallmentScheduleReportAsync(DateTime? from, DateTime? to, int? customerId, string? status);
    Task<SalesByPaymentMethodReportResult> GetSalesByPaymentMethodReportAsync(DateTime? from, DateTime? to, int? warehouseId);
    Task<DailySalesReportResult> GetDailySalesReportAsync(DateTime? from, DateTime? to, int? warehouseId, PaymentMethod? method);
    Task<SalesByWarehouseUserReportResult> GetSalesByWarehouseUserReportAsync(DateTime? from, DateTime? to, int? warehouseId);
    Task<GrossProfitMarginReportResult> GetGrossProfitMarginReportAsync(DateTime? from, DateTime? to);
    Task<OperatingProfitReportResult> GetOperatingProfitReportAsync(DateTime? from, DateTime? to);
    Task<ReceivablesAgingReportResult> GetReceivablesAgingReportAsync(DateTime asOfDate, int? customerId);
    Task<PayablesAgingReportResult> GetPayablesAgingReportAsync(DateTime asOfDate, int? supplierId);
    Task<CustomerCollectionsReportResult> GetCustomerCollectionsReportAsync(DateTime? from, DateTime? to, int? customerId, int? cashBoxId);
    Task<OverdueCustomersReportResult> GetOverdueCustomersReportAsync(DateTime asOfDate, int? minDaysOverdue, int? customerId);
    Task<SupplierPaymentsReportResult> GetSupplierPaymentsReportAsync(DateTime? from, DateTime? to, int? supplierId);
    Task<BankAccountStatementReportResult> GetBankAccountStatementReportAsync(int? bankAccountId, DateTime? from, DateTime? to);
    Task<CashBoxMovementReportResult> GetCashBoxMovementReportAsync(int? cashBoxId, DateTime? from, DateTime? to);
    Task<CashBalancesSummaryReportResult> GetCashBalancesSummaryReportAsync();
    Task<TransfersReportResult> GetTransfersReportAsync(DateTime? from, DateTime? to);
    Task<InventoryValuationReportResult> GetInventoryValuationReportAsync(int? warehouseId, bool includeZero = false);
    Task<StockTakingReportResult> GetStockTakingReportAsync(int? warehouseId, bool includeZero = true);
    Task<CogsReportResult> GetCogsReportAsync(DateTime? from, DateTime? to, int? warehouseId);
    Task<FinancialPositionSummaryReportResult> GetFinancialPositionSummaryReportAsync(DateTime? asOfDate);
    Task<ProfitAndLossReportResult> GetProfitAndLossReportAsync(DateTime? from, DateTime? to);
    Task<StatementOfFinancialPositionReportResult> GetStatementOfFinancialPositionReportAsync(DateTime date);
    Task<WorkSummaryReportResult> GetWorkSummaryAsync(DateTime? from, DateTime? to);

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


// ══════════════════════════════════════════════════════════════
// NEW EXTENDED REPORTS
// ══════════════════════════════════════════════════════════════

public class InvestorProfitDistributionsReportResult
{
    public decimal TotalProfit { get; set; }
    public decimal TotalDistributed { get; set; }
    public int DistributionCount { get; set; }
    public int InvestorCount { get; set; }
    public List<InvestorProfitDistributionRow> Rows { get; set; } = [];
    public List<NameAmountPoint> ByInvestorChart { get; set; } = [];
    public List<DailyAmountPoint> DailyChart { get; set; } = [];
    public List<InvestorProfitDistributionDetailRow> Details { get; set; } = [];
}

public class InvestorProfitDistributionRow
{
    public int DistributionId { get; set; }
    public DateTime Date { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal DistributedAmount { get; set; }
    public int DetailCount { get; set; }
}

public class InvestorProfitDistributionDetailRow
{
    public int DistributionId { get; set; }
    public DateTime Date { get; set; }
    public int InvestorId { get; set; }
    public string InvestorName { get; set; } = string.Empty;
    public decimal ProfitPercentage { get; set; }
    public decimal Amount { get; set; }
}

public class CapitalMovementReportResult
{
    public decimal InitialCapital { get; set; }
    public decimal Adjustments { get; set; }
    public decimal ProfitOpening { get; set; }
    public decimal EquityCapital { get; set; }
    public List<CapitalMovementRow> Rows { get; set; } = [];
    public List<NameAmountPoint> ByTypeChart { get; set; } = [];
    public List<DailyAmountPoint> DailyChart { get; set; } = [];
}

public class CapitalMovementRow
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string TypeDisplay { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}

public class OpeningInstallmentBalancesReportResult
{
    public decimal TotalAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalRemaining { get; set; }
    public int PlanCount { get; set; }
    public int CustomerCount { get; set; }
    public List<OpeningInstallmentBalanceRow> Rows { get; set; } = [];
    public List<NameAmountPoint> StatusChart { get; set; } = [];
}

public class OpeningInstallmentBalanceRow
{
    public int PlanId { get; set; }
    public int InvoiceId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int InstallmentCount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CompanyFeeReportResult
{
    public decimal TotalFees { get; set; }
    public decimal TotalSales { get; set; }
    public decimal AverageFeePercent { get; set; }
    public int InvoiceCount { get; set; }
    public List<CompanyFeeRow> Rows { get; set; } = [];
    public List<DailyAmountPoint> DailyChart { get; set; } = [];
    public List<NameAmountPoint> ByCustomerChart { get; set; } = [];
}

public class CompanyFeeRow
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal NetAmount { get; set; }
    public decimal FeePercent { get; set; }
    public decimal FeeAmount { get; set; }
    public string PlanNumber { get; set; } = string.Empty;
}

public class InstallmentScheduleReportResult
{
    public decimal TotalAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalRemaining { get; set; }
    public int InstallmentCount { get; set; }
    public List<InstallmentScheduleReportRow> Rows { get; set; } = [];
    public List<DailyAmountPoint> DueChart { get; set; } = [];
    public List<NameAmountPoint> StatusChart { get; set; } = [];
}

public class InstallmentScheduleReportRow
{
    public int InstallmentId { get; set; }
    public int PlanId { get; set; }
    public int InvoiceId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PaymentDate { get; set; }
}

public class SalesByPaymentMethodReportResult
{
    public decimal TotalSales { get; set; }
    public decimal CashSales { get; set; }
    public decimal CreditSales { get; set; }
    public decimal InstallmentSales { get; set; }
    public int InvoiceCount { get; set; }
    public List<SalesByPaymentMethodRow> Rows { get; set; } = [];
    public List<NameAmountPoint> MethodChart { get; set; } = [];
    public List<DailyAmountPoint> DailyChart { get; set; } = [];
}

public class SalesByPaymentMethodRow
{
    public string PaymentMethod { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal Amount { get; set; }
    public decimal SharePercent { get; set; }
}

public class DailySalesReportResult
{
    public decimal TotalSales { get; set; }
    public int DayCount { get; set; }
    public int InvoiceCount { get; set; }
    public decimal AverageDaily { get; set; }
    public List<DailySalesRow> Rows { get; set; } = [];
    public List<DailyAmountPoint> DailyChart { get; set; } = [];
}

public class DailySalesRow
{
    public DateTime Date { get; set; }
    public int InvoiceCount { get; set; }
    public decimal CashSales { get; set; }
    public decimal CreditSales { get; set; }
    public decimal InstallmentSales { get; set; }
    public decimal TotalSales { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal CompanyFees { get; set; }
}

public class SalesByWarehouseUserReportResult
{
    public decimal TotalSales { get; set; }
    public int WarehouseCount { get; set; }
    public int UserCount { get; set; }
    public int InvoiceCount { get; set; }
    public List<SalesByWarehouseUserRow> Rows { get; set; } = [];
    public List<NameAmountPoint> WarehouseChart { get; set; } = [];
    public List<NameAmountPoint> UserChart { get; set; } = [];
}

public class SalesByWarehouseUserRow
{
    public string GroupType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal Amount { get; set; }
    public decimal SharePercent { get; set; }
}

public class GrossProfitMarginReportResult
{
    public decimal TotalSales { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossMarginPercent { get; set; }
    public List<GrossProfitMarginRow> Rows { get; set; } = [];
    public List<DailyAmountPoint> DailySalesChart { get; set; } = [];
    public List<DailyAmountPoint> DailyGrossChart { get; set; } = [];
    public List<NameAmountPoint> CompositionChart { get; set; } = [];
}

public class GrossProfitMarginRow
{
    public DateTime Date { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal MarginPercent { get; set; }
}

public class OperatingProfitReportResult
{
    public decimal TotalSales { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalBankFees { get; set; }
    public decimal OperatingProfit { get; set; }
    public decimal OperatingMarginPercent { get; set; }
    public List<OperatingProfitLineRow> Lines { get; set; } = [];
    public List<NameAmountPoint> CompositionChart { get; set; } = [];
    public List<DailyAmountPoint> DailyChart { get; set; } = [];
}

public class OperatingProfitLineRow
{
    public string LineName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsSubtotal { get; set; }
}

public class ReceivablesAgingReportResult
{
    public decimal TotalOutstanding { get; set; }
    public int RowCount { get; set; }
    public int CustomerCount { get; set; }
    public List<AgingBucketSummary> Buckets { get; set; } = [];
    public List<ReceivablesAgingRow> Rows { get; set; } = [];
}

public class AgingBucketSummary
{
    public string BucketName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

public class ReceivablesAgingRow
{
    public string SourceType { get; set; } = string.Empty;
    public int ReferenceId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int DaysOverdue { get; set; }
    public string AgingBucket { get; set; } = string.Empty;
}

public class PayablesAgingReportResult
{
    public decimal TotalOutstanding { get; set; }
    public int RowCount { get; set; }
    public int SupplierCount { get; set; }
    public List<AgingBucketSummary> Buckets { get; set; } = [];
    public List<PayablesAgingRow> Rows { get; set; } = [];
}

public class PayablesAgingRow
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int DaysOverdue { get; set; }
    public string AgingBucket { get; set; } = string.Empty;
}

public class CustomerCollectionsReportResult
{
    public decimal TotalCollected { get; set; }
    public decimal VoucherCollections { get; set; }
    public decimal InstallmentCollections { get; set; }
    public int RowCount { get; set; }
    public List<CustomerCollectionRow> Rows { get; set; } = [];
    public List<DailyAmountPoint> DailyChart { get; set; } = [];
    public List<NameAmountPoint> ByCustomerChart { get; set; } = [];
}

public class CustomerCollectionRow
{
    public DateTime Date { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class OverdueCustomersReportResult
{
    public decimal TotalOverdue { get; set; }
    public int CustomerCount { get; set; }
    public int ItemCount { get; set; }
    public decimal AverageDaysOverdue { get; set; }
    public List<OverdueCustomerRow> Rows { get; set; } = [];
    public List<NameAmountPoint> ByCustomerChart { get; set; } = [];
    public List<AgingBucketSummary> Buckets { get; set; } = [];
}

public class OverdueCustomerRow
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public int ReferenceId { get; set; }
    public DateTime DueDate { get; set; }
    public decimal OverdueAmount { get; set; }
    public int DaysOverdue { get; set; }
}

public class SupplierPaymentsReportResult
{
    public decimal TotalPaid { get; set; }
    public decimal VoucherPayments { get; set; }
    public decimal CashPurchases { get; set; }
    public int RowCount { get; set; }
    public List<SupplierPaymentRow> Rows { get; set; } = [];
    public List<DailyAmountPoint> DailyChart { get; set; } = [];
    public List<NameAmountPoint> BySupplierChart { get; set; } = [];
}

public class SupplierPaymentRow
{
    public DateTime Date { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class BankAccountStatementReportResult
{
    public decimal OpeningBalance { get; set; }
    public decimal TotalIn { get; set; }
    public decimal TotalOut { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<BankAccountStatementRow> Rows { get; set; } = [];
    public List<DailyAmountPoint> DailyInChart { get; set; } = [];
    public List<DailyAmountPoint> DailyOutChart { get; set; } = [];
}

public class BankAccountStatementRow
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Incoming { get; set; }
    public decimal Outgoing { get; set; }
    public decimal Balance { get; set; }
    public string AccountName { get; set; } = string.Empty;
}

public class CashBoxMovementReportResult
{
    public decimal OpeningBalance { get; set; }
    public decimal TotalIncoming { get; set; }
    public decimal TotalOutgoing { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<CashBoxMovementRow> Rows { get; set; } = [];
    public List<DailyAmountPoint> DailyIncomingChart { get; set; } = [];
    public List<DailyAmountPoint> DailyOutgoingChart { get; set; } = [];
}

public class CashBoxMovementRow
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Incoming { get; set; }
    public decimal Outgoing { get; set; }
    public decimal Balance { get; set; }
    public string AccountName { get; set; } = string.Empty;
}

public class CashBalancesSummaryReportResult
{
    public decimal CashBoxesTotal { get; set; }
    public decimal BanksTotal { get; set; }
    public decimal TotalLiquid { get; set; }
    public int AccountCount { get; set; }
    public List<CashBalanceRow> Rows { get; set; } = [];
    public List<NameAmountPoint> CompositionChart { get; set; } = [];
}

public class CashBalanceRow
{
    public string AccountType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}

public class TransfersReportResult
{
    public decimal TotalAmount { get; set; }
    public int TransferCount { get; set; }
    public decimal AverageAmount { get; set; }
    public List<TransferReportRow> Rows { get; set; } = [];
    public List<DailyAmountPoint> DailyChart { get; set; } = [];
    public List<NameAmountPoint> ByTypeChart { get; set; } = [];
}

public class TransferReportRow
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string FromAccount { get; set; } = string.Empty;
    public string ToAccount { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}

public class InventoryValuationReportResult
{
    public decimal TotalValue { get; set; }
    public decimal TotalQuantity { get; set; }
    public int ProductCount { get; set; }
    public int WarehouseCount { get; set; }
    public List<InventoryValuationRow> Rows { get; set; } = [];
    public List<NameAmountPoint> WarehouseChart { get; set; } = [];
    public List<NameAmountPoint> TopProductsChart { get; set; } = [];
}

public class InventoryValuationRow
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public decimal TotalValue { get; set; }
}

public class StockTakingReportResult
{
    public decimal TotalQuantity { get; set; }
    public int ProductCount { get; set; }
    public int WarehouseCount { get; set; }
    public List<StockTakingRow> Rows { get; set; } = [];
    public List<NameAmountPoint> WarehouseChart { get; set; } = [];
}

public class StockTakingRow
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal SystemQuantity { get; set; }
    public decimal? CountedQuantity { get; set; }
    public decimal Difference => (CountedQuantity ?? SystemQuantity) - SystemQuantity;
}

public class CogsReportResult
{
    public decimal TotalCogs { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal GrossProfit { get; set; }
    public int ProductCount { get; set; }
    public List<CogsReportRow> Rows { get; set; } = [];
    public List<NameAmountPoint> TopProductsChart { get; set; } = [];
    public List<DailyAmountPoint> DailyChart { get; set; } = [];
}

public class CogsReportRow
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public decimal AverageCost { get; set; }
    public decimal CogsAmount { get; set; }
    public decimal Revenue { get; set; }
    public decimal GrossProfit { get; set; }
}

public class FinancialPositionSummaryReportResult
{
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public decimal NetWorkingCapital { get; set; }
    public decimal Difference { get; set; }
    public bool IsBalanced { get; set; }
    public List<FinancialPositionLineRow> Rows { get; set; } = [];
    public List<NameAmountPoint> CompositionChart { get; set; } = [];
}

public class FinancialPositionLineRow
{
    public string Section { get; set; } = string.Empty;
    public string LineName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class ProfitAndLossReportResult
{
    public decimal TotalSales { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalBankFees { get; set; }
    public decimal OperatingProfit { get; set; }
    public decimal DistributedProfits { get; set; }
    public decimal NetProfit { get; set; }
    public decimal GrossMarginPercent { get; set; }
    public decimal NetMarginPercent { get; set; }
    public List<ProfitAndLossLineRow> Lines { get; set; } = [];
    public List<NameAmountPoint> CompositionChart { get; set; } = [];
    public List<DailyAmountPoint> MonthlyChart { get; set; } = [];
}

public class ProfitAndLossLineRow
{
    public string LineName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsSubtotal { get; set; }
    public bool IsTotal { get; set; }
}

public class StatementOfFinancialPositionReportResult
{
    public decimal CashAndBanks { get; set; }
    public decimal Receivables { get; set; }
    public decimal InstallmentReceivables { get; set; }
    public decimal Inventory { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal Payables { get; set; }
    public decimal InvestorCapital { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal Capital { get; set; }
    public decimal Adjustments { get; set; }
    public decimal AccumulatedProfits { get; set; }
    public decimal TotalEquity { get; set; }
    public decimal Difference { get; set; }
    public bool IsBalanced { get; set; }
    public List<StatementOfFinancialPositionLineRow> Rows { get; set; } = [];
    public List<NameAmountPoint> AssetsChart { get; set; } = [];
    public List<NameAmountPoint> EquityLiabilitiesChart { get; set; } = [];
}

public class StatementOfFinancialPositionLineRow
{
    public string Section { get; set; } = string.Empty;
    public string LineName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsTotal { get; set; }
}

public class WorkSummaryReportResult
{
    public int NewCustomersCount { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public int DealCount { get; set; }
    public int DistinctProductCount { get; set; }
    public decimal TotalProductQuantity { get; set; }

    public List<NameAmountPoint> SalesByYearChart { get; set; } = [];
    public List<NameAmountPoint> TopCustomersChart { get; set; } = [];
    public List<NameAmountPoint> BusiestHoursChart { get; set; } = [];
    public List<WorkSummaryHourRow> HourRows { get; set; } = [];
}

public class WorkSummaryHourRow
{
    public int Hour { get; set; }
    public string HourLabel { get; set; } = string.Empty;
    public int ActivityCount { get; set; }
    public decimal SalesAmount { get; set; }
}
