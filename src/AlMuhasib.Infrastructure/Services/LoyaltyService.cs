using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Loyalty;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class LoyaltyService : ILoyaltyService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public LoyaltyService(
        IDbContextFactory<AppDbContext> contextFactory,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public async Task<LoyaltySettings> GetOrCreateSettingsAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await GetOrCreateSettingsCoreAsync(context, ct);
    }

    public async Task SaveSettingsAsync(LoyaltySettings settings, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var existing = await context.LoyaltySettings.OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        var username = _currentUserService.Username;

        if (existing is null)
        {
            settings.CreatedBy = username;
            settings.CreatedAt = DateTime.UtcNow;
            NormalizeSettings(settings);
            await context.LoyaltySettings.AddAsync(settings, ct);
        }
        else
        {
            existing.PointsPerAmount = settings.PointsPerAmount;
            existing.PointValueInCurrency = settings.PointValueInCurrency;
            existing.MinInvoiceAmountToEarn = settings.MinInvoiceAmountToEarn;
            existing.MinPointsToRedeem = settings.MinPointsToRedeem;
            existing.MaxRedeemPercentOfInvoice = settings.MaxRedeemPercentOfInvoice;
            existing.PointsExpireAfterDays = settings.PointsExpireAfterDays;
            existing.EarnOnCreditSales = settings.EarnOnCreditSales;
            existing.RoundEarnDown = settings.RoundEarnDown;
            existing.UpdatedBy = username;
            existing.UpdatedAt = DateTime.UtcNow;
            NormalizeSettings(existing);
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task<CustomerLoyaltyAccount?> GetAccountAsync(int customerId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.CustomerLoyaltyAccounts
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.CustomerId == customerId, ct);
    }

    public async Task<CustomerLoyaltyAccount> GetOrCreateAccountAsync(int customerId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await GetOrCreateAccountCoreAsync(context, customerId, ct);
    }

    public async Task<int> GetBalanceAsync(int customerId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var balance = await context.CustomerLoyaltyAccounts
            .Where(a => a.CustomerId == customerId)
            .Select(a => (int?)a.PointsBalance)
            .FirstOrDefaultAsync(ct);
        return balance ?? 0;
    }

    public async Task<LoyaltyQuote> QuoteAsync(
        int customerId,
        decimal invoiceBaseAmount,
        int? redeemPoints,
        PaymentMethod paymentMethod,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var settings = await GetOrCreateSettingsCoreAsync(context, ct);
        var account = await context.CustomerLoyaltyAccounts
            .FirstOrDefaultAsync(a => a.CustomerId == customerId, ct);
        var balance = account?.PointsBalance ?? 0;

        var canEarn = paymentMethod != PaymentMethod.Credit || settings.EarnOnCreditSales;
        var earn = canEarn
            ? LoyaltyPointsCalculator.CalculateEarnPoints(Math.Max(0m, invoiceBaseAmount), settings)
            : 0;

        var maxRedeem = LoyaltyPointsCalculator.MaxRedeemablePoints(balance, Math.Max(0m, invoiceBaseAmount), settings);
        var requested = Math.Max(0, redeemPoints ?? 0);
        var (points, discount, error) = LoyaltyPointsCalculator.ValidateRedeem(
            requested, balance, Math.Max(0m, invoiceBaseAmount), settings);

        return new LoyaltyQuote
        {
            CustomerId = customerId,
            Balance = balance,
            ExpectedEarnPoints = earn,
            MaxRedeemablePoints = maxRedeem,
            RequestedRedeemPoints = points,
            RedeemDiscount = discount,
            Error = error
        };
    }

    public async Task AdjustPointsAsync(int customerId, int pointsDelta, string note, int? userId, CancellationToken ct = default)
    {
        if (pointsDelta == 0)
            throw new InvalidOperationException("قيمة التعديل يجب ألا تكون صفراً");
        if (string.IsNullOrWhiteSpace(note))
            throw new InvalidOperationException("أدخل سبب التعديل");

        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var account = await GetOrCreateAccountCoreAsync(context, customerId, ct);
        var newBalance = account.PointsBalance + pointsDelta;
        if (newBalance < 0)
            throw new InvalidOperationException("لا يمكن أن يصبح رصيد النقاط سالباً");

        account.PointsBalance = newBalance;
        if (pointsDelta > 0)
            account.LifetimeEarned += pointsDelta;
        else
            account.LifetimeRedeemed += Math.Abs(pointsDelta);

        account.UpdatedBy = _currentUserService.Username;
        account.UpdatedAt = DateTime.UtcNow;

        var settings = await GetOrCreateSettingsCoreAsync(context, ct);
        await context.LoyaltyPointTransactions.AddAsync(new LoyaltyPointTransaction
        {
            CustomerId = customerId,
            Type = LoyaltyTransactionType.Adjust,
            Points = Math.Abs(pointsDelta),
            UnitValue = settings.PointValueInCurrency,
            CurrencyAmount = Math.Abs(pointsDelta) * settings.PointValueInCurrency,
            BalanceAfter = newBalance,
            Note = pointsDelta > 0 ? $"تعديل +{pointsDelta}: {note.Trim()}" : $"تعديل {pointsDelta}: {note.Trim()}",
            CreatedByUserId = userId ?? _currentUserService.UserId,
            CreatedBy = _currentUserService.Username,
            CreatedAt = DateTime.UtcNow
        }, ct);

        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<LoyaltyPointTransaction>> GetLedgerAsync(
        int? customerId,
        LoyaltyTransactionType? type,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var q = context.LoyaltyPointTransactions
            .Include(t => t.Customer)
            .Include(t => t.Invoice)
            .AsQueryable();

        if (customerId is int cid)
            q = q.Where(t => t.CustomerId == cid);
        if (type is LoyaltyTransactionType t)
            q = q.Where(x => x.Type == t);
        if (from is DateTime f)
            q = q.Where(x => x.CreatedAt >= f.Date);
        if (to is DateTime tTo)
            q = q.Where(x => x.CreatedAt < tTo.Date.AddDays(1));

        return await q.OrderByDescending(x => x.CreatedAt).Take(2000).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LoyaltyAccountRow>> GetAccountsAsync(string? search, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var q = context.CustomerLoyaltyAccounts.Include(a => a.Customer).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(a => a.Customer.Name.Contains(s) || (a.Customer.Phone != null && a.Customer.Phone.Contains(s)));
        }

        return await q
            .OrderByDescending(a => a.PointsBalance)
            .ThenBy(a => a.Customer.Name)
            .Select(a => new LoyaltyAccountRow
            {
                AccountId = a.Id,
                CustomerId = a.CustomerId,
                CustomerName = a.Customer.Name,
                Phone = a.Customer.Phone,
                PointsBalance = a.PointsBalance,
                LifetimeEarned = a.LifetimeEarned,
                LifetimeRedeemed = a.LifetimeRedeemed,
                TierName = a.Tier.ToString(),
                LastEarnedAt = a.LastEarnedAt,
                LastRedeemedAt = a.LastRedeemedAt
            })
            .ToListAsync(ct);
    }

    public async Task<LoyaltySummaryReport> GetSummaryReportAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var q = context.LoyaltyPointTransactions.AsQueryable();
        if (from is DateTime f)
            q = q.Where(x => x.CreatedAt >= f.Date);
        if (to is DateTime tTo)
            q = q.Where(x => x.CreatedAt < tTo.Date.AddDays(1));

        var rows = await q.Select(x => new { x.Type, x.Points, x.CurrencyAmount, x.CustomerId }).ToListAsync(ct);
        return new LoyaltySummaryReport
        {
            TotalEarnedPoints = rows.Where(r => r.Type == LoyaltyTransactionType.Earn).Sum(r => r.Points),
            TotalRedeemedPoints = rows.Where(r => r.Type == LoyaltyTransactionType.Redeem).Sum(r => r.Points),
            TotalAdjustedPoints = rows.Where(r => r.Type == LoyaltyTransactionType.Adjust).Sum(r => r.Points),
            TotalExpiredPoints = rows.Where(r => r.Type == LoyaltyTransactionType.Expire).Sum(r => r.Points),
            TotalRedeemDiscountValue = rows.Where(r => r.Type == LoyaltyTransactionType.Redeem).Sum(r => r.CurrencyAmount),
            ActiveCustomersCount = rows.Select(r => r.CustomerId).Distinct().Count(),
            TransactionsCount = rows.Count
        };
    }

    public async Task<IReadOnlyList<LoyaltyTopCustomerRow>> GetTopCustomersAsync(
        DateTime? from,
        DateTime? to,
        int take = 50,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        // ترتيب حسب الرصيد الحالي مع إظهار النشاط في الفترة إن وُجدت حركات
        var accounts = await context.CustomerLoyaltyAccounts
            .Include(a => a.Customer)
            .OrderByDescending(a => a.LifetimeEarned)
            .ThenByDescending(a => a.PointsBalance)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync(ct);

        return accounts.Select(a => new LoyaltyTopCustomerRow
        {
            CustomerId = a.CustomerId,
            CustomerName = a.Customer.Name,
            Phone = a.Customer.Phone,
            PointsBalance = a.PointsBalance,
            LifetimeEarned = a.LifetimeEarned,
            LifetimeRedeemed = a.LifetimeRedeemed,
            TierName = TierDisplay(a.Tier)
        }).ToList();
    }

    /// <summary>يضبط خصم الولاء على الفاتورة قبل احتساب الصافي (داخل نفس السياق).</summary>
    public async Task PrepareInvoiceRedeemDiscountAsync(
        AppDbContext context,
        Invoice invoice,
        int loyaltyRedeemPoints,
        CancellationToken ct = default)
    {
        if (invoice.InvoiceType != InvoiceType.Sale || invoice.CustomerId is not int customerId)
        {
            invoice.LoyaltyPointsRedeemed = 0;
            invoice.LoyaltyRedeemDiscountAmount = 0m;
            return;
        }

        var settings = await GetOrCreateSettingsCoreAsync(context, ct);
        var account = await context.CustomerLoyaltyAccounts
            .FirstOrDefaultAsync(a => a.CustomerId == customerId, ct);
        var balance = account?.PointsBalance ?? 0;

        var manualDiscount = Math.Max(0m, invoice.DiscountAmount - Math.Max(0m, invoice.LoyaltyRedeemDiscountAmount));
        var baseBeforeLoyalty = Math.Max(0m, invoice.TotalAmount - manualDiscount);
        var redeemRequested = Math.Max(0, loyaltyRedeemPoints);

        if (redeemRequested <= 0)
        {
            invoice.LoyaltyPointsRedeemed = 0;
            invoice.LoyaltyRedeemDiscountAmount = 0m;
            invoice.DiscountAmount = manualDiscount;
            return;
        }

        var (points, discount, error) = LoyaltyPointsCalculator.ValidateRedeem(
            redeemRequested, balance, baseBeforeLoyalty, settings);
        if (!string.IsNullOrEmpty(error))
            throw new InvalidOperationException(error);

        invoice.LoyaltyPointsRedeemed = points;
        invoice.LoyaltyRedeemDiscountAmount = discount;
        invoice.DiscountAmount = manualDiscount + discount;
    }

    /// <summary>يسجّل حركات الكسب/الاستبدال بعد حفظ الفاتورة داخل نفس المعاملة.</summary>
    public async Task ApplyInvoiceLoyaltyAsync(
        AppDbContext context,
        Invoice invoice,
        int loyaltyRedeemPoints,
        string username,
        int? userId,
        CancellationToken ct = default)
    {
        if (invoice.InvoiceType != InvoiceType.Sale || invoice.CustomerId is not int customerId)
            return;

        var settings = await GetOrCreateSettingsCoreAsync(context, ct);
        var account = await GetOrCreateAccountCoreAsync(context, customerId, ct);
        var changed = false;

        var redeemPoints = invoice.LoyaltyPointsRedeemed > 0
            ? invoice.LoyaltyPointsRedeemed
            : Math.Max(0, loyaltyRedeemPoints);

        if (redeemPoints > 0)
        {
            if (account.PointsBalance < redeemPoints)
                throw new InvalidOperationException("رصيد النقاط غير كافٍ");

            account.PointsBalance -= redeemPoints;
            account.LifetimeRedeemed += redeemPoints;
            account.LastRedeemedAt = DateTime.UtcNow;
            account.UpdatedBy = username;
            account.UpdatedAt = DateTime.UtcNow;
            invoice.LoyaltyPointsRedeemed = redeemPoints;

            await context.LoyaltyPointTransactions.AddAsync(new LoyaltyPointTransaction
            {
                CustomerId = customerId,
                InvoiceId = invoice.Id,
                Type = LoyaltyTransactionType.Redeem,
                Points = redeemPoints,
                UnitValue = settings.PointValueInCurrency,
                CurrencyAmount = invoice.LoyaltyRedeemDiscountAmount,
                BalanceAfter = account.PointsBalance,
                Note = $"استبدال نقاط فاتورة {invoice.InvoiceNumber}",
                CreatedByUserId = userId,
                CreatedBy = username,
                CreatedAt = DateTime.UtcNow
            }, ct);
            changed = true;
        }

        var canEarn = invoice.PaymentMethod != PaymentMethod.Credit || settings.EarnOnCreditSales;
        var earnBase = Math.Max(0m, invoice.TotalAmount - invoice.DiscountAmount);
        var earnPoints = canEarn
            ? LoyaltyPointsCalculator.CalculateEarnPoints(earnBase, settings)
            : 0;

        if (earnPoints > 0)
        {
            account.PointsBalance += earnPoints;
            account.LifetimeEarned += earnPoints;
            account.LastEarnedAt = DateTime.UtcNow;
            account.UpdatedBy = username;
            account.UpdatedAt = DateTime.UtcNow;
            account.Tier = ResolveTier(account.LifetimeEarned);
            invoice.LoyaltyPointsEarned = earnPoints;

            await context.LoyaltyPointTransactions.AddAsync(new LoyaltyPointTransaction
            {
                CustomerId = customerId,
                InvoiceId = invoice.Id,
                Type = LoyaltyTransactionType.Earn,
                Points = earnPoints,
                UnitValue = settings.PointValueInCurrency,
                CurrencyAmount = earnPoints * settings.PointValueInCurrency,
                BalanceAfter = account.PointsBalance,
                Note = $"كسب نقاط فاتورة {invoice.InvoiceNumber}",
                CreatedByUserId = userId,
                CreatedBy = username,
                CreatedAt = DateTime.UtcNow
            }, ct);
            changed = true;
        }

        if (changed)
            await context.SaveChangesAsync(ct);
    }

    private static LoyaltyTier ResolveTier(int lifetimeEarned) => lifetimeEarned switch
    {
        >= 5000 => LoyaltyTier.Gold,
        >= 1000 => LoyaltyTier.Silver,
        _ => LoyaltyTier.Standard
    };

    private static string TierDisplay(LoyaltyTier tier) => tier switch
    {
        LoyaltyTier.Gold => "ذهبي",
        LoyaltyTier.Silver => "فضي",
        _ => "عادي"
    };

    private async Task<LoyaltySettings> GetOrCreateSettingsCoreAsync(AppDbContext context, CancellationToken ct)
    {
        var existing = await context.LoyaltySettings.OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (existing is not null)
            return existing;

        var created = new LoyaltySettings
        {
            CreatedBy = _currentUserService.Username,
            CreatedAt = DateTime.UtcNow
        };
        NormalizeSettings(created);
        await context.LoyaltySettings.AddAsync(created, ct);
        await context.SaveChangesAsync(ct);
        return created;
    }

    private async Task<CustomerLoyaltyAccount> GetOrCreateAccountCoreAsync(
        AppDbContext context, int customerId, CancellationToken ct)
    {
        var existing = await context.CustomerLoyaltyAccounts
            .FirstOrDefaultAsync(a => a.CustomerId == customerId, ct);
        if (existing is not null)
            return existing;

        var customerExists = await context.Customers.AnyAsync(c => c.Id == customerId, ct);
        if (!customerExists)
            throw new InvalidOperationException("الزبون غير موجود");

        var account = new CustomerLoyaltyAccount
        {
            CustomerId = customerId,
            CreatedBy = _currentUserService.Username,
            CreatedAt = DateTime.UtcNow
        };
        await context.CustomerLoyaltyAccounts.AddAsync(account, ct);
        await context.SaveChangesAsync(ct);
        return account;
    }

    private static void NormalizeSettings(LoyaltySettings s)
    {
        if (s.PointsPerAmount <= 0m) s.PointsPerAmount = 1000m;
        if (s.PointValueInCurrency < 0m) s.PointValueInCurrency = 0m;
        if (s.MinInvoiceAmountToEarn < 0m) s.MinInvoiceAmountToEarn = 0m;
        if (s.MinPointsToRedeem < 1) s.MinPointsToRedeem = 1;
        s.MaxRedeemPercentOfInvoice = Math.Clamp(s.MaxRedeemPercentOfInvoice, 0m, 100m);
        if (s.PointsExpireAfterDays is <= 0)
            s.PointsExpireAfterDays = null;
    }
}
