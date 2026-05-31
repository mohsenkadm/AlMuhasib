namespace AlMuhasib.Core.Models;

/// <summary>طلب إنشاء خطة أقساط افتتاحية (رصيد سابق)</summary>
public class OpeningInstallmentBalanceRequest
{
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? FileNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public int NumberOfInstallments { get; set; }
    public int PaidInstallmentsCount { get; set; }
    public DateTime StartDate { get; set; }
    public string? Notes { get; set; }
}
