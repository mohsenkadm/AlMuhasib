using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface ICurrencyExchangeService
{
    Task<CurrencyExchange> CreateAsync(
        int fromCashBoxId,
        int toCashBoxId,
        decimal fromAmount,
        decimal? fxRateOverride,
        DateTime date,
        string? notes,
        CancellationToken ct = default);

    Task ReverseAsync(int exchangeId, CancellationToken ct = default);

    Task<(IReadOnlyList<CurrencyExchangeListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? cashBoxId = null,
        string? search = null,
        CancellationToken ct = default);
}

public sealed class CurrencyExchangeListItem
{
    public int Id { get; init; }
    public DateTime Date { get; init; }
    public string FromCashBoxName { get; init; } = string.Empty;
    public string ToCashBoxName { get; init; } = string.Empty;
    public string FromCurrencyLabel { get; init; } = string.Empty;
    public string ToCurrencyLabel { get; init; } = string.Empty;
    public decimal FromAmount { get; init; }
    public decimal ToAmount { get; init; }
    public decimal FxRate { get; init; }
    public string? Notes { get; init; }
    public string DirectionDisplay { get; init; } = string.Empty;
}
