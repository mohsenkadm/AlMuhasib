namespace AlMuhasib.Core.Entities.Gold;

public class GoldStockBalance : BaseEntity
{
    public int WarehouseId { get; set; }
    public GoldWarehouse? Warehouse { get; set; }
    public int KaratValue { get; set; }
    public decimal GramsOnHand { get; set; }
    public decimal AverageCostPerGram { get; set; }
}
