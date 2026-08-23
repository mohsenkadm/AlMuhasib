using AlMuhasib.Core.Enums;

namespace AlMuhasib.UI.Models;

public class SalesInvoiceDraft
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.Now;
    public int? CustomerId { get; set; }
    public int? DriverId { get; set; }
    public int? SalesRepresentativeId { get; set; }
    public int? WarehouseId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public DateTime? CreditDueDate { get; set; }
    public int? CashBoxId { get; set; }
    public string? Notes { get; set; }
    public decimal PaidAmount { get; set; }
    public List<SalesInvoiceDraftLine> Lines { get; set; } = [];
}

public class SalesInvoiceDraftLine
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public int? WarehouseId { get; set; }
    public int? PricingTypeId { get; set; }
    public string PricingTypeName { get; set; } = string.Empty;
    public string SelectedUnitName { get; set; } = string.Empty;
    public decimal UnitConversionFactor { get; set; } = 1m;
    public string? CustomField1 { get; set; }
    public string? CustomField2 { get; set; }
    public string? CustomField1Label { get; set; }
    public string? CustomField2Label { get; set; }
    public string? SizeName { get; set; }
    public int? ProductSizeId { get; set; }
    public string? ColorName { get; set; }
    public int? ProductColorId { get; set; }
    public string? SerialNumber { get; set; }
    public string? BatchNumber { get; set; }
    public int? BatchId { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
