using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Infrastructure.Data.Gold;
using AlMuhasib.Infrastructure.Services;
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
        var settings = await FindSettingsAsync(context, cancellationToken);
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
        existing.DefaultMakingChargeMode = settings.DefaultMakingChargeMode;
        if (!string.IsNullOrWhiteSpace(settings.UpdatedBy))
            existing.UpdatedBy = settings.UpdatedBy;

        await context.SaveChangesAsync(cancellationToken);
        await SyncEnabledKaratsAsync(context, existing.EnabledKaratsCsv, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task EnsureDefaultsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await GoldDatabaseMigrationService.EnsureSchemaCurrentAsync(context, cancellationToken);
        await EnsureSettingsAsync(context, cancellationToken);
        await EnsureDefaultKaratsInternalAsync(context, cancellationToken);
        await EnsureDefaultCashBoxesInternalAsync(context, cancellationToken);
        await GoldWarehouseService.EnsureDefaultInternalAsync(context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var settings = await FindSettingsAsync(context, cancellationToken);
        if (settings is not null)
        {
            await SyncEnabledKaratsAsync(context, settings.EnabledKaratsCsv, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    internal static Task EnsureDefaultKaratsInternalAsync(GoldDbContext context, CancellationToken cancellationToken) =>
        EnsureDefaultKaratsAsync(context, cancellationToken);

    internal static Task EnsureDefaultCashBoxesInternalAsync(GoldDbContext context, CancellationToken cancellationToken) =>
        EnsureDefaultCashBoxesAsync(context, cancellationToken);

    internal static async Task<GoldSettings?> FindSettingsAsync(
        GoldDbContext context,
        CancellationToken cancellationToken) =>
        await context.GoldSettings.FirstOrDefaultAsync(cancellationToken);

    internal static async Task<GoldSettings> EnsureSettingsAsync(GoldDbContext context, CancellationToken cancellationToken)
    {
        var settings = await FindSettingsAsync(context, cancellationToken);
        if (settings is not null)
            return settings;

        settings = new GoldSettings();
        await context.GoldSettings.AddAsync(settings, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static async Task EnsureDefaultKaratsAsync(GoldDbContext context, CancellationToken cancellationToken)
    {
        var existingKarats = await context.GoldKarats
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);
        var existingByValue = existingKarats.ToDictionary(k => k.KaratValue);

        foreach (var (value, name, purity, order) in DefaultKarats)
        {
            if (existingByValue.TryGetValue(value, out var existing))
            {
                if (existing.IsDeleted)
                    existing.RestoreFromSoftDelete("System");

                existing.Name = name;
                existing.PurityFactor = purity;
                existing.DisplayOrder = order;
                existing.IsActive = true;
                continue;
            }

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

    private static async Task SyncEnabledKaratsAsync(
        GoldDbContext context,
        string enabledKaratsCsv,
        CancellationToken cancellationToken)
    {
        var enabledValues = ParseEnabledKarats(enabledKaratsCsv);
        if (enabledValues.Count == 0)
            return;

        var karats = await context.GoldKarats.IgnoreQueryFilters().ToListAsync(cancellationToken);
        foreach (var karat in karats)
        {
            var shouldBeActive = enabledValues.Contains(karat.KaratValue);
            if (shouldBeActive && karat.IsDeleted)
                karat.RestoreFromSoftDelete("System");

            karat.IsActive = shouldBeActive;
        }
    }

    internal static HashSet<int> ParseEnabledKarats(string? enabledKaratsCsv)
    {
        if (string.IsNullOrWhiteSpace(enabledKaratsCsv))
            return [24, 22, 21, 18];

        var values = new HashSet<int>();
        foreach (var part in enabledKaratsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var value) && value > 0)
                values.Add(value);
        }

        return values.Count > 0 ? values : [24, 22, 21, 18];
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
