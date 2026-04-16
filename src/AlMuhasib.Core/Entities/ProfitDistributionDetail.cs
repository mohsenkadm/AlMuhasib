namespace AlMuhasib.Core.Entities;

/// <summary>تفاصيل توزيع الأرباح لكل مستثمر</summary>
public class ProfitDistributionDetail : BaseEntity
{
    public int ProfitDistributionId { get; set; }
    public int InvestorId { get; set; }
    public decimal ProfitPercentage { get; set; }
    public decimal Amount { get; set; }

    // Navigation
    public ProfitDistribution ProfitDistribution { get; set; } = null!;
    public Investor Investor { get; set; } = null!;
}
