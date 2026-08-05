using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>حساب نقاط ولاء مرتبط بزبون.</summary>
public class CustomerLoyaltyAccount : BaseEntity
{
    public int CustomerId { get; set; }
    public int PointsBalance { get; set; }
    public int LifetimeEarned { get; set; }
    public int LifetimeRedeemed { get; set; }
    public LoyaltyTier Tier { get; set; } = LoyaltyTier.Standard;
    public DateTime? LastEarnedAt { get; set; }
    public DateTime? LastRedeemedAt { get; set; }

    public Customer Customer { get; set; } = null!;
}
