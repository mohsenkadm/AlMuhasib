namespace AlMuhasib.Core.Entities;

/// <summary>مخزون المخزن</summary>
public class WarehouseStock : BaseEntity
{
    public int WarehouseId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }

    // Navigation
    public Warehouse Warehouse { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
