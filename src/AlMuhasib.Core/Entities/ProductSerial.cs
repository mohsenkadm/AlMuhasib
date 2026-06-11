namespace AlMuhasib.Core.Entities;

/// <summary>رقم تسلسلي للمنتج (IMEI/SN).</summary>
public class ProductSerial : BaseEntity
{
    public int ProductId { get; set; }
    public int? WarehouseId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public bool IsSold { get; set; }
    public int? InvoiceItemId { get; set; }

    public Product Product { get; set; } = null!;
    public Warehouse? Warehouse { get; set; }
}
