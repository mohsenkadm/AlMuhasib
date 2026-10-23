using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;

namespace AlMuhasib.Core.Models.Customs;

public sealed class CustomsReportNameAmountPoint
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class CustomsReportCustomerOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

// ── Daily ──────────────────────────────────────────────────

public sealed class CustomsDailyReportResult
{
    public int InvoiceCount { get; set; }
    public decimal InvoicesIqd { get; set; }
    public decimal InvoicesUsd { get; set; }
    public decimal ReceiptsIqd { get; set; }
    public decimal ReceiptsUsd { get; set; }
    public decimal ExpensesIqd { get; set; }
    public decimal ExpensesUsd { get; set; }
    public int VehicleCount { get; set; }
    public List<CustomsReportNameAmountPoint> ByTypeChart { get; set; } = [];
    public List<CustomsDailyReportRow> Rows { get; set; } = [];
}

public sealed class CustomsDailyReportRow
{
    public DateTime Date { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}

// ── Monthly ────────────────────────────────────────────────

public sealed class CustomsMonthlyReportResult
{
    public decimal InvoicesIqd { get; set; }
    public decimal InvoicesUsd { get; set; }
    public decimal ReceiptsIqd { get; set; }
    public decimal ReceiptsUsd { get; set; }
    public decimal ExpensesIqd { get; set; }
    public decimal ExpensesUsd { get; set; }
    public decimal ProfitIqd { get; set; }
    public decimal ProfitUsd { get; set; }
    public List<CustomsReportNameAmountPoint> DailyChart { get; set; } = [];
    public List<CustomsMonthlyReportRow> Rows { get; set; } = [];
}

public sealed class CustomsMonthlyReportRow
{
    public DateTime Date { get; set; }
    public int InvoiceCount { get; set; }
    public decimal InvoicesIqd { get; set; }
    public decimal InvoicesUsd { get; set; }
    public decimal ReceiptsIqd { get; set; }
    public decimal ReceiptsUsd { get; set; }
    public decimal ExpensesIqd { get; set; }
    public decimal ExpensesUsd { get; set; }
    public decimal ProfitIqd { get; set; }
    public decimal ProfitUsd { get; set; }
}

// ── Customer ───────────────────────────────────────────────

public sealed class CustomsCustomerReportResult
{
    public int CustomerCount { get; set; }
    public decimal BalanceIqd { get; set; }
    public decimal BalanceUsd { get; set; }
    public int InvoiceCount { get; set; }
    public List<CustomsReportNameAmountPoint> TopCustomersChart { get; set; } = [];
    public List<CustomsCustomerReportRow> Rows { get; set; } = [];
}

public sealed class CustomsCustomerReportRow
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal BalanceIqd { get; set; }
    public decimal BalanceUsd { get; set; }
    public int InvoiceCount { get; set; }
    public decimal RevenueIqd { get; set; }
    public decimal RevenueUsd { get; set; }
    public decimal PaidIqd { get; set; }
    public decimal PaidUsd { get; set; }
    public decimal RemainingIqd { get; set; }
    public decimal RemainingUsd { get; set; }
}

// ── Invoice ────────────────────────────────────────────────

public sealed class CustomsInvoiceReportResult
{
    public int Count { get; set; }
    public decimal TotalIqd { get; set; }
    public decimal TotalUsd { get; set; }
    public decimal PaidIqd { get; set; }
    public decimal PaidUsd { get; set; }
    public decimal RemainingIqd { get; set; }
    public decimal RemainingUsd { get; set; }
    public List<CustomsReportNameAmountPoint> StatusChart { get; set; } = [];
    public List<CustomsInvoiceReportRow> Rows { get; set; } = [];
}

public sealed class CustomsInvoiceReportRow
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string ServiceTypeLabel { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
}

// ── Vehicles ───────────────────────────────────────────────

public sealed class CustomsVehiclesReportResult
{
    public int Total { get; set; }
    public int InProgress { get; set; }
    public int Exited { get; set; }
    public int Arrived { get; set; }
    public List<CustomsReportNameAmountPoint> StatusChart { get; set; } = [];
    public List<CustomsVehiclesReportRow> Rows { get; set; } = [];
}

public sealed class CustomsVehiclesReportRow
{
    public string IraqiPlate { get; set; } = string.Empty;
    public string SyrianPlate { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public DateTime? LoadingDate { get; set; }
    public DateTime? ExitDate { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal VehiclePrice { get; set; }
}

// ── Debts ──────────────────────────────────────────────────

public sealed class CustomsDebtsReportResult
{
    public decimal DebtsIqd { get; set; }
    public decimal DebtsUsd { get; set; }
    public int UnpaidCount { get; set; }
    public List<CustomsReportNameAmountPoint> AgingChart { get; set; } = [];
    public List<CustomsDebtsReportRow> Rows { get; set; } = [];
}

public sealed class CustomsDebtsReportRow
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal RemainingAmount { get; set; }
    public int DaysOverdue { get; set; }
    public string AgingBucket { get; set; } = string.Empty;
}

// ── Profit ─────────────────────────────────────────────────

public sealed class CustomsProfitReportResult
{
    public decimal RevenueIqd { get; set; }
    public decimal RevenueUsd { get; set; }
    public decimal ExpensesIqd { get; set; }
    public decimal ExpensesUsd { get; set; }
    public decimal ProfitIqd { get; set; }
    public decimal ProfitUsd { get; set; }
    public List<CustomsReportNameAmountPoint> MonthlyChart { get; set; } = [];
    public List<CustomsProfitReportRow> Rows { get; set; } = [];
}

public sealed class CustomsProfitReportRow
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal Profit { get; set; }
}
