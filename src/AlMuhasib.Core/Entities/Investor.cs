namespace AlMuhasib.Core.Entities;

/// <summary>المستثمرون</summary>
public class Investor : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal TotalDeposit { get; set; }
    /// <summary>رصيد افتتاحي للمستثمر — لا يُضاف إلى القاصة</summary>
    public decimal OpeningBalance { get; set; }
    public decimal ProfitPercentage { get; set; }

    // Navigation
    public ICollection<InvestorTransaction> Transactions { get; set; } = [];
    public ICollection<Voucher> Vouchers { get; set; } = [];
    public ICollection<ProfitDistributionDetail> ProfitDistributionDetails { get; set; } = [];
}
