using AlMuhasib.Api.Models;
using AlMuhasib.Cloud.Application.Models;
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
public sealed class MobileInstallmentsController : ControllerBase
{
    private readonly CloudDbContext _db;
    private readonly ITenantContext _tenantContext;

    public MobileInstallmentsController(CloudDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet("installments")]
    public async Task<ActionResult<PagedResult<InstallmentListItem>>> GetInstallments(
        [FromQuery] string? status,
        [FromQuery] Guid? customerSyncId,
        [FromQuery] Guid? planSyncId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        EnsureTenant();
        var tenantId = RequireTenantId();
        var today = DateTime.UtcNow.Date;
        var query = _db.Installments.AsNoTracking()
            .ForTenant(tenantId)
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .Include(i => i.CashBox)
            .AsQueryable();

        if (planSyncId.HasValue)
            query = query.Where(i => i.InstallmentPlan.SyncId == planSyncId.Value);
        if (customerSyncId.HasValue)
            query = query.Where(i => i.InstallmentPlan.Customer.SyncId == customerSyncId.Value);

        if (!string.IsNullOrWhiteSpace(status))
        {
            switch (status.Trim().ToLowerInvariant())
            {
                case "overdue":
                    query = query.Where(i =>
                        i.Status != InstallmentStatus.Paid &&
                        i.DueDate.Date < today &&
                        i.RemainingAmount > 0);
                    break;
                case "upcoming":
                    query = query.Where(i =>
                        i.Status != InstallmentStatus.Paid &&
                        i.DueDate.Date >= today &&
                        i.DueDate.Date <= today.AddDays(14) &&
                        i.RemainingAmount > 0);
                    break;
                case "unpaid":
                    query = query.Where(i => i.Status != InstallmentStatus.Paid && i.RemainingAmount > 0);
                    break;
                case "paid":
                    query = query.Where(i => i.Status == InstallmentStatus.Paid);
                    break;
                case "partial":
                    query = query.Where(i => i.Status == InstallmentStatus.PartiallyPaid);
                    break;
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(i =>
                EF.Functions.Like(i.InstallmentPlan.Customer.Name, term) ||
                (i.InstallmentPlan.FileNumber != null && EF.Functions.Like(i.InstallmentPlan.FileNumber, term)));
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(i => i.DueDate)
            .ThenBy(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(new PagedResult<InstallmentListItem>
        {
            Items = items.Select(i => MapInstallment(i, today)).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("installment-plans/{syncId:guid}")]
    public async Task<ActionResult<InstallmentPlanDetailResponse>> GetPlan(Guid syncId, CancellationToken ct)
    {
        EnsureTenant();
        var tenantId = RequireTenantId();
        var today = DateTime.UtcNow.Date;
        var plan = await _db.InstallmentPlans.AsNoTracking()
            .ForTenant(tenantId)
            .Include(p => p.Customer)
            .Include(p => p.Invoice)
            .Include(p => p.Installments).ThenInclude(i => i.CashBox)
            .FirstOrDefaultAsync(p => p.SyncId == syncId, ct);

        if (plan is null) return NotFound();

        return Ok(new InstallmentPlanDetailResponse
        {
            SyncId = plan.SyncId,
            InvoiceSyncId = plan.Invoice.SyncId,
            InvoiceNumber = plan.Invoice.InvoiceNumber,
            CustomerSyncId = plan.Customer.SyncId,
            CustomerName = plan.Customer.Name,
            FileNumber = plan.FileNumber,
            TotalAmount = plan.TotalAmount,
            NumberOfInstallments = plan.NumberOfInstallments,
            InstallmentAmount = plan.InstallmentAmount,
            StartDate = plan.StartDate,
            InstallmentType = plan.InstallmentType,
            CompanyFeePercentage = plan.CompanyFeePercentage,
            CompanyFeeAmount = plan.CompanyFeeAmount,
            Installments = plan.Installments
                .Where(i => !i.IsDeleted)
                .OrderBy(i => i.DueDate)
                .Select(i => new InstallmentListItem
                {
                    SyncId = i.SyncId,
                    PlanSyncId = plan.SyncId,
                    CustomerSyncId = plan.Customer.SyncId,
                    CustomerName = plan.Customer.Name,
                    FileNumber = plan.FileNumber,
                    DueDate = i.DueDate,
                    Amount = i.Amount,
                    PaidAmount = i.PaidAmount,
                    RemainingAmount = i.RemainingAmount,
                    Status = i.Status != InstallmentStatus.Paid && i.RemainingAmount > 0 && i.DueDate.Date < today
                        ? InstallmentStatus.Overdue
                        : i.Status,
                    PaymentDate = i.PaymentDate,
                    CashBoxSyncId = i.CashBox?.SyncId,
                    CashBoxName = i.CashBox?.Name
                })
                .ToList()
        });
    }

    private static InstallmentListItem MapInstallment(Cloud.Core.Entities.CloudInstallment i, DateTime today)
    {
        var effectiveStatus = i.Status;
        if (i.Status != InstallmentStatus.Paid && i.RemainingAmount > 0 && i.DueDate.Date < today)
            effectiveStatus = InstallmentStatus.Overdue;

        return new InstallmentListItem
        {
            SyncId = i.SyncId,
            PlanSyncId = i.InstallmentPlan.SyncId,
            CustomerSyncId = i.InstallmentPlan.Customer.SyncId,
            CustomerName = i.InstallmentPlan.Customer.Name,
            FileNumber = i.InstallmentPlan.FileNumber,
            DueDate = i.DueDate,
            Amount = i.Amount,
            PaidAmount = i.PaidAmount,
            RemainingAmount = i.RemainingAmount,
            Status = effectiveStatus,
            PaymentDate = i.PaymentDate,
            CashBoxSyncId = i.CashBox?.SyncId,
            CashBoxName = i.CashBox?.Name
        };
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
