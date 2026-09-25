using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Models;

public class DashboardData
{
    // Summary cards
    public decimal TodaySales { get; set; }
    public decimal TodayPurchases { get; set; }
    public decimal NetProfit { get; set; }
    public int OverdueInstallmentsCount { get; set; }

    /// <summary>مكونات معادلة الأرباح الصافية في لوحة التحكم.</summary>
    public decimal NetProfitSales { get; set; }
    public decimal NetProfitPurchases { get; set; }
    /// <summary>قيمة أرصدة المنتجات الافتتاحية (كمية افتتاحية × تكلفة الوحدة).</summary>
    public decimal NetProfitOpeningStock { get; set; }
    public decimal NetProfitExpenses { get; set; }
    public decimal NetProfitDistributions { get; set; }
    public decimal NetProfitOpening { get; set; }

    // Charts
    public List<DailySalesPoint> SalesLast30Days { get; set; } = [];
    public List<ExpenseCategoryShare> ExpenseDistribution { get; set; } = [];

    // Tables
    public List<RecentTransaction> RecentTransactions { get; set; } = [];
    public List<UpcomingInstallment> UpcomingInstallments { get; set; } = [];

    // Additional statistics
    public decimal InvestorBalance { get; set; }
    public decimal InvestorOpeningTotal { get; set; }
    public decimal InvestorDepositsTotal { get; set; }
    public decimal InvestorWithdrawalsTotal { get; set; }
    public decimal UnpaidInstallmentsBalance { get; set; }
    public decimal CustomerCreditBalance { get; set; }
    public decimal CustomerCreditInvoiceRemaining { get; set; }
    public decimal CustomerCreditUnappliedDebt { get; set; }
    public decimal CustomerCreditUnappliedReceipts { get; set; }

    public decimal SupplierCreditBalance { get; set; }
    public decimal SupplierCreditInvoiceRemaining { get; set; }
    public decimal SupplierCreditUnappliedPayments { get; set; }

    // Bottom row
    public List<CashBoxSummary> CashBoxes { get; set; } = [];
    public decimal BankBalance { get; set; }
    public decimal CashBalanceIqd { get; set; }
    public decimal CashBalanceUsd { get; set; }
    public decimal BankBalanceIqd { get; set; }
    public decimal BankBalanceUsd { get; set; }
    public decimal TotalInventoryValue { get; set; }

    // KPI mini-chart trends (last 14 days) + period-over-period %
    public List<DailySalesPoint> PurchasesLast14Days { get; set; } = [];
    public List<DailySalesPoint> NetProfitLast14Days { get; set; } = [];
    public List<DailySalesPoint> OverdueInstallmentsLast14Days { get; set; } = [];
    public List<DailySalesPoint> InvestorBalanceLast14Days { get; set; } = [];
    public List<DailySalesPoint> UnpaidInstallmentsLast14Days { get; set; } = [];
    public List<DailySalesPoint> CustomerCreditLast14Days { get; set; } = [];
    public List<DailySalesPoint> SupplierCreditLast14Days { get; set; } = [];
    public List<DailySalesPoint> CashFlowLast14Days { get; set; } = [];
    public List<DailySalesPoint> BankFlowLast14Days { get; set; } = [];
    public List<DailySalesPoint> InventoryValueLast14Days { get; set; } = [];

    public decimal TodaySalesTrendPercent { get; set; }
    public decimal TodayPurchasesTrendPercent { get; set; }
    public decimal NetProfitTrendPercent { get; set; }
    public decimal OverdueInstallmentsTrendPercent { get; set; }
    public decimal InvestorBalanceTrendPercent { get; set; }
    public decimal UnpaidInstallmentsTrendPercent { get; set; }
    public decimal CustomerCreditTrendPercent { get; set; }
    public decimal SupplierCreditTrendPercent { get; set; }
    public decimal CashBalanceTrendPercent { get; set; }
    public decimal BankBalanceTrendPercent { get; set; }
    public decimal InventoryValueTrendPercent { get; set; }
}

public class DailySalesPoint
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
}

public class ExpenseCategoryShare
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class RecentTransaction
{
    public string Type { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string Party { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
}

public class UpcomingInstallment
{
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerFileNumber { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public int DaysRemaining { get; set; }
}

public class CashBoxSummary
{
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;
}
