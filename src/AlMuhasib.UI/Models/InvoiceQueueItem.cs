namespace AlMuhasib.UI.Models;

public enum InvoiceQueueKind
{
    Sales,
    Purchase,
    Installment
}

public sealed class InvoiceQueueItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public InvoiceQueueKind Kind { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime SavedAt { get; set; } = DateTime.Now;
    public int LineCount { get; set; }
    public decimal TotalAmount { get; set; }

    public string SavedAtText => SavedAt.ToString("yyyy/MM/dd HH:mm");
    public string LineCountText => $"{LineCount:N0} بند";
    public string TotalAmountText => $"{TotalAmount:N0} د.ع";
    public string SummaryText => $"{SavedAtText}  •  {LineCountText}  •  {TotalAmountText}";
}
