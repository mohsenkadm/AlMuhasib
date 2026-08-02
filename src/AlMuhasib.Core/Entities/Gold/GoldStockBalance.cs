namespace AlMuhasib.Core.Entities.Gold;

public class GoldStockBalance : BaseEntity
{
    public int KaratValue { get; set; }
    public decimal GramsOnHand { get; set; }
    public decimal AverageCostPerGram { get; set; }
}
