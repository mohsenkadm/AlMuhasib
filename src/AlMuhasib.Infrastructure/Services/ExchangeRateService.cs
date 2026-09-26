using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class ExchangeRateService : IExchangeRateService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public ExchangeRateService(
        IDbContextFactory<AppDbContext> contextFactory,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ExchangeRate>> GetAllAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.ExchangeRates
            .OrderByDescending(r => r.RateDate)
            .ThenByDescending(r => r.Id)
            .ToListAsync(ct);
    }

    public async Task<ExchangeRate?> GetLatestAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.ExchangeRates
            .OrderByDescending(r => r.RateDate)
            .ThenByDescending(r => r.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ExchangeRate?> GetForDateAsync(DateTime date, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var day = date.Date;
        return await context.ExchangeRates
            .Where(r => r.RateDate.Date <= day)
            .OrderByDescending(r => r.RateDate)
            .ThenByDescending(r => r.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<decimal> GetUsdToIqdForDateOrLatestAsync(DateTime date, CancellationToken ct = default)
    {
        var rate = await GetForDateAsync(date, ct) ?? await GetLatestAsync(ct);
        return rate is { UsdToIqd: > 0 } ? rate.UsdToIqd : 0m;
    }

    public async Task<ExchangeRate> SaveAsync(ExchangeRate rate, CancellationToken ct = default)
    {
        if (rate.UsdToIqd <= 0)
            throw new InvalidOperationException("أدخل سعر صرف صالحاً (دولار → دينار)");

        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var username = _currentUserService.Username;
        var day = rate.RateDate.Date;

        var existingSameDay = await context.ExchangeRates
            .FirstOrDefaultAsync(r => r.RateDate.Date == day, ct);

        if (existingSameDay is not null)
        {
            existingSameDay.UsdToIqd = rate.UsdToIqd;
            existingSameDay.Notes = rate.Notes?.Trim() ?? string.Empty;
            existingSameDay.UpdatedAt = DateTime.UtcNow;
            existingSameDay.UpdatedBy = username;
            await context.SaveChangesAsync(ct);
            return existingSameDay;
        }

        var entity = new ExchangeRate
        {
            RateDate = day,
            UsdToIqd = rate.UsdToIqd,
            Notes = rate.Notes?.Trim() ?? string.Empty,
            CreatedBy = username,
            CreatedAt = DateTime.UtcNow
        };
        context.ExchangeRates.Add(entity);
        await context.SaveChangesAsync(ct);
        return entity;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var existing = await context.ExchangeRates.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new InvalidOperationException("سعر الصرف غير موجود");

        existing.IsDeleted = true;
        existing.DeletedAt = DateTime.UtcNow;
        existing.DeletedBy = _currentUserService.Username;
        await context.SaveChangesAsync(ct);
    }
}
