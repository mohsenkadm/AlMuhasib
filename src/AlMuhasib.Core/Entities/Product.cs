namespace AlMuhasib.Core.Entities;

/// <summary>المنتجات</summary>
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public int CategoryId { get; set; }

    /// <summary>وزن/حجم الوحدة الأساسية للمنتج (0 = غير محدد).</summary>
    public decimal Weight { get; set; }

    /// <summary>وحدة الوزن (كغ، غرام، لتر، مل، …).</summary>
    public string? WeightUnit { get; set; }

    // Navigation
    public Category Category { get; set; } = null!;
    public ICollection<WarehouseStock> WarehouseStocks { get; set; } = [];
    public ICollection<InvoiceItem> InvoiceItems { get; set; } = [];
    public ICollection<ProductPrice> ProductPrices { get; set; } = [];
}
