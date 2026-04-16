using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
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

    public async Task<Investor> AddInvestorAsync(string name, string? phone, decimal profitPercentage)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var investor = new Investor { Name = name, Phone = phone, ProfitPercentage = profitPercentage, TotalDeposit = 0 };
        await context.Investors.AddAsync(investor);
        await context.SaveChangesAsync();
        return investor;
    }

    public async Task UpdateInvestorAsync(int id, string name, string? phone, decimal profitPercentage)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var investor = await context.Investors.FindAsync(id) ?? throw new InvalidOperationException("المستثمر غير موجود");
        investor.Name = name; investor.Phone = phone; investor.ProfitPercentage = profitPercentage;
        await context.SaveChangesAsync();
    }

    public async Task DepositAsync(int investorId, decimal amount, DateTime date, int cashBoxId, string? notes)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var investor = await context.Investors.FindAsync(investorId) ?? throw new InvalidOperationException("المستثمر غير موجود");
            var cashBox = await context.CashBoxes.FindAsync(cashBoxId) ?? throw new InvalidOperationException("القاصة غير موجودة");
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

    public async Task<decimal> GetDistributableProfitsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var totalSales = await context.Invoices.Where(i => i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment).SumAsync(i => (decimal?)i.NetAmount ?? 0);
        var totalPurchases = await context.Invoices.Where(i => i.InvoiceType == InvoiceType.Purchase).SumAsync(i => (decimal?)i.NetAmount ?? 0);
        var totalExpenses = await context.Expenses.SumAsync(e => (decimal?)e.Amount ?? 0);
        var alreadyDistributed = await context.ProfitDistributions.SumAsync(pd => (decimal?)pd.DistributedAmount ?? 0);
        return totalSales - totalPurchases - totalExpenses - alreadyDistributed;
    }

    public async Task<decimal> GetEligibleDepositAsync(int investorId, DateTime distributionDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cutoffDate = distributionDate.AddDays(-15);
        var eligibleDeposits = await context.InvestorTransactions.Where(t => t.InvestorId == investorId && t.Type == InvestorTransactionType.Deposit && t.Date <= cutoffDate).SumAsync(t => (decimal?)t.Amount ?? 0);
        var totalWithdrawals = await context.InvestorTransactions.Where(t => t.InvestorId == investorId && t.Type == InvestorTransactionType.Withdrawal && t.Date <= distributionDate).SumAsync(t => (decimal?)t.Amount ?? 0);
        var eligible = eligibleDeposits - totalWithdrawals;
        var investor = await context.Investors.FindAsync(investorId);
        if (investor is null) return 0;
        return Math.Max(0, Math.Min(eligible, investor.TotalDeposit));
    }

    public async Task<IEnumerable<ProfitPreviewItem>> PreviewProfitDistributionAsync(DateTime distributionDate, decimal totalDistributableProfits)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var investors = await context.Investors.Where(i => i.TotalDeposit > 0 && i.ProfitPercentage > 0).OrderBy(i => i.Name).ToListAsync();
        var previews = new List<ProfitPreviewItem>();
        foreach (var investor in investors)
        {
            var eligible = await GetEligibleDepositAsync(investor.Id, distributionDate);
            if (eligible <= 0) continue;
            previews.Add(new ProfitPreviewItem { InvestorId = investor.Id, InvestorName = investor.Name, TotalDeposit = investor.TotalDeposit, EligibleDeposit = eligible, ProfitPercentage = investor.ProfitPercentage, ProfitAmount = Math.Round(eligible * investor.ProfitPercentage / 100m, 0), IsIncluded = true });
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

    private async Task CreateAuditLogAsync(AppDbContext context, string entityName, int entityId, string description)
    {
        if (!_currentUserService.UserId.HasValue) return;
        await context.AuditLogs.AddAsync(new AuditLog { UserId = _currentUserService.UserId.Value, Action = AuditAction.Add, EntityName = entityName, EntityId = entityId, NewValues = description, Timestamp = DateTime.UtcNow, CreatedBy = _currentUserService.Username, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();
    }
}
