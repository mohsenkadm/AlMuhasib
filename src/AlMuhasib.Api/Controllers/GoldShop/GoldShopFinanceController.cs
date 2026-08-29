using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Cloud.Infrastructure.Services.Gold;
using AlMuhasib.Core.Enums.Gold;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.GoldShop;

[ApiController]
[Route("api/gold-shop")]
[Authorize(Policy = "Tenant")]
public sealed class GoldShopFinanceController : GoldShopApiControllerBase
{
    private readonly CloudGoldOpsHelper _ops;

    public GoldShopFinanceController(CloudDbContext db, ITenantContext tenantContext, CloudGoldOpsHelper ops)
        : base(db, tenantContext) => _ops = ops;

    [HttpGet("cash-boxes")]
    public async Task<ActionResult<List<GoldCashBoxDto>>> GetCashBoxes(CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        var items = await Db.GoldCashBoxes.AsNoTracking()
            .Where(c => c.TenantId == TenantId && c.IsActive)
            .OrderBy(c => c.Currency).ThenBy(c => c.Name)
            .Select(c => new GoldCashBoxDto
            {
                Id = c.Id,
                SyncId = c.SyncId,
                Name = c.Name,
                Currency = c.Currency.ToString(),
                Balance = c.Balance,
                IsDefault = c.IsDefault
            }).ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("expenses")]
    public async Task<ActionResult<GoldExpensesListDto>> GetExpenses(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var q = Db.GoldExpenses.AsNoTracking().Include(e => e.ExpenseType)
            .Where(e => e.TenantId == TenantId);
        if (from.HasValue) q = q.Where(e => e.ExpenseDate >= from.Value.Date);
        if (to.HasValue) q = q.Where(e => e.ExpenseDate <= to.Value.Date);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(e => e.ExpenseDate).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new GoldExpenseDto
            {
                Id = e.Id,
                SyncId = e.SyncId,
                ExpenseDate = e.ExpenseDate,
                ExpenseTypeName = e.ExpenseType != null ? e.ExpenseType.Name : "—",
                Amount = e.Amount,
                Currency = e.Currency.ToString(),
                CashBoxId = e.CashBoxId,
                Notes = e.Notes
            }).ToListAsync(ct);
        return Ok(new GoldExpensesListDto { TotalCount = total, Items = items });
    }

    [HttpGet("expense-types")]
    public async Task<ActionResult<List<GoldExpenseTypeDto>>> GetExpenseTypes(CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        var items = await Db.GoldExpenseTypes.AsNoTracking()
            .Where(t => t.TenantId == TenantId && t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new GoldExpenseTypeDto { Id = t.Id, Name = t.Name })
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost("expenses")]
    public async Task<ActionResult<GoldExpenseDto>> CreateExpense([FromBody] GoldCreateExpenseDto request, CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        try
        {
            var username = User.Identity?.Name ?? "mobile";
            var currency = Enum.TryParse<GoldCurrency>(request.Currency, true, out var c) ? c : GoldCurrency.IQD;
            var created = await _ops.CreateExpenseAsync(TenantId, new CloudGoldCreateExpenseRequest
            {
                ExpenseDate = request.ExpenseDate,
                ExpenseTypeId = request.ExpenseTypeId,
                Amount = request.Amount,
                Currency = currency,
                CashBoxId = request.CashBoxId,
                WarehouseId = request.WarehouseId,
                Notes = request.Notes ?? string.Empty
            }, username, ct);
            var typeName = await Db.GoldExpenseTypes.AsNoTracking()
                .Where(t => t.Id == created.ExpenseTypeId).Select(t => t.Name).FirstOrDefaultAsync(ct) ?? "—";
            return Ok(new GoldExpenseDto
            {
                Id = created.Id,
                SyncId = created.SyncId,
                ExpenseDate = created.ExpenseDate,
                ExpenseTypeName = typeName,
                Amount = created.Amount,
                Currency = created.Currency.ToString(),
                CashBoxId = created.CashBoxId,
                Notes = created.Notes
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

public sealed class GoldCashBoxDto
{
    public int Id { get; set; }
    public Guid SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class GoldExpensesListDto
{
    public int TotalCount { get; set; }
    public List<GoldExpenseDto> Items { get; set; } = [];
}

public sealed class GoldExpenseDto
{
    public int Id { get; set; }
    public Guid SyncId { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string ExpenseTypeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int CashBoxId { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class GoldExpenseTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class GoldCreateExpenseDto
{
    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    public int ExpenseTypeId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "IQD";
    public int? CashBoxId { get; set; }
    public int? WarehouseId { get; set; }
    public string? Notes { get; set; }
}
