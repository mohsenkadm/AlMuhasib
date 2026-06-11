namespace AlMuhasib.Core.Models;

public class CollectionDashboardSummary
{
    public int DueTodayCount { get; set; }
    public decimal DueTodayAmount { get; set; }
    public int OverdueCount { get; set; }
    public decimal OverdueAmount { get; set; }
    public int ThisWeekCount { get; set; }
    public decimal ThisWeekAmount { get; set; }
    public List<CollectionInstallmentRow> Rows { get; set; } = [];
}

public class CollectionInstallmentRow
{
    public int InstallmentId { get; set; }
    public int PlanId { get; set; }
    public int? InvoiceId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public DateTime DueDate { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Bucket { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
}
