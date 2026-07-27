using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.RealEstate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.RealEstate;

[ApiController]
[Route("api/real-estate/reports")]
[Authorize(Policy = "Tenant")]
public sealed class RealEstateReportsController : RealEstateApiControllerBase
{
    public RealEstateReportsController(CloudDbContext db, ITenantContext tenantContext) : base(db, tenantContext) { }

    [HttpGet("contracts")]
    public async Task<ActionResult<RealEstateContractsReportDto>> GetContractsReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? status,
        [FromQuery] string? contractType,
        [FromQuery] string? propertyType,
        CancellationToken ct = default)
    {
        if (await EnsureRealEstateTenantAsync(ct) is { } err) return err;

        var query = Db.RealEstateContracts.AsNoTracking().Where(c => c.TenantId == TenantId);

        if (from.HasValue)
            query = query.Where(c => c.ContractDate >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(c => c.ContractDate <= to.Value.Date);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RealEstateContractStatus>(status, true, out var statusEnum))
            query = query.Where(c => c.Status == statusEnum);
        if (!string.IsNullOrWhiteSpace(contractType) && Enum.TryParse<RealEstateContractType>(contractType, true, out var typeEnum))
            query = query.Where(c => c.ContractType == typeEnum);
        if (!string.IsNullOrWhiteSpace(propertyType) && Enum.TryParse<RealEstatePropertyType>(propertyType, true, out var propEnum))
            query = query.Where(c => c.PropertyType == propEnum);

        var items = await query
            .OrderByDescending(c => c.ContractDate)
            .ThenByDescending(c => c.Id)
            .ToListAsync(ct);

        var rows = items.Select(RealEstateContractMapper.ToListItem).ToList();

        return Ok(new RealEstateContractsReportDto
        {
            Rows = rows,
            ContractCount = items.Count,
            TotalValue = items.Sum(c => c.TotalPrice),
            TotalReceived = items.Sum(c => c.AmountPaid),
            TotalRemaining = items.Sum(c => c.RemainingAmount),
            MonthlyContracts = items
                .GroupBy(c => new DateTime(c.ContractDate.Year, c.ContractDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .ToList(),
            CollectedVsRemaining =
            [
                new NameAmountPoint { Name = "Collected", Amount = items.Sum(c => c.AmountPaid) },
                new NameAmountPoint { Name = "Remaining", Amount = items.Sum(c => c.RemainingAmount) }
            ],
            ByPropertyType = items
                .GroupBy(c => c.PropertyType.ToString())
                .OrderByDescending(g => g.Count())
                .Select(g => new NameCountPoint { Name = g.Key, Count = g.Count() })
                .ToList(),
            ByContractType = items
                .GroupBy(c => c.ContractType.ToString())
                .OrderByDescending(g => g.Count())
                .Select(g => new NameCountPoint { Name = g.Key, Count = g.Count() })
                .ToList()
        });
    }
}

public sealed class RealEstateContractsReportDto
{
    public List<RealEstateContractListDto> Rows { get; set; } = [];
    public int ContractCount { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal TotalRemaining { get; set; }
    public List<NameCountPoint> MonthlyContracts { get; set; } = [];
    public List<NameAmountPoint> CollectedVsRemaining { get; set; } = [];
    public List<NameCountPoint> ByPropertyType { get; set; } = [];
    public List<NameCountPoint> ByContractType { get; set; } = [];
}
