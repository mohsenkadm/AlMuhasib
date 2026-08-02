using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldSettingsService : IGoldSettingsService
{
    private static readonly (int Value, string Name, decimal Purity, int Order)[] DefaultKarats =
    [
        (24, "عيار 24", 1.000m, 1),
        (22, "عيار 22", 0.916m, 2),
        (21, "عيار 21", 0.875m, 3),
        (18, "عيار 18", 0.750m, 4)
    ];

    private readonly IDbContextFactory<GoldDbContext> _contextFactory;

    public GoldSettingsService(IDbContextFactory<GoldDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var settings = await context.GoldSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == GoldSettings.SingletonId, cancellationToken);
        if (settings is null)
            return false;

        var hasKarats = await context.GoldKarats.AnyAsync(cancellationToken);
        var hasCash = await context.GoldCashBoxes.AnyAsync(cancellationToken);
        return hasKarats && hasCash;
    }

    public async Task<GoldSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await EnsureSettingsAsync(context, cancellationToken);
    }

    public async Task<GoldSettings> SaveSettingsAsync(GoldSettings settings, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await EnsureSettingsAsync(context, cancellationToken);

        existing.MithqalGrams = settings.MithqalGrams <= 0 ? 5m : settings.MithqalGrams;
        existing.ScaleComPort = settings.ScaleComPort ?? string.Empty;
        existing.ScaleBaudRate = settings.ScaleBaudRate <= 0 ? 9600 : settings.ScaleBaudRate;
        existing.ScaleStabilityThresholdGrams = settings.ScaleStabilityThresholdGrams <= 0
            ? 0.01m
            : settings.ScaleStabilityThresholdGrams;
        existing.AllowManualWeightEdit = settings.AllowManualWeightEdit;
        existing.LowStockAlertGrams = settings.LowStockAlertGrams < 0 ? 0 : settings.LowStockAlertGrams;
        existing.OverdueDaysThreshold = settings.OverdueDaysThreshold <= 0 ? 30 : settings.OverdueDaysThreshold;
        existing.EnabledKaratsCsv = string.IsNullOrWhiteSpace(settings.EnabledKaratsCsv)
            ? "24,22,21,18"
            : settings.EnabledKaratsCsv;

        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task EnsureDefaultsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureSettingsAsync(context, cancellationToken);
        await EnsureDefaultKaratsAsync(context, cancellationToken);
        await EnsureDefaultCashBoxesAsync(context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    internal static async Task<GoldSettings> EnsureSettingsAsync(GoldDbContext context, CancellationToken cancellationToken)
    {
        var settings = await context.GoldSettings
            .FirstOrDefaultAsync(s => s.Id == GoldSettings.SingletonId, cancellationToken);

        if (settings is not null)
            return settings;

        settings = new GoldSettings { Id = GoldSettings.SingletonId };
        await context.GoldSettings.AddAsync(settings, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static async Task EnsureDefaultKaratsAsync(GoldDbContext context, CancellationToken cancellationToken)
    {
        var existing = await context.GoldKarats
            .IgnoreQueryFilters()
            .Select(k => k.KaratValue)
            .ToListAsync(cancellationToken);
        var existingSet = existing.ToHashSet();

        foreach (var (value, name, purity, order) in DefaultKarats)
        {
            if (existingSet.Contains(value))
                continue;

            await context.GoldKarats.AddAsync(new GoldKarat
            {
                KaratValue = value,
                Name = name,
                PurityFactor = purity,
                DisplayOrder = order,
                IsActive = true
            }, cancellationToken);
        }
    }

    private static async Task EnsureDefaultCashBoxesAsync(GoldDbContext context, CancellationToken cancellationToken)
    {
        var boxes = await context.GoldCashBoxes.ToListAsync(cancellationToken);

        if (!boxes.Any(b => b.Currency == Core.Enums.Gold.GoldCurrency.IQD && b.IsDefault))
        {
            var iqd = boxes.FirstOrDefault(b => b.Currency == Core.Enums.Gold.GoldCurrency.IQD);
            if (iqd is null)
            {
                await context.GoldCashBoxes.AddAsync(new GoldCashBox
                {
                    Name = "صندوق الدينار",
                    Currency = Core.Enums.Gold.GoldCurrency.IQD,
                    IsDefault = true,
                    IsActive = true,
                    Balance = 0
                }, cancellationToken);
            }
            else
            {
                iqd.IsDefault = true;
                iqd.IsActive = true;
            }
        }

        if (!boxes.Any(b => b.Currency == Core.Enums.Gold.GoldCurrency.USD && b.IsDefault))
        {
            var usd = boxes.FirstOrDefault(b => b.Currency == Core.Enums.Gold.GoldCurrency.USD);
            if (usd is null)
            {
                await context.GoldCashBoxes.AddAsync(new GoldCashBox
                {
                    Name = "صندوق الدولار",
                    Currency = Core.Enums.Gold.GoldCurrency.USD,
                    IsDefault = true,
                    IsActive = true,
                    Balance = 0
                }, cancellationToken);
            }
            else
            {
                usd.IsDefault = true;
                usd.IsActive = true;
            }
        }
    }
}
