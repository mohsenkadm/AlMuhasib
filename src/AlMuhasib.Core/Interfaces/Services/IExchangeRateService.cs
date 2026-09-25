using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IExchangeRateService
{
    Task<IReadOnlyList<ExchangeRate>> GetAllAsync(CancellationToken ct = default);
    Task<ExchangeRate?> GetLatestAsync(CancellationToken ct = default);
    Task<ExchangeRate?> GetForDateAsync(DateTime date, CancellationToken ct = default);
    Task<decimal> GetUsdToIqdForDateOrLatestAsync(DateTime date, CancellationToken ct = default);
    Task<ExchangeRate> SaveAsync(ExchangeRate rate, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
