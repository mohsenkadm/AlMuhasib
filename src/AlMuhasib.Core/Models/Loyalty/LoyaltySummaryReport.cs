namespace AlMuhasib.Core.Models.Loyalty;

public sealed class LoyaltySummaryReport
{
    public int TotalEarnedPoints { get; init; }
    public int TotalRedeemedPoints { get; init; }
    public int TotalAdjustedPoints { get; init; }
    public int TotalExpiredPoints { get; init; }
    public decimal TotalRedeemDiscountValue { get; init; }
    public int ActiveCustomersCount { get; init; }
    public int TransactionsCount { get; init; }
}

public sealed class LoyaltyTopCustomerRow
{
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public int PointsBalance { get; init; }
    public int LifetimeEarned { get; init; }
    public int LifetimeRedeemed { get; init; }
    public string TierName { get; init; } = string.Empty;
}

public sealed class LoyaltyAccountRow
{
    public int AccountId { get; init; }
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public int PointsBalance { get; init; }
    public int LifetimeEarned { get; init; }
    public int LifetimeRedeemed { get; init; }
    public string TierName { get; init; } = string.Empty;
    public DateTime? LastEarnedAt { get; init; }
    public DateTime? LastRedeemedAt { get; init; }
}
