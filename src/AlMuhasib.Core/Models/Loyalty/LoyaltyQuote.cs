namespace AlMuhasib.Core.Models.Loyalty;

public sealed class LoyaltyQuote
{
    public int CustomerId { get; init; }
    public int Balance { get; init; }
    public int ExpectedEarnPoints { get; init; }
    public int MaxRedeemablePoints { get; init; }
    public int RequestedRedeemPoints { get; init; }
    public decimal RedeemDiscount { get; init; }
    public string? Error { get; init; }
    public bool CanRedeem => string.IsNullOrEmpty(Error) && RequestedRedeemPoints > 0 && RedeemDiscount > 0m;
}
