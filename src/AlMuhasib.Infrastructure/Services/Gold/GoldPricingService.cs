using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldPricingService : IGoldPricingService
{
    private readonly IDbContextFactory<GoldDbContext> _contextFactory;

    public GoldPricingService(IDbContextFactory<GoldDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<GoldKarat>> GetKaratsAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.GoldKarats.AsNoTracking().AsQueryable();
        if (activeOnly)
            query = query.Where(k => k.IsActive);

        var karats = await query
            .OrderBy(k => k.DisplayOrder)
            .ThenBy(k => k.KaratValue)
            .ToListAsync(cancellationToken);

        var settings = await context.GoldSettings.AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
            return karats;

        var enabledValues = GoldSettingsService.ParseEnabledKarats(settings.EnabledKaratsCsv);
        var filtered = karats
            .Where(k => enabledValues.Contains(k.KaratValue))
            .ToList();
        return filtered.Count > 0 ? filtered : karats;
    }

    public async Task<GoldKarat> SaveKaratAsync(GoldKarat karat, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        if (karat.Id > 0)
        {
            var existing = await context.GoldKarats.FirstOrDefaultAsync(k => k.Id == karat.Id, cancellationToken)
                ?? throw new InvalidOperationException("العيار غير موجود");

            existing.KaratValue = karat.KaratValue;
            existing.Name = karat.Name;
            existing.PurityFactor = karat.PurityFactor;
            existing.IsActive = karat.IsActive;
            existing.DisplayOrder = karat.DisplayOrder;
            await context.SaveChangesAsync(cancellationToken);
            return existing;
        }

        await context.GoldKarats.AddAsync(karat, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return karat;
    }

    public async Task<IReadOnlyList<GoldMithqalPriceRow>> GetPricesAsync(
        DateTime? date = null,
        int? karatValue = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var mithqalGrams = await GetMithqalGramsAsync(context, cancellationToken);

        var query = context.GoldMithqalPrices.AsNoTracking().AsQueryable();
        if (date.HasValue)
            query = query.Where(p => p.PriceDate.Date == date.Value.Date);
        if (karatValue.HasValue)
            query = query.Where(p => p.KaratValue == karatValue.Value);

        var prices = await query
            .OrderByDescending(p => p.PriceDate)
            .ThenBy(p => p.KaratValue)
            .ToListAsync(cancellationToken);

        var karatNames = await context.GoldKarats.AsNoTracking()
            .ToDictionaryAsync(k => k.KaratValue, k => k.Name, cancellationToken);

        return prices.Select(p => ToRow(p, karatNames, mithqalGrams)).ToList();
    }

    public async Task<IReadOnlyList<GoldMithqalPriceRow>> GetLatestPricesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var mithqalGrams = await GetMithqalGramsAsync(context, cancellationToken);

        // One pass: load recent prices only, pick latest per karat in memory.
        var prices = await context.GoldMithqalPrices.AsNoTracking()
            .OrderByDescending(p => p.PriceDate)
            .ThenByDescending(p => p.Id)
            .Take(200)
            .ToListAsync(cancellationToken);

        if (prices.Count == 0)
            return [];

        var latest = prices
            .GroupBy(p => p.KaratValue)
            .Select(g => g.First())
            .OrderBy(p => p.KaratValue)
            .ToList();

        var karatNames = await context.GoldKarats.AsNoTracking()
            .ToDictionaryAsync(k => k.KaratValue, k => k.Name, cancellationToken);

        return latest.Select(p => ToRow(p, karatNames, mithqalGrams)).ToList();
    }

    public async Task<GoldMithqalPrice> SavePriceAsync(GoldMithqalPrice price, CancellationToken cancellationToken = default)
    {
        if (price.PricePerMithqal <= 0)
            throw new InvalidOperationException("سعر المثقال يجب أن يكون أكبر من صفر");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        if (price.Id > 0)
        {
            var existing = await context.GoldMithqalPrices.FirstOrDefaultAsync(p => p.Id == price.Id, cancellationToken)
                ?? throw new InvalidOperationException("السعر غير موجود");

            existing.PriceDate = price.PriceDate.Date;
            existing.KaratValue = price.KaratValue;
            existing.PricePerMithqal = price.PricePerMithqal;
            existing.Currency = price.Currency;
            existing.FxRateUsed = price.FxRateUsed;
            existing.Notes = price.Notes ?? string.Empty;
            await context.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var sameDay = await context.GoldMithqalPrices.FirstOrDefaultAsync(
            p => p.PriceDate.Date == price.PriceDate.Date && p.KaratValue == price.KaratValue,
            cancellationToken);

        if (sameDay is not null)
        {
            sameDay.PricePerMithqal = price.PricePerMithqal;
            sameDay.Currency = price.Currency;
            sameDay.FxRateUsed = price.FxRateUsed;
            sameDay.Notes = price.Notes ?? string.Empty;
            await context.SaveChangesAsync(cancellationToken);
            return sameDay;
        }

        price.PriceDate = price.PriceDate.Date;
        await context.GoldMithqalPrices.AddAsync(price, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return price;
    }

    public async Task DeletePriceAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var price = await context.GoldMithqalPrices.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("السعر غير موجود");

        price.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<GoldFxRate?> GetLatestFxRateAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GoldFxRates.AsNoTracking()
            .OrderByDescending(r => r.RateDate)
            .ThenByDescending(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GoldFxRate>> GetFxRatesAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.GoldFxRates.AsNoTracking().AsQueryable();
        if (dateFrom.HasValue)
            query = query.Where(r => r.RateDate.Date >= dateFrom.Value.Date);
        if (dateTo.HasValue)
            query = query.Where(r => r.RateDate.Date <= dateTo.Value.Date);

        return await query
            .OrderByDescending(r => r.RateDate)
            .ThenByDescending(r => r.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<GoldFxRate> SaveFxRateAsync(GoldFxRate rate, CancellationToken cancellationToken = default)
    {
        if (rate.UsdToIqd <= 0)
            throw new InvalidOperationException("سعر الصرف يجب أن يكون أكبر من صفر");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        if (rate.Id > 0)
        {
            var existing = await context.GoldFxRates.FirstOrDefaultAsync(r => r.Id == rate.Id, cancellationToken)
                ?? throw new InvalidOperationException("سعر الصرف غير موجود");

            existing.RateDate = rate.RateDate.Date;
            existing.UsdToIqd = rate.UsdToIqd;
            existing.Notes = rate.Notes ?? string.Empty;
            await context.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var sameDay = await context.GoldFxRates.FirstOrDefaultAsync(
            r => r.RateDate.Date == rate.RateDate.Date,
            cancellationToken);

        if (sameDay is not null)
        {
            sameDay.UsdToIqd = rate.UsdToIqd;
            sameDay.Notes = rate.Notes ?? string.Empty;
            await context.SaveChangesAsync(cancellationToken);
            return sameDay;
        }

        rate.RateDate = rate.RateDate.Date;
        await context.GoldFxRates.AddAsync(rate, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return rate;
    }

    public async Task<GoldPricingQuote> QuoteAsync(
        int karatValue,
        decimal weightGrams,
        decimal makingCharge,
        GoldCurrency pricingCurrency,
        decimal? mithqalPriceOverride = null,
        decimal? fxRateOverride = null,
        GoldMakingChargeMode makingChargeMode = GoldMakingChargeMode.Fixed,
        decimal makingChargeRate = 0,
        CancellationToken cancellationToken = default)
    {
        if (weightGrams <= 0)
            throw new InvalidOperationException("الوزن يجب أن يكون أكبر من صفر");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var mithqalGrams = await GetMithqalGramsAsync(context, cancellationToken);

        var karat = await context.GoldKarats.AsNoTracking()
            .FirstOrDefaultAsync(k => k.KaratValue == karatValue, cancellationToken);
        var purityFactor = GoldLinePricingHelper.ResolvePurityFactor(karat);

        decimal mithqalPrice;
        if (mithqalPriceOverride.HasValue && mithqalPriceOverride.Value > 0)
        {
            mithqalPrice = mithqalPriceOverride.Value;
        }
        else
        {
            var latest = await context.GoldMithqalPrices.AsNoTracking()
                .Where(p => p.KaratValue == karatValue && p.Currency == pricingCurrency)
                .OrderByDescending(p => p.PriceDate)
                .ThenByDescending(p => p.Id)
                .FirstOrDefaultAsync(cancellationToken)
                ?? await context.GoldMithqalPrices.AsNoTracking()
                    .Where(p => p.KaratValue == karatValue)
                    .OrderByDescending(p => p.PriceDate)
                    .ThenByDescending(p => p.Id)
                    .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException($"لا يوجد سعر مثقال للعيار {karatValue}");

            mithqalPrice = latest.PricePerMithqal;
        }

        var (pricePerGram, pureGrams, goldValue, resolvedMaking, lineTotal) = GoldLinePricingHelper.Calculate(
            weightGrams,
            mithqalPrice,
            mithqalGrams,
            purityFactor,
            makingChargeMode,
            makingCharge,
            makingChargeRate);

        decimal? fx = fxRateOverride;
        if (!fx.HasValue || fx.Value <= 0)
        {
            var latestFx = await context.GoldFxRates.AsNoTracking()
                .OrderByDescending(r => r.RateDate)
                .ThenByDescending(r => r.Id)
                .FirstOrDefaultAsync(cancellationToken);
            fx = latestFx?.UsdToIqd;
        }

        decimal? lineTotalIqd = null;
        decimal? lineTotalUsd = null;
        if (fx.HasValue && fx.Value > 0)
        {
            if (pricingCurrency == GoldCurrency.USD)
            {
                lineTotalUsd = lineTotal;
                lineTotalIqd = GoldCurrencyHelper.Round(lineTotal * fx.Value);
            }
            else
            {
                lineTotalIqd = lineTotal;
                lineTotalUsd = GoldCurrencyHelper.Round(lineTotal / fx.Value);
            }
        }

        return new GoldPricingQuote
        {
            KaratValue = karatValue,
            WeightGrams = weightGrams,
            PureGrams = pureGrams,
            PurityFactor = purityFactor,
            MithqalGrams = mithqalGrams,
            MithqalPrice = mithqalPrice,
            PricingCurrency = pricingCurrency,
            PricePerGram = pricePerGram,
            GoldValue = goldValue,
            MakingCharge = resolvedMaking,
            MakingChargeMode = makingChargeMode,
            MakingChargeRate = makingChargeRate,
            LineTotal = lineTotal,
            FxRate = fx,
            LineTotalIqd = lineTotalIqd,
            LineTotalUsd = lineTotalUsd
        };
    }

    private static async Task<decimal> GetMithqalGramsAsync(GoldDbContext context, CancellationToken cancellationToken)
    {
        var settings = await context.GoldSettings.AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        var grams = settings?.MithqalGrams ?? 5m;
        return grams <= 0 ? 5m : grams;
    }

    private static GoldMithqalPriceRow ToRow(
        GoldMithqalPrice price,
        IReadOnlyDictionary<int, string> karatNames,
        decimal mithqalGrams)
    {
        var grams = mithqalGrams <= 0 ? 5m : mithqalGrams;
        return new GoldMithqalPriceRow
        {
            Id = price.Id,
            PriceDate = price.PriceDate,
            KaratValue = price.KaratValue,
            KaratName = karatNames.TryGetValue(price.KaratValue, out var name) ? name : $"عيار {price.KaratValue}",
            PricePerMithqal = price.PricePerMithqal,
            Currency = price.Currency,
            FxRateUsed = price.FxRateUsed,
            PricePerGram = GoldCurrencyHelper.Round(price.PricePerMithqal / grams, 6),
            Notes = price.Notes
        };
    }
}
