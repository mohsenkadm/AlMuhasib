using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class PartyQuickDetailService : IPartyQuickDetailService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IReportService _reportService;

    public PartyQuickDetailService(
        IDbContextFactory<AppDbContext> contextFactory,
        IReportService reportService)
    {
        _contextFactory = contextFactory;
        _reportService = reportService;
    }

    public async Task<PartyQuickDetailResult?> GetCustomerDetailAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var customer = await context.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
        if (customer is null)
            return null;

        var statement = await _reportService.GetCustomerStatementAsync(customerId);

        var invoices = await context.Invoices.AsNoTracking()
            .Where(i => i.CustomerId == customerId &&
                        (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment))
            .OrderByDescending(i => i.Date)
            .ThenByDescending(i => i.Id)
            .Select(i => new { i.Id, i.Date, i.InvoiceNumber, i.InvoiceType, i.NetAmount })
            .ToListAsync(cancellationToken);

        var last = invoices.FirstOrDefault();
        var products = await LoadProductsAsync(
            context,
            invoiceIds: invoices.Select(i => i.Id).ToList(),
            cancellationToken);

        return new PartyQuickDetailResult
        {
            PartyType = PersonPartyType.Customer,
            Id = customer.Id,
            Name = customer.Name,
            TypeLabel = "عميل",
            Phone = customer.Phone,
            Address = customer.Address,
            FileNumber = customer.FileNumber,
            Notes = customer.Notes,
            Balance = statement.Balance,
            TotalDealAmount = invoices.Sum(i => i.NetAmount),
            DealCount = invoices.Count,
            LastDealDate = last?.Date,
            LastDealDescription = last is null
                ? null
                : $"{InvoiceTypeLabel(last.InvoiceType)} — {last.InvoiceNumber}",
            LastDealAmount = last?.NetAmount,
            Products = products,
            RecentTimeline = statement.Rows
                .OrderByDescending(r => r.Date)
                .Take(8)
                .Select(r => new PartyQuickTimelineRow
                {
                    Date = r.Date,
                    Description = r.Description,
                    Debit = r.Debit,
                    Credit = r.Credit,
                    RunningBalance = r.RunningBalance
                })
                .ToList()
        };
    }

    public async Task<PartyQuickDetailResult?> GetSupplierDetailAsync(
        int supplierId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var supplier = await context.Suppliers.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == supplierId, cancellationToken);
        if (supplier is null)
            return null;

        var statement = await _reportService.GetSupplierStatementAsync(supplierId);

        var invoices = await context.Invoices.AsNoTracking()
            .Where(i => i.SupplierId == supplierId &&
                        (i.InvoiceType == InvoiceType.Purchase || i.InvoiceType == InvoiceType.PurchaseReturn))
            .OrderByDescending(i => i.Date)
            .ThenByDescending(i => i.Id)
            .Select(i => new { i.Id, i.Date, i.InvoiceNumber, i.InvoiceType, i.NetAmount })
            .ToListAsync(cancellationToken);

        var last = invoices.FirstOrDefault(i => i.InvoiceType == InvoiceType.Purchase)
                   ?? invoices.FirstOrDefault();
        var products = await LoadProductsAsync(
            context,
            invoiceIds: invoices.Where(i => i.InvoiceType == InvoiceType.Purchase).Select(i => i.Id).ToList(),
            cancellationToken);

        return new PartyQuickDetailResult
        {
            PartyType = PersonPartyType.Supplier,
            Id = supplier.Id,
            Name = supplier.Name,
            TypeLabel = "مورد",
            Phone = supplier.Phone,
            Address = supplier.Address,
            Notes = supplier.Notes,
            Balance = statement.Balance,
            TotalDealAmount = invoices
                .Where(i => i.InvoiceType == InvoiceType.Purchase)
                .Sum(i => i.NetAmount),
            DealCount = invoices.Count(i => i.InvoiceType == InvoiceType.Purchase),
            LastDealDate = last?.Date,
            LastDealDescription = last is null
                ? null
                : $"{InvoiceTypeLabel(last.InvoiceType)} — {last.InvoiceNumber}",
            LastDealAmount = last?.NetAmount,
            Products = products,
            RecentTimeline = statement.Rows
                .OrderByDescending(r => r.Date)
                .Take(8)
                .Select(r => new PartyQuickTimelineRow
                {
                    Date = r.Date,
                    Description = r.Description,
                    Debit = r.Debit,
                    Credit = r.Credit,
                    RunningBalance = r.RunningBalance
                })
                .ToList()
        };
    }

    private static async Task<List<PartyQuickProductRow>> LoadProductsAsync(
        AppDbContext context,
        List<int> invoiceIds,
        CancellationToken cancellationToken)
    {
        if (invoiceIds.Count == 0)
            return [];

        var items = await context.InvoiceItems.AsNoTracking()
            .Where(ii => invoiceIds.Contains(ii.InvoiceId))
            .Select(ii => new
            {
                ii.ProductId,
                ii.ItemName,
                ii.Quantity,
                ii.UnitPrice,
                Date = ii.Invoice.Date
            })
            .ToListAsync(cancellationToken);

        return items
            .GroupBy(i => new { i.ProductId, Name = string.IsNullOrWhiteSpace(i.ItemName) ? "—" : i.ItemName })
            .Select(g =>
            {
                var last = g.OrderByDescending(x => x.Date).First();
                return new PartyQuickProductRow
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    DealCount = g.Count(),
                    LastDate = last.Date,
                    LastUnitPrice = last.UnitPrice
                };
            })
            .OrderByDescending(p => p.TotalQuantity)
            .ThenBy(p => p.ProductName)
            .Take(50)
            .ToList();
    }

    private static string InvoiceTypeLabel(InvoiceType type) => type switch
    {
        InvoiceType.Sale => "مبيعات",
        InvoiceType.Purchase => "مشتريات",
        InvoiceType.Installment => "أقساط",
        InvoiceType.PurchaseReturn => "مرتجع مشتريات",
        _ => type.ToString()
    };
}

public sealed class NoOpPartyQuickDetailService : IPartyQuickDetailService
{
    public Task<PartyQuickDetailResult?> GetCustomerDetailAsync(int customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<PartyQuickDetailResult?>(null);

    public Task<PartyQuickDetailResult?> GetSupplierDetailAsync(int supplierId, CancellationToken cancellationToken = default) =>
        Task.FromResult<PartyQuickDetailResult?>(null);
}
