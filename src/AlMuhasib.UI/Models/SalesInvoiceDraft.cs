using AlMuhasib.Core.Enums;

namespace AlMuhasib.UI.Models;

public class SalesInvoiceDraft
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.Now;
    public int? CustomerId { get; set; }
    public int? WarehouseId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public DateTime? CreditDueDate { get; set; }
    public int? CashBoxId { get; set; }
    public string? Notes { get; set; }
    public List<SalesInvoiceDraftLine> Lines { get; set; } = [];
}

public class SalesInvoiceDraftLine
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
