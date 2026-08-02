using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldReportService : IGoldReportService
{
    private readonly IDbContextFactory<GoldDbContext> _contextFactory;
    private readonly IGoldCustomerService _customerService;

    public GoldReportService(
        IDbContextFactory<GoldDbContext> contextFactory,
        IGoldCustomerService customerService)
    {
        _contextFactory = contextFactory;
        _customerService = customerService;
    }

    public async Task<GoldReportSummary> GetSummaryAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.GoldInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Where(i => i.Status != GoldInvoiceStatus.Cancelled);

        if (dateFrom.HasValue)
            query = query.Where(i => i.InvoiceDate.Date >= dateFrom.Value.Date);
        if (dateTo.HasValue)
            query = query.Where(i => i.InvoiceDate.Date <= dateTo.Value.Date);

        var invoices = await query
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .ToListAsync(cancellationToken);

        var sales = invoices.Where(i => i.InvoiceType == GoldInvoiceType.Sale).ToList();
        var purchases = invoices.Where(i => i.InvoiceType == GoldInvoiceType.Purchase).ToList();

        // Approx gold profit: sale gold value vs average stock cost at line weight; making charge is margin.
        decimal makingIqd = 0, makingUsd = 0;
        foreach (var sale in sales)
        {
            var making = sale.TotalMakingCharge;
            if (sale.PricingCurrency == GoldCurrency.USD)
            {
                makingUsd += making;
                makingIqd += making * (sale.FxRate > 0 ? sale.FxRate : 1m);
            }
            else
            {
                makingIqd += making;
                makingUsd += making / (sale.FxRate > 0 ? sale.FxRate : 1m);
            }
        }

        var vouchers = context.GoldVouchers.AsNoTracking().AsQueryable();
        if (dateFrom.HasValue)
            vouchers = vouchers.Where(v => v.VoucherDate.Date >= dateFrom.Value.Date);
        if (dateTo.HasValue)
            vouchers = vouchers.Where(v => v.VoucherDate.Date <= dateTo.Value.Date);
        var voucherList = await vouchers.ToListAsync(cancellationToken);

        var cashInIqd = voucherList.Where(v => v.VoucherType == GoldVoucherType.Receipt && v.Currency == GoldCurrency.IQD).Sum(v => v.Amount)
            + sales.SelectMany(s => s.Payments).Where(p => p.Currency == GoldCurrency.IQD).Sum(p => p.Amount);
        var cashInUsd = voucherList.Where(v => v.VoucherType == GoldVoucherType.Receipt && v.Currency == GoldCurrency.USD).Sum(v => v.Amount)
            + sales.SelectMany(s => s.Payments).Where(p => p.Currency == GoldCurrency.USD).Sum(p => p.Amount);
        var cashOutIqd = voucherList.Where(v => v.VoucherType == GoldVoucherType.Payment && v.Currency == GoldCurrency.IQD).Sum(v => v.Amount)
            + purchases.SelectMany(s => s.Payments).Where(p => p.Currency == GoldCurrency.IQD).Sum(p => p.Amount);
        var cashOutUsd = voucherList.Where(v => v.VoucherType == GoldVoucherType.Payment && v.Currency == GoldCurrency.USD).Sum(v => v.Amount)
            + purchases.SelectMany(s => s.Payments).Where(p => p.Currency == GoldCurrency.USD).Sum(p => p.Amount);

        var creditCustomers = await context.GoldCustomers.AsNoTracking()
            .Where(c => c.CreditBalanceIqd > 0 || c.CreditBalanceUsd > 0)
            .ToListAsync(cancellationToken);

        var stock = await GoldInventoryService.BuildStockRowsAsync(context, cancellationToken);

        return new GoldReportSummary
        {
            DateFrom = dateFrom?.Date,
            DateTo = dateTo?.Date,
            SaleCount = sales.Count,
            PurchaseCount = purchases.Count,
            TotalSalesIqd = sales.Sum(s => s.TotalAmountIqd),
            TotalSalesUsd = sales.Sum(s => s.TotalAmountUsd),
            TotalPurchasesIqd = purchases.Sum(p => p.TotalAmountIqd),
            TotalPurchasesUsd = purchases.Sum(p => p.TotalAmountUsd),
            TotalMakingChargesIqd = GoldCurrencyHelper.Round(makingIqd),
            TotalMakingChargesUsd = GoldCurrencyHelper.Round(makingUsd),
            TotalWeightSoldGrams = sales.Sum(s => s.TotalWeightGrams),
            TotalWeightPurchasedGrams = purchases.Sum(p => p.TotalWeightGrams),
            CashInIqd = GoldCurrencyHelper.Round(cashInIqd),
            CashInUsd = GoldCurrencyHelper.Round(cashInUsd),
            CashOutIqd = GoldCurrencyHelper.Round(cashOutIqd),
            CashOutUsd = GoldCurrencyHelper.Round(cashOutUsd),
            CreditOutstandingIqd = creditCustomers.Sum(c => c.CreditBalanceIqd),
            CreditOutstandingUsd = creditCustomers.Sum(c => c.CreditBalanceUsd),
            ClosingStock = stock,
            Invoices = invoices.Select(GoldCurrencyHelper.ToListItem).ToList()
        };
    }

    public async Task<IReadOnlyList<GoldInvoiceListItem>> GetSalesReportAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        GoldInvoiceStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var (items, _) = await GoldInvoiceQueryHelper.GetPagedAsync(
            _contextFactory,
            GoldInvoiceType.Sale,
            1,
            10_000,
            null,
            dateFrom,
            dateTo,
            status,
            null,
            cancellationToken);
        return items;
    }

    public async Task<IReadOnlyList<GoldInvoiceListItem>> GetPurchasesReportAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        GoldInvoiceStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var (items, _) = await GoldInvoiceQueryHelper.GetPagedAsync(
            _contextFactory,
            GoldInvoiceType.Purchase,
            1,
            10_000,
            null,
            dateFrom,
            dateTo,
            status,
            null,
            cancellationToken);
        return items;
    }

    public async Task<IReadOnlyList<GoldStockRow>> GetStockReportAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await GoldInventoryService.BuildStockRowsAsync(context, cancellationToken);
    }

    public async Task<IReadOnlyList<GoldCustomerListItem>> GetCreditReportAsync(
        bool overdueOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (overdueOnly)
            return await _customerService.GetOverdueCreditCustomersAsync(cancellationToken);

        var (items, _) = await _customerService.GetPagedAsync(
            1,
            10_000,
            creditOnly: true,
            cancellationToken: cancellationToken);
        return items;
    }
}
