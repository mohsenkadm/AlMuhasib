using AlMuhasib.Core.Entities.RealEstate;
using AlMuhasib.Core.Models.RealEstate;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IRealEstateContractService
{
    Task<(IReadOnlyList<RealEstateContractListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        RealEstateContractFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RealEstateContractListItem>> GetAllForExportAsync(
        RealEstateContractFilter filter,
        CancellationToken cancellationToken = default);

    Task<RealEstateContract?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<RealEstateContract> CreateAsync(RealEstateContract contract, CancellationToken cancellationToken = default);
    Task<RealEstateContract> UpdateAsync(RealEstateContract contract, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default);
    Task<RealEstateContract> RecordPaymentAsync(int contractId, decimal amount, DateTime paymentDate, string? notes, CancellationToken cancellationToken = default);
    Task<RealEstateContractDashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RealEstateDebtItem>> GetDebtsAsync(bool overdueOnly = false, CancellationToken cancellationToken = default);
}

public interface IRealEstateContractReportService
{
    Task<RealEstateContractReportData> GetReportAsync(RealEstateContractFilter filter, CancellationToken cancellationToken = default);
    Task<RealEstateProfitReportData> GetProfitReportAsync(DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken = default);
}

public interface IRealEstateExpenseService
{
    Task<IReadOnlyList<RealEstateExpenseType>> GetTypesAsync(CancellationToken cancellationToken = default);
    Task EnsureDefaultTypesAsync(CancellationToken cancellationToken = default);
    Task<RealEstateExpenseType> SaveTypeAsync(RealEstateExpenseType type, CancellationToken cancellationToken = default);
    Task DeleteTypeAsync(int id, string deletedBy, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<RealEstateExpenseListItem> Items, int TotalCount, decimal TotalAmount)> GetPagedAsync(
        int page, int pageSize, RealEstateExpenseFilter filter, CancellationToken cancellationToken = default);
    Task<RealEstateExpense?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<RealEstateExpense> SaveAsync(RealEstateExpense expense, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
}

public interface IRealEstateClauseTemplateService
{
    Task<IReadOnlyList<RealEstateClauseTemplate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RealEstateClauseTemplate>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<RealEstateClauseTemplate> SaveAsync(RealEstateClauseTemplate template, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default);
    Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);
}

public interface IRealEstatePartyService
{
    Task<(IReadOnlyList<RealEstatePartyListItem> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, CancellationToken cancellationToken = default);
    Task<RealEstateParty?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<RealEstateParty> SaveAsync(RealEstateParty party, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default);
}

public interface IRealEstateContractPrintService
{
    void PrintContract(RealEstateContract contract, int copies = 1);
    void PrintContracts(IEnumerable<RealEstateContract> contracts, int copiesEach = 1);
}
