namespace AlMuhasib.Core.Models.Customs;

public sealed class CustomsDashboardNameCountPoint
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class CustomsDashboardStats
{
    public int CustomerCount { get; set; }
    public int InvoiceCount { get; set; }
    public int VehicleCount { get; set; }
    public int VehiclesInProgress { get; set; }
    public int VehiclesExited { get; set; }
    public int VehiclesArrived { get; set; }
    public int UnpaidInvoiceCount { get; set; }

    public decimal ReceiptsIqd { get; set; }
    public decimal ReceiptsUsd { get; set; }
    public decimal ExpensesIqd { get; set; }
    public decimal ExpensesUsd { get; set; }
    public decimal DebtsIqd { get; set; }
    public decimal DebtsUsd { get; set; }
    public decimal ProfitIqd { get; set; }
    public decimal ProfitUsd { get; set; }

    public List<CustomsDashboardNameCountPoint> MonthlyInvoices { get; set; } = [];
    public List<CustomsDashboardNameCountPoint> VehicleStatusChart { get; set; } = [];
}
