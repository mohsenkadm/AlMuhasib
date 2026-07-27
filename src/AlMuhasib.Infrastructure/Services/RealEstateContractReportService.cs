using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.RealEstate;
using AlMuhasib.Infrastructure.Data.RealEstate;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class RealEstateContractReportService : IRealEstateContractReportService
{
    private readonly IDbContextFactory<RealEstateDbContext> _contextFactory;
    private readonly IRealEstateContractService _contractService;

    public RealEstateContractReportService(
        IDbContextFactory<RealEstateDbContext> contextFactory,
        IRealEstateContractService contractService)
    {
        _contextFactory = contextFactory;
        _contractService = contractService;
    }

    public async Task<RealEstateContractReportData> GetReportAsync(
        RealEstateContractFilter filter,
        CancellationToken cancellationToken = default)
    {
        var rows = await _contractService.GetAllForExportAsync(filter, cancellationToken);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var contracts = await context.RealEstateContracts
            .Where(c => rows.Select(r => r.Id).Contains(c.Id))
            .ToListAsync(cancellationToken);

        return new RealEstateContractReportData
        {
            Rows = rows.ToList(),
            TotalValue = rows.Sum(r => r.TotalPrice),
            TotalReceived = rows.Sum(r => r.AmountPaid),
            TotalRemaining = rows.Sum(r => r.RemainingAmount),
            MonthlyContracts = contracts
                .GroupBy(c => new DateTime(c.ContractDate.Year, c.ContractDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .ToList(),
            CollectedVsRemaining =
            [
                new NameAmountPoint { Name = "المحصّل", Amount = rows.Sum(r => r.AmountPaid) },
                new NameAmountPoint { Name = "المتبقي", Amount = rows.Sum(r => r.RemainingAmount) }
            ],
            ByPropertyType = contracts
                .GroupBy(c => RealEstateContractService.GetPropertyTypeLabel(c.PropertyType))
                .OrderByDescending(g => g.Count())
                .Select(g => new NameCountPoint { Name = g.Key, Count = g.Count() })
                .ToList(),
            ByContractType = contracts
                .GroupBy(c => RealEstateContractService.GetContractTypeLabel(c.ContractType))
                .Select(g => new NameCountPoint { Name = g.Key, Count = g.Count() })
                .ToList()
        };
    }
}
