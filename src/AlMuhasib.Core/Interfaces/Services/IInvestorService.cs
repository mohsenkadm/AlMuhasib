using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IInvestorService
{
    // ── Investor CRUD ──
    Task<IEnumerable<Investor>> GetAllInvestorsAsync();
    Task<Investor> AddInvestorAsync(string name, string? phone, decimal profitPercentage, string? customFieldsJson = null);
    Task UpdateInvestorAsync(int id, string name, string? phone, decimal profitPercentage, string? customFieldsJson = null);

    /// <summary>
    /// حذف ناعم للمستثمر. يُرفض إن بقي رصيد إيداع؛ السجل التاريخي يبقى محفوظاً.
    /// </summary>
    Task DeleteInvestorAsync(int id);

    /// <summary>حفظ الأرصدة الافتتاحية للمستثمرين (لا تؤثر على القاصة)</summary>
    Task SaveOpeningBalancesAsync(IEnumerable<InvestorOpeningBalanceItem> items);

    // ── Deposit / Withdrawal ──
    Task DepositAsync(int investorId, decimal amount, DateTime date, int cashBoxId, string? notes);
    Task WithdrawAsync(int investorId, decimal amount, DateTime date, int cashBoxId, string? notes);

    // ── Recent transactions ──
    Task<IEnumerable<InvestorTransaction>> GetRecentDepositsAsync(int count = 20);
    Task<IEnumerable<InvestorTransaction>> GetRecentWithdrawalsAsync(int count = 20);

    // ── Profit Distribution ──
    /// <summary>
    /// الربح الحقيقي للشهر: مبيعات − تكلفة البضاعة المباعة − المصاريف (− توزيعات نفس الشهر).
    /// </summary>
    Task<DistributableProfitBreakdown> GetDistributableProfitsAsync(DateTime periodDate);
    Task<decimal> GetEligibleDepositAsync(int investorId, DateTime distributionDate, int eligibilityDays = 15);
    Task<IEnumerable<ProfitPreviewItem>> PreviewProfitDistributionAsync(DateTime distributionDate, decimal totalDistributableProfits, int eligibilityDays = 15);
    Task DistributeProfitsAsync(DateTime distributionDate, int cashBoxId,
        decimal totalDistributableProfits, IEnumerable<ProfitPreviewItem> items);

    // ── Profit Statement ──
    Task<IEnumerable<ProfitDistributionDetail>> GetProfitDetailsForInvestorAsync(int investorId);
    Task<decimal> GetTotalProfitsEarnedAsync(int investorId);
}

/// <summary>معاينة صف توزيع الأرباح</summary>
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

/// <summary>تفصيل الربح الحقيقي لشهر التوزيع.</summary>
public class DistributableProfitBreakdown
{
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodToExclusive { get; set; }
    public decimal Sales { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal Expenses { get; set; }
    public decimal AlreadyDistributedInPeriod { get; set; }

    /// <summary>مبيعات − تكلفة البضاعة = إجمالي الربح</summary>
    public decimal GrossProfit => Sales - CostOfGoodsSold;

    /// <summary>إجمالي الربح − المصاريف = الربح الحقيقي</summary>
    public decimal NetProfit => GrossProfit - Expenses;

    /// <summary>الربح المتاح للتوزيع بعد خصم ما وُزّع في نفس الشهر</summary>
    public decimal DistributableAmount => Math.Max(0, NetProfit - AlreadyDistributedInPeriod);
}

public class InvestorOpeningBalanceItem
{
    public int InvestorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal ProfitPercentage { get; set; }
    public decimal OpeningBalance { get; set; }
}
