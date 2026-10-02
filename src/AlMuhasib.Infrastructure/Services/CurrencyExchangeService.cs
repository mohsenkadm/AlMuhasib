using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class CurrencyExchangeService : ICurrencyExchangeService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IExchangeRateService _exchangeRateService;

    public CurrencyExchangeService(
        IDbContextFactory<AppDbContext> contextFactory,
        ICurrentUserService currentUserService,
        IExchangeRateService exchangeRateService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _exchangeRateService = exchangeRateService;
    }

    public async Task<CurrencyExchange> CreateAsync(
        int fromCashBoxId,
        int toCashBoxId,
        decimal fromAmount,
        decimal? fxRateOverride,
        DateTime date,
        string? notes,
        CancellationToken ct = default)
    {
        if (fromCashBoxId == toCashBoxId)
            throw new InvalidOperationException("اختر قاصتين مختلفتين");
        if (fromAmount <= 0)
            throw new InvalidOperationException("أدخل مبلغاً أكبر من صفر");

        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        await using var tx = await context.Database.BeginTransactionAsync(ct);
        try
        {
            var fromBox = await context.CashBoxes.FirstOrDefaultAsync(c => c.Id == fromCashBoxId, ct)
                ?? throw new InvalidOperationException("قاصة المصدر غير موجودة");
            var toBox = await context.CashBoxes.FirstOrDefaultAsync(c => c.Id == toCashBoxId, ct)
                ?? throw new InvalidOperationException("قاصة الوجهة غير موجودة");

            if (fromBox.Currency == toBox.Currency)
                throw new InvalidOperationException("الصيرفة تتطلب قاصتين بعملتين مختلفتين (دينار ودولار)");

            var fromCurrency = fromBox.Currency;
            var toCurrency = toBox.Currency;

            var fx = fxRateOverride is > 0
                ? fxRateOverride.Value
                : await _exchangeRateService.GetUsdToIqdForDateOrLatestAsync(date.Date, ct);

            // التحقق من تفعيل تعدد العملات وصلاحية السعر (نمرّر USD حتى لا يُصفَّر السعر عند المصدر دينار)
            (_, fx) = await MultiCurrencyFeatureGate.ApplyForWriteAsync(
                context, AccountingCurrency.USD, fx, "صيرفة");

            if (fx <= 0)
                throw new InvalidOperationException("أدخل سعر صرف صالحاً أو سجّل سعراً من شاشة أسعار الصرف");

            var normalizedFrom = fromCurrency == AccountingCurrency.USD
                ? AccountingCurrencyHelper.RoundUsd(fromAmount)
                : AccountingCurrencyHelper.RoundIqd(fromAmount);

            if (fromBox.Balance < normalizedFrom)
                throw new InvalidOperationException(
                    $"رصيد قاصة المصدر غير كافٍ (المتاح: {AccountingCurrencyHelper.Format(fromBox.Balance, fromCurrency)})");

            var toAmount = AccountingCurrencyHelper.Convert(normalizedFrom, fromCurrency, toCurrency, fx);

            var username = _currentUserService.Username ?? "system";
            fromBox.Balance -= normalizedFrom;
            fromBox.UpdatedBy = username;
            fromBox.UpdatedAt = DateTime.UtcNow;

            toBox.Balance += toAmount;
            toBox.UpdatedBy = username;
            toBox.UpdatedAt = DateTime.UtcNow;

            var exchange = new CurrencyExchange
            {
                FromCashBoxId = fromCashBoxId,
                ToCashBoxId = toCashBoxId,
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                FromAmount = normalizedFrom,
                ToAmount = toAmount,
                FxRate = AccountingCurrencyHelper.RoundIqd(fx),
                Date = date,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                CreatedBy = username,
                CreatedAt = DateTime.UtcNow
            };

            context.CurrencyExchanges.Add(exchange);
            await context.SaveChangesAsync(ct);

            if (_currentUserService.UserId.HasValue)
            {
                context.AuditLogs.Add(new AuditLog
                {
                    UserId = _currentUserService.UserId.Value,
                    Action = AuditAction.Add,
                    EntityName = "CurrencyExchange",
                    EntityId = exchange.Id,
                    NewValues =
                        $"صيرفة: {AccountingCurrencyHelper.Format(normalizedFrom, fromCurrency)} → {AccountingCurrencyHelper.Format(toAmount, toCurrency)} @ {fx:N0}",
                    Timestamp = DateTime.UtcNow
                });
                await context.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
            return exchange;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task ReverseAsync(int exchangeId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        await using var tx = await context.Database.BeginTransactionAsync(ct);
        try
        {
            var exchange = await context.CurrencyExchanges
                .FirstOrDefaultAsync(e => e.Id == exchangeId, ct)
                ?? throw new InvalidOperationException("عملية الصيرفة غير موجودة");

            var fromBox = await context.CashBoxes.FirstOrDefaultAsync(c => c.Id == exchange.FromCashBoxId, ct)
                ?? throw new InvalidOperationException("قاصة المصدر غير موجودة");
            var toBox = await context.CashBoxes.FirstOrDefaultAsync(c => c.Id == exchange.ToCashBoxId, ct)
                ?? throw new InvalidOperationException("قاصة الوجهة غير موجودة");

            if (toBox.Balance < exchange.ToAmount)
                throw new InvalidOperationException("لا يمكن التراجع — رصيد قاصة الوجهة غير كافٍ لإرجاع المبلغ");

            var username = _currentUserService.Username ?? "system";
            toBox.Balance -= exchange.ToAmount;
            toBox.UpdatedBy = username;
            toBox.UpdatedAt = DateTime.UtcNow;

            fromBox.Balance += exchange.FromAmount;
            fromBox.UpdatedBy = username;
            fromBox.UpdatedAt = DateTime.UtcNow;

            exchange.MarkSoftDeleted(username);
            await context.SaveChangesAsync(ct);

            if (_currentUserService.UserId.HasValue)
            {
                context.AuditLogs.Add(new AuditLog
                {
                    UserId = _currentUserService.UserId.Value,
                    Action = AuditAction.Delete,
                    EntityName = "CurrencyExchange",
                    EntityId = exchange.Id,
                    OldValues = $"تراجع صيرفة #{exchange.Id}",
                    Timestamp = DateTime.UtcNow
                });
                await context.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<(IReadOnlyList<CurrencyExchangeListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? cashBoxId = null,
        string? search = null,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var query = context.CurrencyExchanges.AsNoTracking()
            .Include(e => e.FromCashBox)
            .Include(e => e.ToCashBox)
            .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(e => e.Date >= fromDate.Value.Date);
        if (toDate.HasValue)
        {
            var toExclusive = toDate.Value.Date.AddDays(1);
            query = query.Where(e => e.Date < toExclusive);
        }

        if (cashBoxId is > 0)
            query = query.Where(e => e.FromCashBoxId == cashBoxId || e.ToCashBoxId == cashBoxId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e =>
                (e.Notes != null && e.Notes.Contains(term))
                || e.FromCashBox.Name.Contains(term)
                || e.ToCashBox.Name.Contains(term));
        }

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Id)
            .Skip(Math.Max(0, (page - 1) * pageSize))
            .Take(pageSize)
            .ToListAsync(ct);

        var items = rows.Select(e => new CurrencyExchangeListItem
        {
            Id = e.Id,
            Date = e.Date,
            FromCashBoxName = e.FromCashBox.Name,
            ToCashBoxName = e.ToCashBox.Name,
            FromCurrencyLabel = AccountingCurrencyHelper.GetDisplayName(e.FromCurrency),
            ToCurrencyLabel = AccountingCurrencyHelper.GetDisplayName(e.ToCurrency),
            FromAmount = e.FromAmount,
            ToAmount = e.ToAmount,
            FxRate = e.FxRate,
            Notes = e.Notes,
            DirectionDisplay =
                $"{AccountingCurrencyHelper.Format(e.FromAmount, e.FromCurrency)} → {AccountingCurrencyHelper.Format(e.ToAmount, e.ToCurrency)}"
        }).ToList();

        return (items, total);
    }
}
