using AlMuhasib.Api.Models;
using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize(Policy = "Tenant")]
public sealed class InvoicesController : ControllerBase
{
    private readonly CloudDbContext _db;
    private readonly ITenantContext _tenantContext;

    public InvoicesController(CloudDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<InvoiceDetailResponse>>> GetAll(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] InvoiceType? invoiceType,
        [FromQuery] PaymentMethod? paymentMethod,
        CancellationToken ct)
    {
        EnsureTenant();
        var query = BuildInvoiceQuery();
        if (from.HasValue) query = query.Where(i => i.Date >= from.Value);
        if (to.HasValue) query = query.Where(i => i.Date <= to.Value);
        if (invoiceType.HasValue) query = query.Where(i => i.InvoiceType == invoiceType.Value);
        if (paymentMethod.HasValue) query = query.Where(i => i.PaymentMethod == paymentMethod.Value);

        var invoices = await query.OrderByDescending(i => i.Date).ToListAsync(ct);
        return Ok(invoices.Select(MapInvoice).ToList());
    }

    [HttpGet("{syncId:guid}")]
    public async Task<ActionResult<InvoiceDetailResponse>> GetBySyncId(Guid syncId, CancellationToken ct)
    {
        EnsureTenant();
        var invoice = await BuildInvoiceQuery()
            .FirstOrDefaultAsync(i => i.SyncId == syncId, ct);
        return invoice is null ? NotFound() : Ok(MapInvoice(invoice));
    }

    private void EnsureTenant()
    {
        var tenantId = int.Parse(User.FindFirst("tenant_id")!.Value);
        _tenantContext.SetTenant(tenantId);
    }

    private IQueryable<CloudInvoice> BuildInvoiceQuery() =>
        _db.Invoices.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Supplier)
            .Include(i => i.Warehouse)
            .Include(i => i.CashBox)
            .Include(i => i.Items).ThenInclude(item => item.Product)
            .Include(i => i.InstallmentPlans).ThenInclude(p => p.Customer)
            .Include(i => i.InstallmentPlans).ThenInclude(p => p.Installments).ThenInclude(inst => inst.CashBox);

    private static InvoiceDetailResponse MapInvoice(CloudInvoice i) => new()
    {
        Id = i.Id,
        SyncId = i.SyncId,
        InvoiceNumber = i.InvoiceNumber,
        InvoiceType = i.InvoiceType,
        CustomerSyncId = i.Customer?.SyncId,
        CustomerName = i.Customer?.Name,
        SupplierSyncId = i.Supplier?.SyncId,
        SupplierName = i.Supplier?.Name,
        WarehouseSyncId = i.Warehouse.SyncId,
        WarehouseName = i.Warehouse.Name,
        PaymentMethod = i.PaymentMethod,
        TotalAmount = i.TotalAmount,
        DiscountAmount = i.DiscountAmount,
        NetAmount = i.NetAmount,
        CompanyFeePercentage = i.CompanyFeePercentage,
        CompanyFeeAmount = i.CompanyFeeAmount,
        RoundingAmount = i.RoundingAmount,
        RoundingType = i.RoundingType,
        CashBoxSyncId = i.CashBox?.SyncId,
        CashBoxName = i.CashBox?.Name,
        Date = i.Date,
        CreditDueDate = i.CreditDueDate,
        Notes = i.Notes,
        PaidAmount = i.PaidAmount,
        RemainingAmount = i.RemainingAmount,
        IsCreditPaid = i.IsCreditPaid,
        Items = i.Items
            .Where(item => !item.IsDeleted)
            .OrderBy(item => item.Id)
            .Select(item => new InvoiceItemDetail
            {
                SyncId = item.SyncId,
                ProductSyncId = item.Product?.SyncId,
                ItemName = item.ItemName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            }).ToList(),
        InstallmentPlans = i.InstallmentPlans
            .Where(p => !p.IsDeleted)
            .Select(p => new InstallmentPlanDetail
            {
                SyncId = p.SyncId,
                CustomerSyncId = p.Customer.SyncId,
                FileNumber = p.FileNumber,
                TotalAmount = p.TotalAmount,
                NumberOfInstallments = p.NumberOfInstallments,
                InstallmentAmount = p.InstallmentAmount,
                StartDate = p.StartDate,
                InstallmentType = p.InstallmentType,
                CompanyFeePercentage = p.CompanyFeePercentage,
                CompanyFeeAmount = p.CompanyFeeAmount,
                Installments = p.Installments
                    .Where(inst => !inst.IsDeleted)
                    .OrderBy(inst => inst.DueDate)
                    .Select(inst => new InstallmentDetail
                    {
                        SyncId = inst.SyncId,
                        DueDate = inst.DueDate,
                        Amount = inst.Amount,
                        PaidAmount = inst.PaidAmount,
                        RemainingAmount = inst.RemainingAmount,
                        Status = inst.Status,
                        PaymentDate = inst.PaymentDate,
                        CashBoxSyncId = inst.CashBox?.SyncId
                    }).ToList()
            }).ToList()
    };
}
