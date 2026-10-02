using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class InvestorService : IInvestorService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public InvestorService(IDbContextFactory<AppDbContext> contextFactory, ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<Investor>> GetAllInvestorsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Investors.OrderBy(i => i.Name).ToListAsync();
    }

    public async Task<Investor> AddInvestorAsync(string name, string? phone, decimal profitPercentage, string? customFieldsJson = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var trimmedName = name.Trim();
        var trimmedPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        Investor? softDeleted = null;
        if (!await context.Investors.AnyAsync(i => i.Name == trimmedName))
        {
            softDeleted = await context.Investors
                .IgnoreQueryFilters()
                .Where(i => i.IsDeleted && i.Name == trimmedName)
                .OrderByDescending(i => i.DeletedAt)
                .FirstOrDefaultAsync();
        }

        if (softDeleted is not null)
        {
            softDeleted.RestoreFromSoftDelete(_currentUserService.Username);
            softDeleted.Name = trimmedName;
            softDeleted.Phone = trimmedPhone;
            softDeleted.ProfitPercentage = profitPercentage;
            softDeleted.CustomFieldsJson = customFieldsJson;
            await context.SaveChangesAsync();
            return softDeleted;
        }

        var investor = new Investor
        {
            Name = trimmedName,
            Phone = trimmedPhone,
            ProfitPercentage = profitPercentage,
            TotalDeposit = 0,
            OpeningBalance = 0,
            CustomFieldsJson = customFieldsJson,
            CreatedBy = _currentUserService.Username
        };
        await context.Investors.AddAsync(investor);
        await context.SaveChangesAsync();
        return investor;
    }

    public async Task UpdateInvestorAsync(int id, string name, string? phone, decimal profitPercentage, string? customFieldsJson = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var investor = await context.Investors.FindAsync(id) ?? throw new InvalidOperationException("المستثمر غير موجود");
        investor.Name = name;
        investor.Phone = phone;
        investor.ProfitPercentage = profitPercentage;
        investor.CustomFieldsJson = customFieldsJson;
        await context.SaveChangesAsync();
    }

    public async Task DeleteInvestorAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var investor = await context.Investors.FindAsync(id)
            ?? throw new InvalidOperationException("المستثمر غير موجود");

        // لا يُحذف مستثمر له رأس مال متبقٍ — يجب سحب الرصيد أولاً للحفاظ على توازن القاصة
        if (investor.TotalDeposit != 0)
            throw new InvalidOperationException(
                $"لا يمكن حذف المستثمر «{investor.Name}» لأن له رصيد إيداع متبقٍ ({investor.TotalDeposit:N0}). اسحب الرصيد أولاً ثم أعد المحاولة.");

        investor.MarkSoftDeleted(_currentUserService.Username ?? "system");
        await context.SaveChangesAsync();
    }

    public async Task SaveOpeningBalancesAsync(IEnumerable<InvestorOpeningBalanceItem> items)
    {
        var list = items.ToList();
        if (list.Count == 0) return;

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var username = _currentUserService.Username ?? "system";
            foreach (var item in list)
            {
                var investor = await context.Investors.FindAsync(item.InvestorId);
                if (investor is null) continue;

                var nameChanged = investor.Name != item.Name.Trim();
                var phoneChanged = investor.Phone != item.Phone;
                var pctChanged = investor.ProfitPercentage != item.ProfitPercentage;
                var openingChanged = investor.OpeningBalance != item.OpeningBalance;

                if (!nameChanged && !phoneChanged && !pctChanged && !openingChanged)
                    continue;

                investor.Name = item.Name.Trim();
                investor.Phone = string.IsNullOrWhiteSpace(item.Phone) ? null : item.Phone.Trim();
                investor.ProfitPercentage = item.ProfitPercentage;

                if (openingChanged)
                {
                    investor.OpeningBalance = item.OpeningBalance;
                    await RecalculateTotalDepositAsync(context, investor);

                    var existingOpeningTx = await context.InvestorTransactions
                        .FirstOrDefaultAsync(t => t.InvestorId == investor.Id && t.Type == InvestorTransactionType.OpeningBalance);

                    if (existingOpeningTx is not null)
                    {
                        existingOpeningTx.Amount = item.OpeningBalance;
                        existingOpeningTx.Notes = "رصيد افتتاحي للمستثمر";
                        existingOpeningTx.UpdatedBy = username;
                        existingOpeningTx.UpdatedAt = DateTime.UtcNow;
                    }
                    else if (item.OpeningBalance > 0)
                    {
                        await context.InvestorTransactions.AddAsync(new InvestorTransaction
                        {
                            InvestorId = investor.Id,
                            Type = InvestorTransactionType.OpeningBalance,
                            Amount = item.OpeningBalance,
                            Date = DateTime.Today,
                            Notes = "رصيد افتتاحي للمستثمر",
                            CreatedBy = username,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                investor.UpdatedBy = username;
                investor.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch { await transaction.RollbackAsync(); throw; }
    }

    public async Task DepositAsync(int investorId, decimal amount, DateTime date, int cashBoxId, string? notes)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var investor = await context.Investors.FindAsync(investorId) ?? throw new InvalidOperationException("المستثمر غير موجود");
            var cashBox = await context.CashBoxes.FindAsync(cashBoxId) ?? throw new InvalidOperationException("القاصة غير موجودة");
            EnsureInvestorCashBoxIsIqd(cashBox);
            cashBox.Balance += amount; investor.TotalDeposit += amount;
            var tx = new InvestorTransaction { InvestorId = investorId, Type = InvestorTransactionType.Deposit, Amount = amount, Date = date, Notes = notes };
            await context.InvestorTransactions.AddAsync(tx);
            await context.SaveChangesAsync();
            await CreateAuditLogAsync(context, "InvestorDeposit", tx.Id, $"إيداع مستثمر: {investor.Name}, المبلغ: {amount:N0}, القاصة: {cashBox.Name}");
            await transaction.CommitAsync();
        }
        catch { await transaction.RollbackAsync(); throw; }
    }

    public async Task WithdrawAsync(int investorId, decimal amount, DateTime date, int cashBoxId, string? notes)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var investor = await context.Investors.FindAsync(investorId) ?? throw new InvalidOperationException("المستثمر غير موجود");
            var cashBox = await context.CashBoxes.FindAsync(cashBoxId) ?? throw new InvalidOperationException("القاصة غير موجودة");
            EnsureInvestorCashBoxIsIqd(cashBox);
            if (amount > investor.TotalDeposit) throw new InvalidOperationException($"مبلغ السحب ({amount:N0}) يتجاوز رصيد الإيداع ({investor.TotalDeposit:N0})");
            if (amount > cashBox.Balance) throw new InvalidOperationException($"رصيد القاصة غير كافٍ. الرصيد الحالي: {cashBox.Balance:N0}");
            cashBox.Balance -= amount; investor.TotalDeposit -= amount;
            var tx = new InvestorTransaction { InvestorId = investorId, Type = InvestorTransactionType.Withdrawal, Amount = amount, Date = date, Notes = notes };
            await context.InvestorTransactions.AddAsync(tx);
            await context.SaveChangesAsync();
            await CreateAuditLogAsync(context, "InvestorWithdrawal", tx.Id, $"سحب مستثمر: {investor.Name}, المبلغ: {amount:N0}, القاصة: {cashBox.Name}");
            await transaction.CommitAsync();
        }
        catch { await transaction.RollbackAsync(); throw; }
    }

    public async Task<IEnumerable<InvestorTransaction>> GetRecentDepositsAsync(int count = 20)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.InvestorTransactions.Include(t => t.Investor)
            .Where(t => t.Type == InvestorTransactionType.Deposit)
            .OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).Take(count).ToListAsync();
    }

    public async Task<IEnumerable<InvestorTransaction>> GetRecentWithdrawalsAsync(int count = 20)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.InvestorTransactions.Include(t => t.Investor)
            .Where(t => t.Type == InvestorTransactionType.Withdrawal)
            .OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).Take(count).ToListAsync();
    }

    public async Task<DistributableProfitBreakdown> GetDistributableProfitsAsync(DateTime periodDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await MultiCurrencyFeatureGate.IsEnabledAsync(context));

        var monthStart = new DateTime(periodDate.Year, periodDate.Month, 1);
        var monthEndExclusive = monthStart.AddMonths(1);

        // مبيعات الشهر (مع MultiCurrency: طي الدولار إلى دينار بسعر المستند)
        var salesQ = foldInUsd
            ? InvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans, ReportCurrencyScope.All)
            : InvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans);
        salesQ = salesQ.Where(i => i.Date >= monthStart && i.Date < monthEndExclusive);

        var salesRows = await salesQ
            .Select(i => new { i.InvoiceType, i.NetAmount, i.Currency, i.FxRate })
            .ToListAsync();
        var totalSales = FinancialReportIqdFoldIn.SumSignedSalesInBaseIqd(
            salesRows.Select(r => (r.InvoiceType, r.NetAmount, r.Currency, r.FxRate)), foldInUsd);

        // تكلفة البضاعة المباعة خلال الشهر
        var cogs = await ProductCostHelper.CalculateCogsAsync(context, monthStart, monthEndExclusive);

        // مصاريف الشهر
        var expensesQ = foldInUsd
            ? context.Expenses.AsQueryable()
            : context.Expenses.Where(e => e.Currency == AccountingCurrency.IQD);
        expensesQ = expensesQ.Where(e => e.Date >= monthStart && e.Date < monthEndExclusive);

        var expenseRows = await expensesQ
            .Select(e => new { e.Amount, e.Currency, e.FxRate })
            .ToListAsync();
        var totalExpenses = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            expenseRows.Select(r => (r.Amount, r.Currency, r.FxRate)), foldInUsd);

        var alreadyDistributed = await context.ProfitDistributions
            .Where(pd => pd.Date >= monthStart && pd.Date < monthEndExclusive)
            .SumAsync(pd => (decimal?)pd.DistributedAmount ?? 0);

        return new DistributableProfitBreakdown
        {
            PeriodFrom = monthStart,
            PeriodToExclusive = monthEndExclusive,
            Sales = totalSales,
            CostOfGoodsSold = cogs,
            Expenses = totalExpenses,
            AlreadyDistributedInPeriod = alreadyDistributed
        };
    }

    public async Task<decimal> GetEligibleDepositAsync(int investorId, DateTime distributionDate, int eligibilityDays = 15)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var days = Math.Max(0, eligibilityDays);
        var cutoffDate = distributionDate.AddDays(-days);
        var investor = await context.Investors.FindAsync(investorId);
        if (investor is null) return 0;

        var eligibleDeposits = investor.OpeningBalance + await context.InvestorTransactions
            .Where(t => t.InvestorId == investorId && t.Type == InvestorTransactionType.Deposit && t.Date <= cutoffDate)
            .SumAsync(t => (decimal?)t.Amount ?? 0);
        var totalWithdrawals = await context.InvestorTransactions
            .Where(t => t.InvestorId == investorId && t.Type == InvestorTransactionType.Withdrawal && t.Date <= distributionDate)
            .SumAsync(t => (decimal?)t.Amount ?? 0);
        var eligible = eligibleDeposits - totalWithdrawals;
        return Math.Max(0, Math.Min(eligible, investor.TotalDeposit));
    }

    public async Task<IEnumerable<ProfitPreviewItem>> PreviewProfitDistributionAsync(DateTime distributionDate, decimal totalDistributableProfits, int eligibilityDays = 15)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var investors = await context.Investors.Where(i => i.TotalDeposit > 0 && i.ProfitPercentage > 0).OrderBy(i => i.Name).ToListAsync();
        var previews = new List<ProfitPreviewItem>();
        foreach (var investor in investors)
        {
            var eligible = await GetEligibleDepositAsync(investor.Id, distributionDate, eligibilityDays);
            if (eligible <= 0) continue;
            previews.Add(new ProfitPreviewItem
            {
                InvestorId = investor.Id,
                InvestorName = investor.Name,
                InvestorPhone = investor.Phone,
                TotalDeposit = investor.TotalDeposit,
                EligibleDeposit = eligible,
                ProfitPercentage = investor.ProfitPercentage,
                ProfitAmount = Math.Round(eligible * investor.ProfitPercentage / 100m, 0),
                IsIncluded = true
            });
        }
        return previews;
    }

    public async Task DistributeProfitsAsync(DateTime distributionDate, int cashBoxId, decimal totalDistributableProfits, IEnumerable<ProfitPreviewItem> items)
    {
        var includedItems = items.Where(i => i.IsIncluded && i.ProfitAmount > 0).ToList();
        if (includedItems.Count == 0) throw new InvalidOperationException("لا يوجد مستثمرون مؤهلون للتوزيع");
        var totalToDistribute = includedItems.Sum(i => i.ProfitAmount);
        if (totalToDistribute > totalDistributableProfits) throw new InvalidOperationException($"مجموع التوزيع ({totalToDistribute:N0}) يتجاوز الأرباح المتاحة ({totalDistributableProfits:N0})");

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var cashBox = await context.CashBoxes.FindAsync(cashBoxId) ?? throw new InvalidOperationException("القاصة غير موجودة");
            EnsureInvestorCashBoxIsIqd(cashBox);
            if (totalToDistribute > cashBox.Balance) throw new InvalidOperationException($"رصيد القاصة غير كافٍ. الرصيد: {cashBox.Balance:N0}, المطلوب: {totalToDistribute:N0}");
            cashBox.Balance -= totalToDistribute;

            var distribution = new ProfitDistribution { Date = distributionDate, TotalProfit = totalDistributableProfits, DistributedAmount = totalToDistribute };
            await context.ProfitDistributions.AddAsync(distribution);
            await context.SaveChangesAsync();

            foreach (var item in includedItems)
            {
                await context.ProfitDistributionDetails.AddAsync(new ProfitDistributionDetail { ProfitDistributionId = distribution.Id, InvestorId = item.InvestorId, ProfitPercentage = item.ProfitPercentage, Amount = item.ProfitAmount });
                await context.InvestorTransactions.AddAsync(new InvestorTransaction { InvestorId = item.InvestorId, Type = InvestorTransactionType.ProfitDistribution, Amount = item.ProfitAmount, Date = distributionDate, Notes = $"توزيع أرباح - الإيداع المؤهل: {item.EligibleDeposit:N0}, النسبة: {item.ProfitPercentage}%" });
            }
            await context.SaveChangesAsync();
            await CreateAuditLogAsync(context, "ProfitDistribution", distribution.Id, $"توزيع أرباح: {totalToDistribute:N0} على {includedItems.Count} مستثمر");
            await transaction.CommitAsync();
        }
        catch { await transaction.RollbackAsync(); throw; }
    }

    public async Task<IEnumerable<ProfitDistributionDetail>> GetProfitDetailsForInvestorAsync(int investorId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProfitDistributionDetails.Include(d => d.ProfitDistribution)
            .Where(d => d.InvestorId == investorId).OrderByDescending(d => d.ProfitDistribution.Date).ToListAsync();
    }

    public async Task<decimal> GetTotalProfitsEarnedAsync(int investorId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProfitDistributionDetails.Where(d => d.InvestorId == investorId).SumAsync(d => (decimal?)d.Amount ?? 0);
    }

    private static async Task RecalculateTotalDepositAsync(AppDbContext context, Investor investor)
    {
        var deposits = await context.InvestorTransactions
            .Where(t => t.InvestorId == investor.Id && t.Type == InvestorTransactionType.Deposit)
            .SumAsync(t => (decimal?)t.Amount ?? 0);
        var withdrawals = await context.InvestorTransactions
            .Where(t => t.InvestorId == investor.Id && t.Type == InvestorTransactionType.Withdrawal)
            .SumAsync(t => (decimal?)t.Amount ?? 0);
        investor.TotalDeposit = investor.OpeningBalance + deposits - withdrawals;
    }

    /// <summary>
    /// دفتر المستثمر ورأس المال بالدينار فقط حالياً — منع خلط دولار في TotalDeposit/التوزيع.
    /// </summary>
    private static void EnsureInvestorCashBoxIsIqd(CashBox cashBox)
    {
        if (cashBox.Currency == AccountingCurrency.IQD)
            return;

        throw new InvalidOperationException(
            $"عمليات المستثمرين بالدينار فقط. القاصة '{cashBox.Name}' بعملة {AccountingCurrencyHelper.GetDisplayName(cashBox.Currency)}. اختر قاصة دينار.");
    }

    private async Task CreateAuditLogAsync(AppDbContext context, string entityName, int entityId, string description)
    {
        if (!_currentUserService.UserId.HasValue) return;
        await context.AuditLogs.AddAsync(new AuditLog { UserId = _currentUserService.UserId.Value, Action = AuditAction.Add, EntityName = entityName, EntityId = entityId, NewValues = description, Timestamp = DateTime.UtcNow, CreatedBy = _currentUserService.Username, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();
    }
}
