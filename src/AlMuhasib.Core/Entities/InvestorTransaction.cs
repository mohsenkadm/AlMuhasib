using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>حركات المستثمر</summary>
public class InvestorTransaction : BaseEntity
{
    public int InvestorId { get; set; }
    public InvestorTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Investor Investor { get; set; } = null!;
}
