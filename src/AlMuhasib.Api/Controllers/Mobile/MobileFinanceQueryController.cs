using AlMuhasib.Api.Models;
using AlMuhasib.Cloud.Application.Models;
using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.Mobile;

[ApiController]
[Route("api/mobile")]
[Authorize(Policy = "Tenant")]
public sealed class MobileFinanceQueryController : ControllerBase
{
    private readonly CloudDbContext _db;
    private readonly ITenantContext _tenantContext;

    public MobileFinanceQueryController(CloudDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet("vouchers")]
    public async Task<ActionResult<PagedResult<VoucherListItem>>> GetVouchers(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] VoucherType? voucherType,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        EnsureTenant();
        var tenantId = RequireTenantId();
        var query = _db.Vouchers.AsNoTracking()
            .ForTenant(tenantId)
            .Include(v => v.Customer)
            .Include(v => v.Investor)
            .Include(v => v.CashBox)
            .Include(v => v.BankAccount)
            .AsQueryable();

        if (from.HasValue) query = query.Where(v => v.Date >= from.Value);
        if (to.HasValue) query = query.Where(v => v.Date <= to.Value);
        if (voucherType.HasValue) query = query.Where(v => v.VoucherType == voucherType.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(v =>
                EF.Functions.Like(v.VoucherNumber, term) ||
                (v.Customer != null && EF.Functions.Like(v.Customer.Name, term)) ||
                (v.Investor != null && EF.Functions.Like(v.Investor.Name, term)) ||
                (v.Notes != null && EF.Functions.Like(v.Notes, term)));
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(v => v.Date).ThenByDescending(v => v.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return Ok(new PagedResult<VoucherListItem>
        {
            Items = items.Select(v => new VoucherListItem
            {
                SyncId = v.SyncId,
                VoucherNumber = v.VoucherNumber,
                VoucherType = v.VoucherType,
                Amount = v.Amount,
                BankFees = v.BankFees,
                CustomerSyncId = v.Customer?.SyncId,
                CustomerName = v.Customer?.Name,
                InvestorSyncId = v.Investor?.SyncId,
                InvestorName = v.Investor?.Name,
                CashBoxSyncId = v.CashBox.SyncId,
                CashBoxName = v.CashBox.Name,
                BankAccountSyncId = v.BankAccount?.SyncId,
                BankAccountName = v.BankAccount?.Name,
                Date = v.Date,
                Notes = v.Notes
            }).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("expenses")]
    public async Task<ActionResult<PagedResult<ExpenseListItem>>> GetExpenses(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        EnsureTenant();
        var tenantId = RequireTenantId();
        var query = _db.Expenses.AsNoTracking()
            .ForTenant(tenantId)
            .Include(e => e.ExpenseType)
            .Include(e => e.CashBox)
            .AsQueryable();

        if (from.HasValue) query = query.Where(e => e.Date >= from.Value);
        if (to.HasValue) query = query.Where(e => e.Date <= to.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(e =>
                EF.Functions.Like(e.ExpenseType.Name, term) ||
                (e.Notes != null && EF.Functions.Like(e.Notes, term)));
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return Ok(new PagedResult<ExpenseListItem>
        {
            Items = items.Select(e => new ExpenseListItem
            {
                SyncId = e.SyncId,
                ExpenseTypeSyncId = e.ExpenseType.SyncId,
                ExpenseTypeName = e.ExpenseType.Name,
                Amount = e.Amount,
                Date = e.Date,
                CashBoxSyncId = e.CashBox.SyncId,
                CashBoxName = e.CashBox.Name,
                Notes = e.Notes
            }).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("transfers")]
    public async Task<ActionResult<PagedResult<TransferListItem>>> GetTransfers(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        EnsureTenant();
        var tenantId = RequireTenantId();
        var query = _db.Transfers.AsNoTracking().ForTenant(tenantId);
        if (from.HasValue) query = query.Where(t => t.Date >= from.Value);
        if (to.HasValue) query = query.Where(t => t.Date <= to.Value);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var cashBoxes = await _db.CashBoxes.AsNoTracking().ForTenant(tenantId).ToDictionaryAsync(c => c.Id, ct);
        var banks = await _db.BankAccounts.AsNoTracking().ForTenant(tenantId).ToDictionaryAsync(b => b.Id, ct);

        string ResolveName(TransferAccountType type, int id) =>
            type == TransferAccountType.CashBox
                ? (cashBoxes.TryGetValue(id, out var c) ? c.Name : id.ToString())
                : (banks.TryGetValue(id, out var b) ? b.Name : id.ToString());

        Guid? ResolveSync(TransferAccountType type, int id) =>
            type == TransferAccountType.CashBox
                ? (cashBoxes.TryGetValue(id, out var c) ? c.SyncId : null)
                : (banks.TryGetValue(id, out var b) ? b.SyncId : null);

        return Ok(new PagedResult<TransferListItem>
        {
            Items = items.Select(t => new TransferListItem
            {
                SyncId = t.SyncId,
                FromType = t.FromType,
                FromSyncId = ResolveSync(t.FromType, t.FromId),
                FromName = ResolveName(t.FromType, t.FromId),
                ToType = t.ToType,
                ToSyncId = ResolveSync(t.ToType, t.ToId),
                ToName = ResolveName(t.ToType, t.ToId),
                Amount = t.Amount,
                Date = t.Date,
                Notes = t.Notes
            }).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("warehouse-stocks")]
    public async Task<ActionResult<PagedResult<WarehouseStockListItem>>> GetWarehouseStocks(
        [FromQuery] Guid? warehouseSyncId,
        [FromQuery] Guid? productSyncId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken ct = default)
    {
        EnsureTenant();
        var tenantId = RequireTenantId();
        var query = _db.WarehouseStocks.AsNoTracking()
            .ForTenant(tenantId)
            .Include(s => s.Warehouse)
            .Include(s => s.Product)
            .AsQueryable();

        if (warehouseSyncId.HasValue)
            query = query.Where(s => s.Warehouse.SyncId == warehouseSyncId.Value);
        if (productSyncId.HasValue)
            query = query.Where(s => s.Product.SyncId == productSyncId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(s =>
                EF.Functions.Like(s.Product.Name, term) ||
                (s.Product.ScientificName != null && EF.Functions.Like(s.Product.ScientificName, term)) ||
                EF.Functions.Like(s.Warehouse.Name, term));
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(s => s.Warehouse.Name).ThenBy(s => s.Product.Name)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return Ok(new PagedResult<WarehouseStockListItem>
        {
            Items = items.Select(s => new WarehouseStockListItem
            {
                SyncId = s.SyncId,
                WarehouseSyncId = s.Warehouse.SyncId,
                WarehouseName = s.Warehouse.Name,
                ProductSyncId = s.Product.SyncId,
                ProductName = s.Product.Name,
                Quantity = s.Quantity,
                OpeningQuantity = s.OpeningQuantity,
                UnitCost = s.UnitCost
            }).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("warehouse-transfers")]
    public async Task<ActionResult<PagedResult<WarehouseTransferListItem>>> GetWarehouseTransfers(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        EnsureTenant();
        var tenantId = RequireTenantId();
        var query = _db.WarehouseTransfers.AsNoTracking()
            .ForTenant(tenantId)
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .Include(t => t.Items).ThenInclude(i => i.Product)
            .AsQueryable();

        if (from.HasValue) query = query.Where(t => t.Date >= from.Value);
        if (to.HasValue) query = query.Where(t => t.Date <= to.Value);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return Ok(new PagedResult<WarehouseTransferListItem>
        {
            Items = items.Select(t => new WarehouseTransferListItem
            {
                SyncId = t.SyncId,
                TransferNumber = t.TransferNumber,
                FromWarehouseSyncId = t.FromWarehouse.SyncId,
                FromWarehouseName = t.FromWarehouse.Name,
                ToWarehouseSyncId = t.ToWarehouse.SyncId,
                ToWarehouseName = t.ToWarehouse.Name,
                Date = t.Date,
                Notes = t.Notes,
                Items = t.Items.Where(i => !i.IsDeleted).Select(i => new WarehouseTransferItemListItem
                {
                    SyncId = i.SyncId,
                    ProductSyncId = i.Product.SyncId,
                    ProductName = i.Product.Name,
                    Quantity = i.Quantity
                }).ToList()
            }).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    private void EnsureTenant()
    {
        var tenantId = int.Parse(User.FindFirst("tenant_id")!.Value);
        if (tenantId <= 0)
            throw new InvalidOperationException("Invalid tenant_id claim.");
        _tenantContext.SetTenant(tenantId);
    }

    private int RequireTenantId()
    {
        var tid = _tenantContext.TenantId;
        if (tid is null || tid.Value <= 0)
            throw new InvalidOperationException("Tenant context is required");
        return tid.Value;
    }
}
