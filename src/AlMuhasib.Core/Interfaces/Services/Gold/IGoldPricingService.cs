using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Models.Gold;

namespace AlMuhasib.Core.Interfaces.Services.Gold;

public interface IGoldPricingService
{
    Task<IReadOnlyList<GoldKarat>> GetKaratsAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<GoldKarat> SaveKaratAsync(GoldKarat karat, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoldMithqalPriceRow>> GetPricesAsync(
        DateTime? date = null,
        int? karatValue = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoldMithqalPriceRow>> GetLatestPricesAsync(CancellationToken cancellationToken = default);
    Task<GoldMithqalPrice> SavePriceAsync(GoldMithqalPrice price, CancellationToken cancellationToken = default);
    Task DeletePriceAsync(int id, string deletedBy, CancellationToken cancellationToken = default);

    Task<GoldFxRate?> GetLatestFxRateAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GoldFxRate>> GetFxRatesAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default);
    Task<GoldFxRate> SaveFxRateAsync(GoldFxRate rate, CancellationToken cancellationToken = default);

    Task<GoldPricingQuote> QuoteAsync(
        int karatValue,
        decimal weightGrams,
        decimal makingCharge,
        GoldCurrency pricingCurrency,
        decimal? mithqalPriceOverride = null,
        decimal? fxRateOverride = null,
        CancellationToken cancellationToken = default);
}
