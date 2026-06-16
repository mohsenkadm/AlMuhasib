using AlMuhasib.Core.Entities.Car;
using AlMuhasib.Core.Models.Car;

namespace AlMuhasib.Core.Interfaces.Services;

public interface ICarContractService
{
    Task<(IReadOnlyList<CarContractListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CarContractFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarContractListItem>> GetAllForExportAsync(
        CarContractFilter filter,
        CancellationToken cancellationToken = default);

    Task<CarSaleContract?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CarSaleContract> CreateAsync(CarSaleContract contract, CancellationToken cancellationToken = default);
    Task<CarSaleContract> UpdateAsync(CarSaleContract contract, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default);
    Task<CarSaleContract> RecordPaymentAsync(int contractId, decimal amount, DateTime paymentDate, string? notes, CancellationToken cancellationToken = default);
    Task<CarContractDashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken = default);
}

public interface ICarContractReportService
{
    Task<CarContractReportData> GetReportAsync(CarContractFilter filter, CancellationToken cancellationToken = default);
}

public interface ICarContractPrintService
{
    void PrintContract(CarSaleContract contract, int copies = 5);
    void PrintContracts(IEnumerable<CarSaleContract> contracts, int copiesEach = 1);
}
