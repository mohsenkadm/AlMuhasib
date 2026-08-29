using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums.Gold;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.GoldShop;

[ApiController]
[Route("api/gold-shop/statements")]
[Authorize(Policy = "Tenant")]
public sealed class GoldShopStatementsController : GoldShopApiControllerBase
{
    public GoldShopStatementsController(CloudDbContext db, ITenantContext tenantContext) : base(db, tenantContext) { }

    [HttpGet("customer/{id:int}")]
    public async Task<ActionResult<GoldStatementDto>> GetCustomerStatement(
        int id, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var customer = await Db.GoldCustomers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == TenantId && c.Id == id, ct);
        if (customer is null) return NotFound();

        var rows = new List<GoldStatementRowDto>();

        var invoices = await Db.GoldInvoices.AsNoTracking()
            .Where(i => i.TenantId == TenantId && i.CustomerId == id && i.Status != GoldInvoiceStatus.Cancelled)
            .ToListAsync(ct);
        foreach (var i in invoices)
        {
            if (from.HasValue && i.InvoiceDate.Date < from.Value.Date) continue;
            if (to.HasValue && i.InvoiceDate.Date > to.Value.Date) continue;
            var debit = i.InvoiceType is GoldInvoiceType.Sale or GoldInvoiceType.Exchange
                ? i.TotalAmountIqd
                : 0;
            var credit = i.InvoiceType == GoldInvoiceType.SaleReturn ? i.TotalAmountIqd : 0;
            if (i.InvoiceType == GoldInvoiceType.Exchange && i.ExchangeCashDifference < 0)
            {
                credit = Math.Abs(i.TotalAmountIqd);
                debit = 0;
            }
            rows.Add(new GoldStatementRowDto
            {
                Date = i.InvoiceDate,
                DocumentType = i.InvoiceType.ToString(),
                DocumentNumber = i.InvoiceNumber,
                Debit = debit,
                Credit = credit,
                Notes = i.Notes
            });
        }

        var vouchers = await Db.GoldVouchers.AsNoTracking()
            .Where(v => v.TenantId == TenantId && v.CustomerId == id && !v.IsDeleted)
            .ToListAsync(ct);
        foreach (var v in vouchers)
        {
            if (from.HasValue && v.VoucherDate.Date < from.Value.Date) continue;
            if (to.HasValue && v.VoucherDate.Date > to.Value.Date) continue;
            var amountIqd = v.Currency == GoldCurrency.IQD ? v.Amount : 0; // simple; FX omitted for statement clarity
            if (v.Currency == GoldCurrency.USD) amountIqd = v.Amount; // show raw; label currency in notes
            rows.Add(new GoldStatementRowDto
            {
                Date = v.VoucherDate,
                DocumentType = v.VoucherType == GoldVoucherType.Receipt ? "Receipt" : "Payment",
                DocumentNumber = v.VoucherNumber,
                Debit = v.VoucherType == GoldVoucherType.Payment ? amountIqd : 0,
                Credit = v.VoucherType == GoldVoucherType.Receipt ? amountIqd : 0,
                Notes = v.Currency == GoldCurrency.USD ? $"USD {v.Amount:N2} — {v.Notes}" : v.Notes
            });
        }

        var payments = await Db.GoldPayments.AsNoTracking()
            .Include(p => p.Invoice)
            .Where(p => p.TenantId == TenantId && p.Invoice != null && p.Invoice.CustomerId == id)
            .ToListAsync(ct);
        foreach (var p in payments)
        {
            if (from.HasValue && p.PaymentDate.Date < from.Value.Date) continue;
            if (to.HasValue && p.PaymentDate.Date > to.Value.Date) continue;
            rows.Add(new GoldStatementRowDto
            {
                Date = p.PaymentDate,
                DocumentType = "Collection",
                DocumentNumber = p.Invoice?.InvoiceNumber ?? $"P-{p.Id}",
                Debit = 0,
                Credit = p.Currency == GoldCurrency.IQD ? p.Amount : p.Amount,
                Notes = p.Notes
            });
        }

        var ordered = rows.OrderBy(r => r.Date).ThenBy(r => r.DocumentNumber).ToList();
        decimal running = 0;
        foreach (var r in ordered)
        {
            running += r.Debit - r.Credit;
            r.Balance = running;
        }

        return Ok(new GoldStatementDto
        {
            PartyId = customer.Id,
            PartyName = customer.Name,
            PartyType = "Customer",
            CreditBalanceIqd = customer.CreditBalanceIqd,
            CreditBalanceUsd = customer.CreditBalanceUsd,
            OpeningBalance = 0,
            ClosingBalance = running,
            Rows = ordered
        });
    }

    [HttpGet("supplier/{id:int}")]
    public async Task<ActionResult<GoldStatementDto>> GetSupplierStatement(
        int id, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var supplier = await Db.GoldSuppliers.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == TenantId && s.Id == id, ct);
        if (supplier is null) return NotFound();

        var rows = new List<GoldStatementRowDto>();
        var invoices = await Db.GoldInvoices.AsNoTracking()
            .Where(i => i.TenantId == TenantId && i.SupplierId == id && i.Status != GoldInvoiceStatus.Cancelled)
            .ToListAsync(ct);
        foreach (var i in invoices)
        {
            if (from.HasValue && i.InvoiceDate.Date < from.Value.Date) continue;
            if (to.HasValue && i.InvoiceDate.Date > to.Value.Date) continue;
            rows.Add(new GoldStatementRowDto
            {
                Date = i.InvoiceDate,
                DocumentType = i.InvoiceType.ToString(),
                DocumentNumber = i.InvoiceNumber,
                Debit = i.InvoiceType == GoldInvoiceType.Purchase ? i.TotalAmountIqd : 0,
                Credit = 0,
                Notes = i.Notes
            });
        }

        var vouchers = await Db.GoldVouchers.AsNoTracking()
            .Where(v => v.TenantId == TenantId && v.SupplierId == id && !v.IsDeleted)
            .ToListAsync(ct);
        foreach (var v in vouchers)
        {
            if (from.HasValue && v.VoucherDate.Date < from.Value.Date) continue;
            if (to.HasValue && v.VoucherDate.Date > to.Value.Date) continue;
            rows.Add(new GoldStatementRowDto
            {
                Date = v.VoucherDate,
                DocumentType = v.VoucherType == GoldVoucherType.Receipt ? "Receipt" : "Payment",
                DocumentNumber = v.VoucherNumber,
                Debit = 0,
                Credit = v.Amount,
                Notes = v.Notes
            });
        }

        var ordered = rows.OrderBy(r => r.Date).ThenBy(r => r.DocumentNumber).ToList();
        decimal running = 0;
        foreach (var r in ordered)
        {
            running += r.Debit - r.Credit;
            r.Balance = running;
        }

        return Ok(new GoldStatementDto
        {
            PartyId = supplier.Id,
            PartyName = supplier.Name,
            PartyType = "Supplier",
            CreditBalanceIqd = supplier.CreditBalanceIqd,
            CreditBalanceUsd = supplier.CreditBalanceUsd,
            OpeningBalance = 0,
            ClosingBalance = running,
            Rows = ordered
        });
    }
}

public sealed class GoldStatementDto
{
    public int PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string PartyType { get; set; } = string.Empty;
    public decimal CreditBalanceIqd { get; set; }
    public decimal CreditBalanceUsd { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<GoldStatementRowDto> Rows { get; set; } = [];
}

public sealed class GoldStatementRowDto
{
    public DateTime Date { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public string Notes { get; set; } = string.Empty;
}
