using AlMuhasib.Core.Enums.Gold;

namespace AlMuhasib.Core.Entities.Gold;

public class GoldMithqalPrice : BaseEntity
{
    public DateTime PriceDate { get; set; } = DateTime.Today;
    public int KaratValue { get; set; }
    public decimal PricePerMithqal { get; set; }
    public GoldCurrency Currency { get; set; } = GoldCurrency.USD;
    public decimal? FxRateUsed { get; set; }
    public string Notes { get; set; } = string.Empty;
}
