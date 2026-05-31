namespace AlMuhasib.UI.Models;

public class PurchaseInvoiceDraft
{
    public DateTime InvoiceDate { get; set; } = DateTime.Now;
    public int? SupplierId { get; set; }
    public int? WarehouseId { get; set; }
    public bool IsCashPayment { get; set; } = true;
    public int? CashBoxId { get; set; }
    public string? Notes { get; set; }
    public List<SalesInvoiceDraftLine> Lines { get; set; } = [];
}
