using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Cloud.Infrastructure.Mobile;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Sync.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers;

[ApiController]
[Route("api/exchange-rates")]
[Authorize(Policy = "Tenant")]
public sealed class ExchangeRatesController : ControllerBase
{
    private readonly CloudDbContext _db;
    private readonly ITenantContext _tenantContext;

    public ExchangeRatesController(CloudDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    /// <summary>آخر سعر صرف USD→IQD المتزامن (لتعبئة fxRate الافتراضي على الجوال).</summary>
    [HttpGet("latest")]
    public async Task<ActionResult<LatestExchangeRateResponse>> GetLatest(CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        var query = ScopedRates(tenantId);

        var latest = await query
            .OrderByDescending(r => r.RateDate)
            .ThenByDescending(r => r.UpdatedAt ?? r.CreatedAt)
            .Select(r => new LatestExchangeRateResponse
            {
                RateDate = r.RateDate,
                UsdToIqd = r.UsdToIqd,
                Notes = r.Notes,
                SyncId = r.SyncId
            })
            .FirstOrDefaultAsync(ct);

        if (latest is null)
            return NotFound(new ApiErrorResponse { Code = "NO_EXCHANGE_RATE", Message = "لا يوجد سعر صرف مسجّل" });

        return Ok(latest);
    }

    [HttpGet]
    public async Task<ActionResult<List<LatestExchangeRateResponse>>> List(
        [FromQuery] int take = 30, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();
        take = Math.Clamp(take, 1, 200);
        var items = await ScopedRates(tenantId)
            .OrderByDescending(r => r.RateDate)
            .ThenByDescending(r => r.UpdatedAt ?? r.CreatedAt)
            .Take(take)
            .Select(r => new LatestExchangeRateResponse
            {
                RateDate = r.RateDate,
                UsdToIqd = r.UsdToIqd,
                Notes = r.Notes,
                SyncId = r.SyncId
            })
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<LatestExchangeRateResponse>> Create(
        [FromBody] CreateExchangeRateRequest request, CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        try
        {
            await CloudMultiCurrencyFeatureGate.ApplyForWriteAsync(
                _db, tenantId, AccountingCurrency.USD, request.UsdToIqd, "سعر صرف موبايل", ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiErrorResponse { Code = "MULTI_CURRENCY", Message = ex.Message });
        }

        if (request.UsdToIqd <= 0)
            return BadRequest(new ApiErrorResponse { Code = "INVALID_RATE", Message = "سعر الصرف يجب أن يكون أكبر من صفر" });

        var branchId = _tenantContext.BranchId
            ?? (_tenantContext.AllowedBranchIds.Count == 1 ? _tenantContext.AllowedBranchIds[0] : (int?)null);
        if (branchId is not > 0)
            return BadRequest(new ApiErrorResponse { Code = "BRANCH_REQUIRED", Message = "اختر فرعاً قبل تسجيل سعر الصرف" });

        var username = User.Identity?.Name ?? "mobile";
        var syncId = request.SyncId is { } s && s != Guid.Empty ? s : Guid.NewGuid();
        var rateDate = (request.RateDate == default ? DateTime.UtcNow : request.RateDate).Date;

        var existingSameDay = await _db.ExchangeRates
            .FirstOrDefaultAsync(r =>
                r.TenantId == tenantId && r.BranchId == branchId.Value
                && !r.IsDeleted && r.RateDate == rateDate, ct);

        if (existingSameDay is not null)
        {
            existingSameDay.UsdToIqd = AccountingCurrencyHelper.RoundIqd(request.UsdToIqd);
            existingSameDay.Notes = request.Notes?.Trim() ?? string.Empty;
            existingSameDay.UpdatedAt = DateTime.UtcNow;
            existingSameDay.UpdatedBy = username;
            await _db.SaveChangesAsync(ct);
            return Ok(new LatestExchangeRateResponse
            {
                RateDate = existingSameDay.RateDate,
                UsdToIqd = existingSameDay.UsdToIqd,
                Notes = existingSameDay.Notes,
                SyncId = existingSameDay.SyncId
            });
        }

        var entity = new CloudExchangeRate
        {
            TenantId = tenantId,
            BranchId = branchId.Value,
            SyncId = syncId,
            RateDate = rateDate,
            UsdToIqd = AccountingCurrencyHelper.RoundIqd(request.UsdToIqd),
            Notes = request.Notes?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = username
        };
        _db.ExchangeRates.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new LatestExchangeRateResponse
        {
            RateDate = entity.RateDate,
            UsdToIqd = entity.UsdToIqd,
            Notes = entity.Notes,
            SyncId = entity.SyncId
        });
    }

    private int RequireTenantId() =>
        _tenantContext.TenantId ?? int.Parse(User.FindFirst("tenant_id")!.Value);

    private IQueryable<CloudExchangeRate> ScopedRates(int tenantId)
    {
        var query = _db.ExchangeRates.AsNoTracking()
            .Where(r => r.TenantId == tenantId && !r.IsDeleted);

        if (_tenantContext is { IsAllBranchesMode: false, BranchId: > 0 } ctx)
            query = query.Where(r => r.BranchId == ctx.BranchId!.Value);
        else if (_tenantContext.AllowedBranchIds.Count > 0)
            query = query.Where(r => _tenantContext.AllowedBranchIds.Contains(r.BranchId));

        return query;
    }
}

public sealed class CreateExchangeRateRequest
{
    public Guid? SyncId { get; set; }
    public DateTime RateDate { get; set; }
    public decimal UsdToIqd { get; set; }
    public string? Notes { get; set; }
}
