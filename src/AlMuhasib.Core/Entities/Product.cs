namespace AlMuhasib.Core.Entities;

/// <summary>المنتجات</summary>
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public int CategoryId { get; set; }

    // Navigation
    public Category Category { get; set; } = null!;
    public ICollection<WarehouseStock> WarehouseStocks { get; set; } = [];
    public ICollection<InvoiceItem> InvoiceItems { get; set; } = [];
    public ICollection<ProductPrice> ProductPrices { get; set; } = [];
}
