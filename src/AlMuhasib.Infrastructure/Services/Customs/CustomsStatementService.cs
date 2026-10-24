using AlMuhasib.Core.Constants;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Interfaces.Services.Customs;
using AlMuhasib.Core.Models.Customs;
using AlMuhasib.Infrastructure.Data.Customs;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Customs;

public sealed class CustomsStatementService : ICustomsStatementService
{
    private readonly IDbContextFactory<CustomsDbContext> _contextFactory;
    private readonly CustomsPermissionGuard _permissions;

    public CustomsStatementService(
        IDbContextFactory<CustomsDbContext> contextFactory,
        CustomsPermissionGuard permissions)
    {
        _contextFactory = contextFactory;
        _permissions = permissions;
    }

    public async Task<CustomsOutstandingClaimDetail> GetOutstandingClaimAsync(
        int customerId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? invoiceId = null,
        CancellationToken cancellationToken = default)
    {
        _permissions.EnsureView(CustomsScreenNames.Statement);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var customer = await db.CustomsCustomers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken)
            ?? throw new InvalidOperationException("العميل غير موجود");

        var invoicesQuery = db.CustomsInvoices.AsNoTracking()
            .Include(i => i.Vehicles)
            .Where(i => i.CustomerId == customerId
                        && i.Status == CustomsInvoiceStatus.Approved
                        && i.RemainingAmount > 0);

        if (invoiceId.HasValue)
            invoicesQuery = invoicesQuery.Where(i => i.Id == invoiceId.Value);
        if (fromDate.HasValue)
            invoicesQuery = invoicesQuery.Where(i => i.InvoiceDate >= fromDate.Value.Date);
        if (toDate.HasValue)
            invoicesQuery = invoicesQuery.Where(i => i.InvoiceDate <= toDate.Value.Date);

        var invoices = await invoicesQuery
            .OrderBy(i => i.InvoiceDate)
            .ThenBy(i => i.Id)
            .ToListAsync(cancellationToken);

        var invoiceIds = invoices.Select(i => i.Id).ToList();
        var receiptAllocs = invoiceIds.Count == 0
            ? []
            : await db.CustomsReceiptAllocations.AsNoTracking()
                .Include(a => a.Receipt)
                .Where(a => invoiceIds.Contains(a.InvoiceId) && a.Amount > 0)
                .ToListAsync(cancellationToken);

        var receiptsByInvoice = receiptAllocs
            .GroupBy(a => a.InvoiceId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(a => a.Receipt?.ReceiptNumber ?? string.Empty)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList());

        // Prefer a single currency when possible (USD first if any open USD invoices).
        var currency = invoices.Any(i => i.Currency == AccountingCurrency.USD)
            ? AccountingCurrency.USD
            : AccountingCurrency.IQD;
        var scoped = invoices.Where(i => i.Currency == currency).ToList();

        var claimInvoices = scoped.Select(inv =>
        {
            receiptsByInvoice.TryGetValue(inv.Id, out var receiptNos);
            return new CustomsOutstandingClaimInvoice
            {
                InvoiceId = inv.Id,
                InvoiceNumber = inv.InvoiceNumber,
                InvoiceDate = inv.InvoiceDate,
                Description = inv.Description?.Trim() ?? string.Empty,
                ServiceLabel = CustomsServiceTypeDisplay.ToArabic(inv.ServiceType, inv.TransportKind),
                VehicleCount = inv.Vehicles.Count,
                TotalAmount = inv.TotalAmount,
                PaidAmount = inv.PaidAmount,
                RemainingAmount = inv.RemainingAmount,
                Currency = inv.Currency,
                ReceiptNumbers = receiptNos ?? []
            };
        }).ToList();

        return new CustomsOutstandingClaimDetail
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            CustomerPhone = customer.Phone,
            DocumentDate = DateTime.Today,
            DocumentNumber = $"DN-{DateTime.Today:yyyy}-{customer.Id:D4}",
            Currency = currency,
            Invoices = claimInvoices
        };
    }

    public async Task<IReadOnlyList<CustomsStatementInvoiceOption>> GetInvoiceOptionsAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        _permissions.EnsureView(CustomsScreenNames.Statement);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var invoices = await db.CustomsInvoices.AsNoTracking()
            .Where(i => i.CustomerId == customerId && i.Status == CustomsInvoiceStatus.Approved)
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Select(i => new { i.Id, i.InvoiceNumber, i.Currency })
            .ToListAsync(cancellationToken);

        var options = new List<CustomsStatementInvoiceOption>
        {
            new() { Id = null, Label = "كل الفواتير" }
        };

        foreach (var inv in invoices)
        {
            var cur = inv.Currency == AccountingCurrency.USD ? "$" : "د.ع";
            options.Add(new CustomsStatementInvoiceOption
            {
                Id = inv.Id,
                Label = $"{inv.InvoiceNumber} ({cur})"
            });
        }

        return options;
    }

    public async Task<CustomsStatementResult> GetStatementAsync(
        int customerId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CustomsStatementTransactionType transactionType = CustomsStatementTransactionType.All,
        int? invoiceId = null,
        CancellationToken cancellationToken = default)
    {
        _permissions.EnsureView(CustomsScreenNames.Statement);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var customer = await db.CustomsCustomers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken)
            ?? throw new InvalidOperationException("العميل غير موجود");

        var invoicesQuery = db.CustomsInvoices.AsNoTracking()
            .Where(i => i.CustomerId == customerId && i.Status == CustomsInvoiceStatus.Approved);

        if (invoiceId.HasValue)
            invoicesQuery = invoicesQuery.Where(i => i.Id == invoiceId.Value);

        var invoices = await invoicesQuery
            .OrderBy(i => i.InvoiceDate)
            .ThenBy(i => i.Id)
            .ToListAsync(cancellationToken);

        var receipts = await db.CustomsReceipts.AsNoTracking()
            .Include(r => r.Allocations).ThenInclude(a => a.Invoice)
            .Where(r => r.CustomerId == customerId)
            .OrderBy(r => r.ReceiptDate)
            .ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var raw = new List<RawMovement>();

        if (transactionType is CustomsStatementTransactionType.All or CustomsStatementTransactionType.Invoice)
        {
            foreach (var inv in invoices)
            {
                raw.Add(new RawMovement(
                    inv.InvoiceDate.Date,
                    $"فاتورة {inv.InvoiceNumber}",
                    "Invoice",
                    "فاتورة",
                    inv.Id,
                    inv.Id,
                    inv.InvoiceNumber,
                    Debit: inv.TotalAmount,
                    Credit: 0,
                    inv.Currency,
                    Order: inv.Id));
            }
        }

        if (transactionType is CustomsStatementTransactionType.All or CustomsStatementTransactionType.Receipt)
        {
            foreach (var receipt in receipts)
            {
                var allocs = receipt.Allocations.Where(a => !a.IsDeleted).ToList();
                if (invoiceId.HasValue)
                {
                    var matched = allocs.Where(a => a.InvoiceId == invoiceId.Value).ToList();
                    if (matched.Count == 0)
                        continue;

                    var amount = matched.Sum(a => a.Amount);
                    var invNum = matched.First().Invoice?.InvoiceNumber
                                 ?? invoices.FirstOrDefault(i => i.Id == invoiceId.Value)?.InvoiceNumber
                                 ?? string.Empty;
                    raw.Add(new RawMovement(
                        receipt.ReceiptDate.Date,
                        $"قبض {receipt.ReceiptNumber} على {invNum}",
                        "Receipt",
                        "قبض",
                        receipt.Id,
                        invoiceId,
                        invNum,
                        Debit: 0,
                        Credit: amount,
                        receipt.Currency,
                        Order: receipt.Id));
                }
                else
                {
                    var detail = allocs.Count == 0
                        ? string.Empty
                        : " — " + string.Join(" | ", allocs.Select(a =>
                            $"{a.Invoice?.InvoiceNumber ?? a.InvoiceId.ToString()}:{a.Amount:N0}"));

                    raw.Add(new RawMovement(
                        receipt.ReceiptDate.Date,
                        $"قبض {receipt.ReceiptNumber}{detail}",
                        "Receipt",
                        "قبض",
                        receipt.Id,
                        null,
                        null,
                        Debit: 0,
                        Credit: receipt.Amount,
                        receipt.Currency,
                        Order: receipt.Id));
                }
            }
        }

        var from = fromDate?.Date;
        var toExclusive = toDate?.Date.AddDays(1);

        decimal OpeningNet(AccountingCurrency currency)
        {
            if (from is null) return 0m;
            return raw
                .Where(m => m.Currency == currency && m.Date < from.Value)
                .Sum(m => m.Debit - m.Credit);
        }

        var openingIqd = OpeningNet(AccountingCurrency.IQD);
        var openingUsd = OpeningNet(AccountingCurrency.USD);

        var inRange = raw
            .Where(m => (from is null || m.Date >= from.Value)
                        && (toExclusive is null || m.Date < toExclusive.Value))
            .OrderBy(m => m.Date)
            .ThenBy(m => m.Order)
            .ToList();

        var rows = new List<CustomsStatementRow>();
        rows.AddRange(BuildCurrencyLedger(AccountingCurrency.IQD, openingIqd, inRange, from));
        rows.AddRange(BuildCurrencyLedger(AccountingCurrency.USD, openingUsd, inRange, from));

        rows = rows
            .OrderBy(r => r.Date)
            .ThenBy(r => r.TransactionType == "Opening" ? 0 : 1)
            .ThenBy(r => r.DocumentId)
            .ToList();

        var periodRows = rows.Where(r => r.TransactionType != "Opening").ToList();

        return new CustomsStatementResult
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            CustomerPhone = customer.Phone,
            OpeningIqd = openingIqd,
            OpeningUsd = openingUsd,
            TotalDebitIqd = periodRows.Where(r => r.Currency == AccountingCurrency.IQD).Sum(r => r.Debit),
            TotalCreditIqd = periodRows.Where(r => r.Currency == AccountingCurrency.IQD).Sum(r => r.Credit),
            TotalDebitUsd = periodRows.Where(r => r.Currency == AccountingCurrency.USD).Sum(r => r.Debit),
            TotalCreditUsd = periodRows.Where(r => r.Currency == AccountingCurrency.USD).Sum(r => r.Credit),
            BalanceIqd = openingIqd
                         + periodRows.Where(r => r.Currency == AccountingCurrency.IQD).Sum(r => r.Debit - r.Credit),
            BalanceUsd = openingUsd
                         + periodRows.Where(r => r.Currency == AccountingCurrency.USD).Sum(r => r.Debit - r.Credit),
            TransactionCount = periodRows.Count,
            Rows = rows
        };
    }

    private static List<CustomsStatementRow> BuildCurrencyLedger(
        AccountingCurrency currency,
        decimal opening,
        IReadOnlyList<RawMovement> inRange,
        DateTime? from)
    {
        var rows = new List<CustomsStatementRow>();
        var running = opening;
        var movements = inRange.Where(m => m.Currency == currency).ToList();

        if (from.HasValue && (opening != 0 || movements.Count > 0))
        {
            rows.Add(new CustomsStatementRow
            {
                Date = from.Value,
                Description = "رصيد افتتاحي",
                TransactionType = "Opening",
                TransactionTypeLabel = "افتتاحي",
                DocumentId = 0,
                Debit = opening > 0 ? opening : 0,
                Credit = opening < 0 ? Math.Abs(opening) : 0,
                RunningBalance = running,
                Currency = currency
            });
        }

        foreach (var m in movements)
        {
            running += m.Debit - m.Credit;
            rows.Add(new CustomsStatementRow
            {
                Date = m.Date,
                Description = m.Description,
                TransactionType = m.TransactionType,
                TransactionTypeLabel = m.TransactionTypeLabel,
                DocumentId = m.DocumentId,
                RelatedInvoiceId = m.RelatedInvoiceId,
                RelatedInvoiceNumber = m.RelatedInvoiceNumber,
                Debit = m.Debit,
                Credit = m.Credit,
                RunningBalance = running,
                Currency = currency
            });
        }

        return rows;
    }

    private sealed record RawMovement(
        DateTime Date,
        string Description,
        string TransactionType,
        string TransactionTypeLabel,
        int DocumentId,
        int? RelatedInvoiceId,
        string? RelatedInvoiceNumber,
        decimal Debit,
        decimal Credit,
        AccountingCurrency Currency,
        int Order);
}
