using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Car;
using AlMuhasib.Infrastructure.Data.Car;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class CarContractReportService : ICarContractReportService
{
    private readonly IDbContextFactory<CarDbContext> _contextFactory;
    private readonly ICarContractService _contractService;

    public CarContractReportService(
        IDbContextFactory<CarDbContext> contextFactory,
        ICarContractService contractService)
    {
        _contextFactory = contextFactory;
        _contractService = contractService;
    }

    public async Task<CarContractReportData> GetReportAsync(CarContractFilter filter, CancellationToken cancellationToken = default)
    {
        var rows = await _contractService.GetAllForExportAsync(filter, cancellationToken);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var contracts = await context.CarSaleContracts
            .Where(c => rows.Select(r => r.Id).Contains(c.Id))
            .ToListAsync(cancellationToken);

        return new CarContractReportData
        {
            Rows = rows.ToList(),
            TotalCarValue = rows.Sum(r => r.CarPrice),
            TotalReceived = rows.Sum(r => r.AmountReceived),
            TotalRemaining = rows.Sum(r => r.RemainingAmount),
            MonthlyContracts = contracts
                .GroupBy(c => new DateTime(c.ContractDate.Year, c.ContractDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .ToList(),
            CollectedVsRemaining =
            [
                new NameAmountPoint { Name = "المحصّل", Amount = rows.Sum(r => r.AmountReceived) },
                new NameAmountPoint { Name = "المتبقي", Amount = rows.Sum(r => r.RemainingAmount) }
            ],
            ByCarType = contracts
                .GroupBy(c => string.IsNullOrWhiteSpace(c.CarType) ? "غير محدد" : c.CarType)
                .OrderByDescending(g => g.Count())
                .Select(g => new NameCountPoint { Name = g.Key, Count = g.Count() })
                .ToList()
        };
    }
}
