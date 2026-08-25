namespace AlMuhasib.UI.Messages;

public sealed class GoldFxRateChangedMessage
{
    public decimal UsdToIqd { get; init; }
    public DateTime RateDate { get; init; }
}
