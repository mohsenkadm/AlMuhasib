namespace AlMuhasib.Core.Entities;

/// <summary>دفعة منتج مع تاريخ صلاحية.</summary>
public class ProductBatch : BaseEntity
{
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal Quantity { get; set; }

    public Product Product { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
}
