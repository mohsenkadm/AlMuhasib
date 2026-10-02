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
    private readonly IBranchContext _branchContext;

    public ExchangeRateService(
        IDbContextFactory<AppDbContext> contextFactory,
        ICurrentUserService currentUserService,
        IBranchContext branchContext)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _branchContext = branchContext;
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
        // معالج النقل قد يحفظ سعر الصرف أثناء العمل على فرع غير فرع السجل — تجاوز الحارس.
        context.BypassBranchFilter = true;

        var username = _currentUserService.Username;
        var day = rate.RateDate.Date;
        var branchId = rate.BranchId > 0
            ? rate.BranchId
            : _branchContext.CurrentBranchId
              ?? await context.Branches.AsNoTracking()
                  .Where(b => b.IsMain && !b.IsDeleted)
                  .Select(b => (int?)b.Id)
                  .FirstOrDefaultAsync(ct)
              ?? 1;

        var existingSameDay = await context.ExchangeRates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => !r.IsDeleted && r.BranchId == branchId && r.RateDate.Date == day, ct);

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
            BranchId = branchId,
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
        context.BypassBranchFilter = true;
        var existing = await context.ExchangeRates.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct)
            ?? throw new InvalidOperationException("سعر الصرف غير موجود");

        existing.IsDeleted = true;
        existing.DeletedAt = DateTime.UtcNow;
        existing.DeletedBy = _currentUserService.Username;
        await context.SaveChangesAsync(ct);
    }
}
