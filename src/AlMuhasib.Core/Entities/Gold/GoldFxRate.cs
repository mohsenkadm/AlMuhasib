namespace AlMuhasib.Core.Entities.Gold;

public class GoldFxRate : BaseEntity
{
    public DateTime RateDate { get; set; } = DateTime.Today;
    public decimal UsdToIqd { get; set; }
    public string Notes { get; set; } = string.Empty;
}
