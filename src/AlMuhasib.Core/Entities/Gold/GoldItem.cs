using AlMuhasib.Core.Enums.Gold;

namespace AlMuhasib.Core.Entities.Gold;

public class GoldItem : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int KaratValue { get; set; }
    public decimal WeightGrams { get; set; }
    public decimal SuggestedMakingCharge { get; set; }
    public GoldCurrency MakingChargeCurrency { get; set; } = GoldCurrency.IQD;
    public decimal CostPerGram { get; set; }
    public GoldItemStatus Status { get; set; } = GoldItemStatus.InStock;
    public bool TrackAsPiece { get; set; } = true;
}
