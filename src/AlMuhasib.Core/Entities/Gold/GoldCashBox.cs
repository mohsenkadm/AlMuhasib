using AlMuhasib.Core.Enums.Gold;

namespace AlMuhasib.Core.Entities.Gold;

public class GoldCashBox : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public GoldCurrency Currency { get; set; } = GoldCurrency.IQD;
    public decimal Balance { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}
