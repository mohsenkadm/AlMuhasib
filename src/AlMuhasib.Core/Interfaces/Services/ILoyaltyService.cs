using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.Loyalty;

namespace AlMuhasib.Core.Interfaces.Services;

public interface ILoyaltyService
{
    Task<LoyaltySettings> GetOrCreateSettingsAsync(CancellationToken ct = default);
    Task SaveSettingsAsync(LoyaltySettings settings, CancellationToken ct = default);

    Task<CustomerLoyaltyAccount?> GetAccountAsync(int customerId, CancellationToken ct = default);
    Task<CustomerLoyaltyAccount> GetOrCreateAccountAsync(int customerId, CancellationToken ct = default);
    Task<int> GetBalanceAsync(int customerId, CancellationToken ct = default);

    Task<LoyaltyQuote> QuoteAsync(
        int customerId,
        decimal invoiceBaseAmount,
        int? redeemPoints,
        PaymentMethod paymentMethod,
        CancellationToken ct = default);

    Task AdjustPointsAsync(int customerId, int pointsDelta, string note, int? userId, CancellationToken ct = default);

    Task<IReadOnlyList<LoyaltyPointTransaction>> GetLedgerAsync(
        int? customerId,
        LoyaltyTransactionType? type,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default);

    Task<IReadOnlyList<LoyaltyAccountRow>> GetAccountsAsync(string? search, CancellationToken ct = default);

    Task<LoyaltySummaryReport> GetSummaryReportAsync(DateTime? from, DateTime? to, CancellationToken ct = default);

    Task<IReadOnlyList<LoyaltyTopCustomerRow>> GetTopCustomersAsync(
        DateTime? from,
        DateTime? to,
        int take = 50,
        CancellationToken ct = default);
}
