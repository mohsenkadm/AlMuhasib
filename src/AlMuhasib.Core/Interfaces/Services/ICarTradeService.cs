using AlMuhasib.Core.Entities.CarTrade;
using AlMuhasib.Core.Models.CarTrade;

namespace AlMuhasib.Core.Interfaces.Services;

public interface ICarTradeService
{
    Task<(IReadOnlyList<CarTradeListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CarTradeFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarTradeListItem>> GetAllForExportAsync(
        CarTradeFilter filter,
        CancellationToken cancellationToken = default);

    Task<CarTradeTransaction?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CarTradeTransaction> CreateAsync(CarTradeTransaction transaction, CancellationToken cancellationToken = default);
    Task<CarTradeTransaction> UpdateAsync(CarTradeTransaction transaction, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default);
    Task<CarTradeTransaction> RecordPaymentAsync(int transactionId, decimal amount, DateTime paymentDate, string? notes, CancellationToken cancellationToken = default);
    Task<CarTradeDashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetPartyNamesAsync(string? search, CancellationToken cancellationToken = default);
    Task<CarTradePartyStatementData> GetPartyStatementAsync(CarTradePartyStatementFilter filter, CancellationToken cancellationToken = default);
}

public interface ICarTradeReportService
{
    Task<CarTradeReportData> GetReportAsync(CarTradeFilter filter, CancellationToken cancellationToken = default);
}

public interface ICarTradePrintService
{
    void PrintTransaction(CarTradeTransaction transaction, int copies = 1);
    void PrintPaymentReceipt(CarTradeTransaction transaction, CarTradePayment payment, int copies = 1);
    void PrintTransactions(IEnumerable<CarTradeTransaction> transactions, int copiesEach = 1);
}
