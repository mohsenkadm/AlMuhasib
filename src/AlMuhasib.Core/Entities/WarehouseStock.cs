namespace AlMuhasib.Core.Entities;

/// <summary>مخزون المخزن</summary>
public class WarehouseStock : BaseEntity
{
    public int WarehouseId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    /// <summary>كمية الرصيد الافتتاحي (لحساب متوسط الكلفة)</summary>
    public decimal OpeningQuantity { get; set; }
    /// <summary>كلفة شراء الوحدة للرصيد الافتتاحي</summary>
    public decimal UnitCost { get; set; }

    // Navigation
    public Warehouse Warehouse { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
