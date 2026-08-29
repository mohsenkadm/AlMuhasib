using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Cloud.Infrastructure.Services.Gold;
using AlMuhasib.Core.Enums.Gold;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.GoldShop;

[ApiController]
[Route("api/gold-shop/vouchers")]
[Authorize(Policy = "Tenant")]
public sealed class GoldShopVouchersController : GoldShopApiControllerBase
{
    private readonly CloudGoldOpsHelper _ops;

    public GoldShopVouchersController(CloudDbContext db, ITenantContext tenantContext, CloudGoldOpsHelper ops)
        : base(db, tenantContext) => _ops = ops;

    [HttpGet]
    public async Task<ActionResult<GoldVouchersListDto>> GetVouchers(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] GoldVoucherType? type,
        [FromQuery] int? customerId,
        [FromQuery] int? supplierId,
        [FromQuery] int? cashBoxId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = Db.GoldVouchers.AsNoTracking()
            .Where(v => v.TenantId == TenantId && !v.IsDeleted);

        if (from.HasValue) query = query.Where(v => v.VoucherDate >= from.Value.Date);
        if (to.HasValue) query = query.Where(v => v.VoucherDate <= to.Value.Date);
        if (type.HasValue) query = query.Where(v => v.VoucherType == type.Value);
        if (customerId.HasValue) query = query.Where(v => v.CustomerId == customerId.Value);
        if (supplierId.HasValue) query = query.Where(v => v.SupplierId == supplierId.Value);
        if (cashBoxId.HasValue) query = query.Where(v => v.CashBoxId == cashBoxId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(v => v.VoucherDate)
            .ThenByDescending(v => v.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new GoldVoucherListDto
            {
                Id = v.Id,
                SyncId = v.SyncId,
                VoucherNumber = v.VoucherNumber,
                VoucherDate = v.VoucherDate,
                VoucherType = v.VoucherType.ToString(),
                Currency = v.Currency.ToString(),
                Amount = v.Amount,
                CustomerId = v.CustomerId,
                SupplierId = v.SupplierId,
                CashBoxId = v.CashBoxId,
                IsOpeningBalance = v.IsOpeningBalance,
                AffectsCashBox = v.AffectsCashBox,
                Notes = v.Notes
            })
            .ToListAsync(ct);

        return Ok(new GoldVouchersListDto { TotalCount = total, Items = items });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GoldVoucherListDto>> GetVoucher(int id, CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        var v = await Db.GoldVouchers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == TenantId && x.Id == id, ct);
        if (v is null) return NotFound();
        return Ok(new GoldVoucherListDto
        {
            Id = v.Id,
            SyncId = v.SyncId,
            VoucherNumber = v.VoucherNumber,
            VoucherDate = v.VoucherDate,
            VoucherType = v.VoucherType.ToString(),
            Currency = v.Currency.ToString(),
            Amount = v.Amount,
            CustomerId = v.CustomerId,
            SupplierId = v.SupplierId,
            CashBoxId = v.CashBoxId,
            IsOpeningBalance = v.IsOpeningBalance,
            AffectsCashBox = v.AffectsCashBox,
            Notes = v.Notes
        });
    }

    [HttpPost]
    public async Task<ActionResult<GoldVoucherListDto>> CreateVoucher(
        [FromBody] GoldCreateVoucherDto request, CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        try
        {
            var username = User.Identity?.Name ?? "mobile";
            var type = Enum.TryParse<GoldVoucherType>(request.VoucherType, true, out var t) ? t : GoldVoucherType.Receipt;
            var currency = Enum.TryParse<GoldCurrency>(request.Currency, true, out var c) ? c : GoldCurrency.IQD;
            var created = await _ops.CreateVoucherAsync(TenantId, new CloudGoldCreateVoucherRequest
            {
                VoucherDate = request.VoucherDate,
                VoucherType = type,
                Currency = currency,
                Amount = request.Amount,
                CashBoxId = request.CashBoxId,
                CustomerId = request.CustomerId,
                SupplierId = request.SupplierId,
                IsOpeningBalance = request.IsOpeningBalance,
                AffectsCashBox = request.AffectsCashBox,
                Notes = request.Notes ?? string.Empty
            }, username, ct);

            return Ok(new GoldVoucherListDto
            {
                Id = created.Id,
                SyncId = created.SyncId,
                VoucherNumber = created.VoucherNumber,
                VoucherDate = created.VoucherDate,
                VoucherType = created.VoucherType.ToString(),
                Currency = created.Currency.ToString(),
                Amount = created.Amount,
                CustomerId = created.CustomerId,
                SupplierId = created.SupplierId,
                CashBoxId = created.CashBoxId,
                IsOpeningBalance = created.IsOpeningBalance,
                AffectsCashBox = created.AffectsCashBox,
                Notes = created.Notes
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

public sealed class GoldVouchersListDto
{
    public int TotalCount { get; set; }
    public List<GoldVoucherListDto> Items { get; set; } = [];
}

public sealed class GoldVoucherListDto
{
    public int Id { get; set; }
    public Guid SyncId { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; }
    public string VoucherType { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public int? CashBoxId { get; set; }
    public bool IsOpeningBalance { get; set; }
    public bool AffectsCashBox { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class GoldCreateVoucherDto
{
    public DateTime VoucherDate { get; set; } = DateTime.Today;
    public string VoucherType { get; set; } = "Receipt";
    public string Currency { get; set; } = "IQD";
    public decimal Amount { get; set; }
    public int? CashBoxId { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public bool IsOpeningBalance { get; set; }
    public bool AffectsCashBox { get; set; } = true;
    public string? Notes { get; set; }
}
