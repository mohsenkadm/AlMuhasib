using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>حركة نقاط ولاء (كسب / استبدال / تعديل / انتهاء).</summary>
public class LoyaltyPointTransaction : BaseEntity
{
    public int CustomerId { get; set; }
    public int? InvoiceId { get; set; }
    public LoyaltyTransactionType Type { get; set; }
    public int Points { get; set; }
    public decimal UnitValue { get; set; }
    public decimal CurrencyAmount { get; set; }
    public int BalanceAfter { get; set; }
    public string? Note { get; set; }
    public int? CreatedByUserId { get; set; }

    public Customer Customer { get; set; } = null!;
    public Invoice? Invoice { get; set; }
}
