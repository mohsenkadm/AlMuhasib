using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IInvestorService
{
    // ── Investor CRUD ──
    Task<IEnumerable<Investor>> GetAllInvestorsAsync();
    Task<Investor> AddInvestorAsync(string name, string? phone, decimal profitPercentage, string? customFieldsJson = null);
    Task UpdateInvestorAsync(int id, string name, string? phone, decimal profitPercentage, string? customFieldsJson = null);

    /// <summary>حفظ الأرصدة الافتتاحية للمستثمرين (لا تؤثر على القاصة)</summary>
    Task SaveOpeningBalancesAsync(IEnumerable<InvestorOpeningBalanceItem> items);

    // ── Deposit / Withdrawal ──
    Task DepositAsync(int investorId, decimal amount, DateTime date, int cashBoxId, string? notes);
    Task WithdrawAsync(int investorId, decimal amount, DateTime date, int cashBoxId, string? notes);

    // ── Recent transactions ──
    Task<IEnumerable<InvestorTransaction>> GetRecentDepositsAsync(int count = 20);
    Task<IEnumerable<InvestorTransaction>> GetRecentWithdrawalsAsync(int count = 20);

    // ── Profit Distribution ──
    Task<decimal> GetDistributableProfitsAsync();
    Task<decimal> GetEligibleDepositAsync(int investorId, DateTime distributionDate);
    Task<IEnumerable<ProfitPreviewItem>> PreviewProfitDistributionAsync(DateTime distributionDate, decimal totalDistributableProfits);
    Task DistributeProfitsAsync(DateTime distributionDate, int cashBoxId,
        decimal totalDistributableProfits, IEnumerable<ProfitPreviewItem> items);

    // ── Profit Statement ──
    Task<IEnumerable<ProfitDistributionDetail>> GetProfitDetailsForInvestorAsync(int investorId);
    Task<decimal> GetTotalProfitsEarnedAsync(int investorId);
}

/// <summary>Preview row for profit distribution</summary>
public class ProfitPreviewItem
{
    public int InvestorId { get; set; }
    public string InvestorName { get; set; } = string.Empty;
    public string? InvestorPhone { get; set; }
    public decimal TotalDeposit { get; set; }
    public decimal EligibleDeposit { get; set; }
    public decimal ProfitPercentage { get; set; }
    public decimal ProfitAmount { get; set; }
    public bool IsIncluded { get; set; } = true;
}

public class InvestorOpeningBalanceItem
{
    public int InvestorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal ProfitPercentage { get; set; }
    public decimal OpeningBalance { get; set; }
}
