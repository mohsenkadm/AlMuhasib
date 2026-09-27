namespace AlMuhasib.Core.Entities;

/// <summary>توزيع الأرباح</summary>
public class ProfitDistribution : BranchScopedEntity
{
    public DateTime Date { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal DistributedAmount { get; set; }

    // Navigation
    public ICollection<ProfitDistributionDetail> Details { get; set; } = [];
}
