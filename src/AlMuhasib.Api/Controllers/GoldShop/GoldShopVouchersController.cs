using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
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
    public GoldShopVouchersController(CloudDbContext db, ITenantContext tenantContext) : base(db, tenantContext) { }

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
}

public sealed class GoldVouchersListDto
{
    public int TotalCount { get; set; }
    public List<GoldVoucherListDto> Items { get; set; } = [];
}

public sealed class GoldVoucherListDto
{
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
