namespace AlMuhasib.Core.Entities.Gold;

public class GoldWarehouseTransfer : BaseEntity
{
    public DateTime TransferDate { get; set; } = DateTime.Today;
    public int FromWarehouseId { get; set; }
    public GoldWarehouse? FromWarehouse { get; set; }
    public int ToWarehouseId { get; set; }
    public GoldWarehouse? ToWarehouse { get; set; }
    public int KaratValue { get; set; }
    public decimal WeightGrams { get; set; }
    public string Notes { get; set; } = string.Empty;
}
