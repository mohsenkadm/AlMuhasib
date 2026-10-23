using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Interfaces.Services.Customs;
using AlMuhasib.Core.Models.Customs;
using AlMuhasib.Infrastructure.Data.Customs;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Customs;

public sealed class CustomsDashboardService : ICustomsDashboardService
{
    private readonly IDbContextFactory<CustomsDbContext> _contextFactory;

    public CustomsDashboardService(IDbContextFactory<CustomsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<CustomsDashboardStats> GetDashboardStatsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var to = (toDate ?? DateTime.Today).Date;
        var from = (fromDate ?? new DateTime(to.Year, to.Month, 1)).Date;
        var toExclusive = to.AddDays(1);

        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var customerCount = await db.CustomsCustomers.AsNoTracking()
            .CountAsync(c => c.IsActive, cancellationToken);

        var invoices = await db.CustomsInvoices.AsNoTracking()
            .Where(i => i.Status != CustomsInvoiceStatus.Cancelled)
            .Select(i => new
            {
                i.Id,
                i.InvoiceDate,
                i.Currency,
                i.TotalAmount,
                i.RemainingAmount
            })
            .ToListAsync(cancellationToken);

        var periodInvoices = invoices
            .Where(i => i.InvoiceDate.Date >= from && i.InvoiceDate.Date < toExclusive)
            .ToList();

        var unpaidInvoiceCount = invoices.Count(i => i.RemainingAmount > 0);

        var debtsIqd = invoices
            .Where(i => i.Currency == AccountingCurrency.IQD)
            .Sum(i => i.RemainingAmount);
        var debtsUsd = invoices
            .Where(i => i.Currency == AccountingCurrency.USD)
            .Sum(i => i.RemainingAmount);

        var vehicles = await db.CustomsVehicles.AsNoTracking()
            .Where(v => v.Invoice.Status != CustomsInvoiceStatus.Cancelled)
            .Select(v => v.Status)
            .ToListAsync(cancellationToken);

        var vehiclesInProgress = vehicles.Count(s => s == CustomsVehicleStatus.InProgress);
        var vehiclesExited = vehicles.Count(s => s == CustomsVehicleStatus.Exited);
        var vehiclesArrived = vehicles.Count(s => s == CustomsVehicleStatus.Arrived);

        var receipts = await db.CustomsReceipts.AsNoTracking()
            .Where(r => r.ReceiptDate >= from && r.ReceiptDate < toExclusive)
            .Select(r => new { r.Currency, r.Amount })
            .ToListAsync(cancellationToken);

        var receiptsIqd = receipts.Where(r => r.Currency == AccountingCurrency.IQD).Sum(r => r.Amount);
        var receiptsUsd = receipts.Where(r => r.Currency == AccountingCurrency.USD).Sum(r => r.Amount);

        var expenses = await db.CustomsExpenses.AsNoTracking()
            .Where(e => e.ExpenseDate >= from && e.ExpenseDate < toExclusive
                        && e.Invoice.Status != CustomsInvoiceStatus.Cancelled)
            .Select(e => new { e.Currency, e.Amount })
            .ToListAsync(cancellationToken);

        var expensesIqd = expenses.Where(e => e.Currency == AccountingCurrency.IQD).Sum(e => e.Amount);
        var expensesUsd = expenses.Where(e => e.Currency == AccountingCurrency.USD).Sum(e => e.Amount);

        // Same-currency profit: period invoices' totals minus all expenses on those invoices.
        var periodInvoiceIds = periodInvoices.Select(i => i.Id).ToList();
        decimal periodExpenseIqd = 0;
        decimal periodExpenseUsd = 0;
        if (periodInvoiceIds.Count > 0)
        {
            var periodInvoiceExpenses = await db.CustomsExpenses.AsNoTracking()
                .Where(e => periodInvoiceIds.Contains(e.InvoiceId)
                            && e.Invoice.Status != CustomsInvoiceStatus.Cancelled)
                .Select(e => new { e.Currency, e.Amount })
                .ToListAsync(cancellationToken);

            periodExpenseIqd = periodInvoiceExpenses
                .Where(e => e.Currency == AccountingCurrency.IQD)
                .Sum(e => e.Amount);
            periodExpenseUsd = periodInvoiceExpenses
                .Where(e => e.Currency == AccountingCurrency.USD)
                .Sum(e => e.Amount);
        }

        var profitIqd = periodInvoices
            .Where(i => i.Currency == AccountingCurrency.IQD)
            .Sum(i => i.TotalAmount) - periodExpenseIqd;

        var profitUsd = periodInvoices
            .Where(i => i.Currency == AccountingCurrency.USD)
            .Sum(i => i.TotalAmount) - periodExpenseUsd;

        var chartStart = new DateTime(to.Year, to.Month, 1).AddMonths(-11);
        var monthlyInvoices = invoices
            .Where(i => i.InvoiceDate.Date >= chartStart)
            .GroupBy(i => new DateTime(i.InvoiceDate.Year, i.InvoiceDate.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => new CustomsDashboardNameCountPoint
            {
                Name = g.Key.ToString("yyyy/MM"),
                Count = g.Count()
            })
            .ToList();

        // Ensure continuous last-12-month axis even when some months are empty.
        var filledMonthly = new List<CustomsDashboardNameCountPoint>();
        for (var m = 0; m < 12; m++)
        {
            var month = chartStart.AddMonths(m);
            var key = month.ToString("yyyy/MM");
            filledMonthly.Add(new CustomsDashboardNameCountPoint
            {
                Name = key,
                Count = monthlyInvoices.FirstOrDefault(x => x.Name == key)?.Count ?? 0
            });
        }

        return new CustomsDashboardStats
        {
            CustomerCount = customerCount,
            InvoiceCount = periodInvoices.Count,
            VehicleCount = vehicles.Count,
            VehiclesInProgress = vehiclesInProgress,
            VehiclesExited = vehiclesExited,
            VehiclesArrived = vehiclesArrived,
            UnpaidInvoiceCount = unpaidInvoiceCount,
            ReceiptsIqd = receiptsIqd,
            ReceiptsUsd = receiptsUsd,
            ExpensesIqd = expensesIqd,
            ExpensesUsd = expensesUsd,
            DebtsIqd = debtsIqd,
            DebtsUsd = debtsUsd,
            ProfitIqd = profitIqd,
            ProfitUsd = profitUsd,
            MonthlyInvoices = filledMonthly,
            VehicleStatusChart =
            [
                new CustomsDashboardNameCountPoint { Name = "قيد الإجراء", Count = vehiclesInProgress },
                new CustomsDashboardNameCountPoint { Name = "خارجة", Count = vehiclesExited },
                new CustomsDashboardNameCountPoint { Name = "واصلة", Count = vehiclesArrived }
            ]
        };
    }
}
