namespace AlMuhasib.Core.Entities;

/// <summary>مخزون قياس منتج في مخزن معيّن — يُفعّل مع ميزة محلات الألبسة فقط.</summary>
public class ProductSizeStock : BaseEntity
{
    public int ProductId { get; set; }
    public int ProductSizeId { get; set; }
    public int WarehouseId { get; set; }
    public decimal Quantity { get; set; }

    public Product Product { get; set; } = null!;
    public ProductSize ProductSize { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
}
