using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.RealEstate;

[ApiController]
[Route("api/real-estate/expenses")]
[Authorize(Policy = "Tenant")]
public sealed class RealEstateExpensesController : RealEstateApiControllerBase
{
    public RealEstateExpensesController(CloudDbContext db, ITenantContext tenantContext) : base(db, tenantContext) { }

    [HttpGet("types")]
    public async Task<ActionResult<List<RealEstateExpenseTypeDto>>> GetTypes(CancellationToken ct)
    {
        if (await EnsureRealEstateTenantAsync(ct) is { } err) return err;

        var types = await Db.RealEstateExpenseTypes.AsNoTracking()
            .Where(t => t.TenantId == TenantId)
            .OrderBy(t => t.Name)
            .Select(t => new RealEstateExpenseTypeDto
            {
                SyncId = t.SyncId,
                Name = t.Name,
                Notes = t.Notes,
                IsActive = t.IsActive
            })
            .ToListAsync(ct);

        return Ok(types);
    }

    [HttpPost("types")]
    public async Task<ActionResult<object>> CreateType([FromBody] SaveRealEstateExpenseTypeRequest request, CancellationToken ct)
    {
        if (await EnsureRealEstateTenantAsync(ct) is { } err) return err;
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required" });

        var entity = new CloudRealEstateExpenseType
        {
            TenantId = TenantId,
            SyncId = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Notes = request.Notes?.Trim() ?? string.Empty,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Mobile"
        };
        Db.RealEstateExpenseTypes.Add(entity);
        await Db.SaveChangesAsync(ct);
        return Ok(new { syncId = entity.SyncId });
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetExpenses(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        [FromQuery] Guid? typeSyncId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (await EnsureRealEstateTenantAsync(ct) is { } err) return err;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = Db.RealEstateExpenses.AsNoTracking()
            .Include(e => e.ExpenseType)
            .Include(e => e.RelatedContract)
            .Where(e => e.TenantId == TenantId);

        if (from.HasValue) query = query.Where(e => e.ExpenseDate >= from.Value.Date);
        if (to.HasValue) query = query.Where(e => e.ExpenseDate <= to.Value.Date);
        if (typeSyncId.HasValue && typeSyncId != Guid.Empty)
            query = query.Where(e => e.ExpenseType.SyncId == typeSyncId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e =>
                e.Description.Contains(term) ||
                e.Notes.Contains(term) ||
                e.ExpenseType.Name.Contains(term) ||
                (e.RelatedContract != null && e.RelatedContract.ContractNumber.Contains(term)));
        }

        var totalCount = await query.CountAsync(ct);
        var totalAmount = await query.SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;
        var items = await query
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new RealEstateExpenseDto
            {
                SyncId = e.SyncId,
                ExpenseDate = e.ExpenseDate,
                Amount = e.Amount,
                Description = e.Description,
                Notes = e.Notes,
                ExpenseTypeSyncId = e.ExpenseType.SyncId,
                ExpenseTypeName = e.ExpenseType.Name,
                RelatedContractSyncId = e.RelatedContract != null ? e.RelatedContract.SyncId : null,
                RelatedContractNumber = e.RelatedContract != null ? e.RelatedContract.ContractNumber : string.Empty
            })
            .ToListAsync(ct);

        return Ok(new { items, totalCount, totalAmount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateExpense([FromBody] SaveRealEstateExpenseRequest request, CancellationToken ct)
    {
        if (await EnsureRealEstateTenantAsync(ct) is { } err) return err;
        if (request.Amount <= 0) return BadRequest(new { message = "Amount must be positive" });

        var type = await Db.RealEstateExpenseTypes
            .FirstOrDefaultAsync(t => t.TenantId == TenantId && t.SyncId == request.ExpenseTypeSyncId, ct);
        if (type is null) return BadRequest(new { message = "Expense type not found" });

        int? contractId = null;
        if (request.RelatedContractSyncId.HasValue && request.RelatedContractSyncId != Guid.Empty)
        {
            var contract = await Db.RealEstateContracts
                .FirstOrDefaultAsync(c => c.TenantId == TenantId && c.SyncId == request.RelatedContractSyncId.Value, ct);
            if (contract is null) return BadRequest(new { message = "Related contract not found" });
            contractId = contract.Id;
        }

        var entity = new CloudRealEstateExpense
        {
            TenantId = TenantId,
            SyncId = Guid.NewGuid(),
            ExpenseTypeId = type.Id,
            ExpenseDate = request.ExpenseDate.Date,
            Amount = request.Amount,
            Description = request.Description?.Trim() ?? string.Empty,
            Notes = request.Notes?.Trim() ?? string.Empty,
            RelatedContractId = contractId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Mobile"
        };
        Db.RealEstateExpenses.Add(entity);
        await Db.SaveChangesAsync(ct);
        return Ok(new { syncId = entity.SyncId });
    }

    [HttpPut("{syncId:guid}")]
    public async Task<IActionResult> UpdateExpense(Guid syncId, [FromBody] SaveRealEstateExpenseRequest request, CancellationToken ct)
    {
        if (await EnsureRealEstateTenantAsync(ct) is { } err) return err;

        var entity = await Db.RealEstateExpenses
            .FirstOrDefaultAsync(e => e.TenantId == TenantId && e.SyncId == syncId, ct);
        if (entity is null) return NotFound();

        var type = await Db.RealEstateExpenseTypes
            .FirstOrDefaultAsync(t => t.TenantId == TenantId && t.SyncId == request.ExpenseTypeSyncId, ct);
        if (type is null) return BadRequest(new { message = "Expense type not found" });

        int? contractId = null;
        if (request.RelatedContractSyncId.HasValue && request.RelatedContractSyncId != Guid.Empty)
        {
            var contract = await Db.RealEstateContracts
                .FirstOrDefaultAsync(c => c.TenantId == TenantId && c.SyncId == request.RelatedContractSyncId.Value, ct);
            if (contract is null) return BadRequest(new { message = "Related contract not found" });
            contractId = contract.Id;
        }

        entity.ExpenseTypeId = type.Id;
        entity.ExpenseDate = request.ExpenseDate.Date;
        entity.Amount = request.Amount;
        entity.Description = request.Description?.Trim() ?? string.Empty;
        entity.Notes = request.Notes?.Trim() ?? string.Empty;
        entity.RelatedContractId = contractId;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "Mobile";
        await Db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{syncId:guid}")]
    public async Task<IActionResult> DeleteExpense(Guid syncId, CancellationToken ct)
    {
        if (await EnsureRealEstateTenantAsync(ct) is { } err) return err;

        var entity = await Db.RealEstateExpenses
            .FirstOrDefaultAsync(e => e.TenantId == TenantId && e.SyncId == syncId, ct);
        if (entity is null) return NotFound();

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = "Mobile";
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "Mobile";
        await Db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public sealed class RealEstateExpenseTypeDto
{
    public Guid SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class RealEstateExpenseDto
{
    public Guid SyncId { get; set; }
    public DateTime ExpenseDate { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public Guid ExpenseTypeSyncId { get; set; }
    public string ExpenseTypeName { get; set; } = string.Empty;
    public Guid? RelatedContractSyncId { get; set; }
    public string RelatedContractNumber { get; set; } = string.Empty;
}

public sealed class SaveRealEstateExpenseTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SaveRealEstateExpenseRequest
{
    public Guid ExpenseTypeSyncId { get; set; }
    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public Guid? RelatedContractSyncId { get; set; }
}
