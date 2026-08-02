using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums.Gold;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.GoldShop;

[ApiController]
[Route("api/gold-shop/invoices")]
[Authorize(Policy = "Tenant")]
public sealed class GoldShopInvoicesController : GoldShopApiControllerBase
{
    public GoldShopInvoicesController(CloudDbContext db, ITenantContext tenantContext) : base(db, tenantContext) { }

    [HttpGet]
    public async Task<ActionResult<List<GoldInvoiceListDto>>> GetInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? invoiceType = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] bool? hasRemaining = null,
        CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = Db.GoldInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Where(i => i.TenantId == TenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i =>
                i.InvoiceNumber.Contains(term) ||
                (i.Customer != null && i.Customer.Name.Contains(term)) ||
                i.Notes.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(invoiceType) && Enum.TryParse<GoldInvoiceType>(invoiceType, true, out var typeEnum))
            query = query.Where(i => i.InvoiceType == typeEnum);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<GoldInvoiceStatus>(status, true, out var statusEnum))
            query = query.Where(i => i.Status == statusEnum);

        if (from.HasValue)
            query = query.Where(i => i.InvoiceDate >= from.Value.Date);

        if (to.HasValue)
            query = query.Where(i => i.InvoiceDate <= to.Value.Date);

        if (hasRemaining == true)
            query = query.Where(i => i.RemainingAmount > 0);

        var items = await query
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(items.Select(GoldShopInvoiceMapper.ToListItem).ToList());
    }

    [HttpGet("{syncId:guid}")]
    public async Task<ActionResult<GoldInvoiceDetailDto>> GetInvoice(Guid syncId, CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var invoice = await Db.GoldInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.TenantId == TenantId && i.SyncId == syncId, ct);
        if (invoice is null) return NotFound();

        return Ok(GoldShopInvoiceMapper.ToDetail(invoice));
    }
}

public class GoldInvoiceListDto
{
    public Guid SyncId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string InvoiceType { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public Guid? CustomerSyncId { get; set; }
    public string PricingCurrency { get; set; } = string.Empty;
    public string PaymentCurrency { get; set; } = string.Empty;
    public decimal TotalWeightGrams { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalAmountIqd { get; set; }
    public decimal TotalAmountUsd { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class GoldInvoiceDetailDto : GoldInvoiceListDto
{
    public decimal FxRate { get; set; }
    public decimal TotalGoldValue { get; set; }
    public decimal TotalMakingCharge { get; set; }
    public decimal DiscountAmount { get; set; }
    public bool WeightFromScale { get; set; }
    public List<GoldInvoiceLineDto> Lines { get; set; } = [];
    public List<GoldPaymentDto> Payments { get; set; } = [];
}

public sealed class GoldInvoiceLineDto
{
    public Guid SyncId { get; set; }
    public int KaratValue { get; set; }
    public decimal WeightGrams { get; set; }
    public decimal MithqalPrice { get; set; }
    public decimal PricePerGram { get; set; }
    public decimal GoldValue { get; set; }
    public decimal MakingCharge { get; set; }
    public decimal LineTotal { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class GoldPaymentDto
{
    public Guid SyncId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal FxRate { get; set; }
    public string Notes { get; set; } = string.Empty;
}

internal static class GoldShopInvoiceMapper
{
    public static GoldInvoiceListDto ToListItem(CloudGoldInvoice i) => new()
    {
        SyncId = i.SyncId,
        InvoiceNumber = i.InvoiceNumber,
        InvoiceDate = i.InvoiceDate,
        InvoiceType = i.InvoiceType.ToString(),
        PaymentMethod = i.PaymentMethod.ToString(),
        Status = i.Status.ToString(),
        CustomerName = i.Customer?.Name,
        CustomerSyncId = i.Customer?.SyncId,
        PricingCurrency = i.PricingCurrency.ToString(),
        PaymentCurrency = i.PaymentCurrency.ToString(),
        TotalWeightGrams = i.TotalWeightGrams,
        TotalAmount = i.TotalAmount,
        TotalAmountIqd = i.TotalAmountIqd,
        TotalAmountUsd = i.TotalAmountUsd,
        PaidAmount = i.PaidAmount,
        RemainingAmount = i.RemainingAmount,
        Notes = i.Notes
    };

    public static GoldInvoiceDetailDto ToDetail(CloudGoldInvoice i)
    {
        var list = ToListItem(i);
        return new GoldInvoiceDetailDto
        {
            SyncId = list.SyncId,
            InvoiceNumber = list.InvoiceNumber,
            InvoiceDate = list.InvoiceDate,
            InvoiceType = list.InvoiceType,
            PaymentMethod = list.PaymentMethod,
            Status = list.Status,
            CustomerName = list.CustomerName,
            CustomerSyncId = list.CustomerSyncId,
            PricingCurrency = list.PricingCurrency,
            PaymentCurrency = list.PaymentCurrency,
            TotalWeightGrams = list.TotalWeightGrams,
            TotalAmount = list.TotalAmount,
            TotalAmountIqd = list.TotalAmountIqd,
            TotalAmountUsd = list.TotalAmountUsd,
            PaidAmount = list.PaidAmount,
            RemainingAmount = list.RemainingAmount,
            Notes = list.Notes,
            FxRate = i.FxRate,
            TotalGoldValue = i.TotalGoldValue,
            TotalMakingCharge = i.TotalMakingCharge,
            DiscountAmount = i.DiscountAmount,
            WeightFromScale = i.WeightFromScale,
            Lines = i.Lines
                .Where(l => !l.IsDeleted)
                .Select(l => new GoldInvoiceLineDto
                {
                    SyncId = l.SyncId,
                    KaratValue = l.KaratValue,
                    WeightGrams = l.WeightGrams,
                    MithqalPrice = l.MithqalPrice,
                    PricePerGram = l.PricePerGram,
                    GoldValue = l.GoldValue,
                    MakingCharge = l.MakingCharge,
                    LineTotal = l.LineTotal,
                    Description = l.Description
                })
                .ToList(),
            Payments = i.Payments
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new GoldPaymentDto
                {
                    SyncId = p.SyncId,
                    PaymentDate = p.PaymentDate,
                    Amount = p.Amount,
                    Currency = p.Currency.ToString(),
                    FxRate = p.FxRate,
                    Notes = p.Notes
                })
                .ToList()
        };
    }
}
