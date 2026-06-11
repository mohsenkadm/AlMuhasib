namespace AlMuhasib.Core.Entities;

/// <summary>نقل بين مخازن</summary>
public class WarehouseTransfer : BaseEntity
{
    public string TransferNumber { get; set; } = string.Empty;
    public int FromWarehouseId { get; set; }
    public int ToWarehouseId { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }

    public Warehouse FromWarehouse { get; set; } = null!;
    public Warehouse ToWarehouse { get; set; } = null!;
    public ICollection<WarehouseTransferItem> Items { get; set; } = [];
}

public class WarehouseTransferItem : BaseEntity
{
    public int WarehouseTransferId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }

    public WarehouseTransfer WarehouseTransfer { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
