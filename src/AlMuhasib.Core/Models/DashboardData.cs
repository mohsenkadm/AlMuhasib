namespace AlMuhasib.Core.Models;

public class DashboardData
{
    // Summary cards
    public decimal TodaySales { get; set; }
    public decimal TodayPurchases { get; set; }
    public decimal NetProfit { get; set; }
    public int OverdueInstallmentsCount { get; set; }

    // Charts
    public List<DailySalesPoint> SalesLast30Days { get; set; } = [];
    public List<ExpenseCategoryShare> ExpenseDistribution { get; set; } = [];

    // Tables
    public List<RecentTransaction> RecentTransactions { get; set; } = [];
    public List<UpcomingInstallment> UpcomingInstallments { get; set; } = [];

    // Additional statistics
    public decimal InvestorBalance { get; set; }
    public decimal UnpaidInstallmentsBalance { get; set; }
    public decimal CustomerCreditBalance { get; set; }

    // Bottom row
    public List<CashBoxSummary> CashBoxes { get; set; } = [];
    public decimal BankBalance { get; set; }
    public decimal TotalInventoryValue { get; set; }
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
}
