using AlMuhasib.Core.Constants;
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
    private readonly CustomsPermissionGuard _permissions;

    public CustomsDashboardService(
        IDbContextFactory<CustomsDbContext> contextFactory,
        CustomsPermissionGuard permissions)
    {
        _contextFactory = contextFactory;
        _permissions = permissions;
    }

    public async Task<CustomsDashboardStats> GetDashboardStatsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        _permissions.EnsureView(CustomsScreenNames.Dashboard);
        var to = (toDate ?? DateTime.Today).Date;
        var from = (fromDate ?? new DateTime(to.Year, to.Month, 1)).Date;
        var toExclusive = to.AddDays(1);
        var chartStart = new DateTime(to.Year, to.Month, 1).AddMonths(-11);

        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var customerCount = await db.CustomsCustomers.AsNoTracking()
            .CountAsync(c => c.IsActive, cancellationToken);

        // H-03: period / debt / profit aggregates in SQL — never load all invoices into memory.
        var invoiceCount = await db.CustomsInvoices.AsNoTracking()
            .CountAsync(
                i => i.Status != CustomsInvoiceStatus.Cancelled
                     && i.InvoiceDate >= from && i.InvoiceDate < toExclusive,
                cancellationToken);

        var unpaidInvoiceCount = await db.CustomsInvoices.AsNoTracking()
            .CountAsync(
                i => i.Status == CustomsInvoiceStatus.Approved && i.RemainingAmount > 0,
                cancellationToken);

        var debtsIqd = await db.CustomsInvoices.AsNoTracking()
            .Where(i => i.Status == CustomsInvoiceStatus.Approved
                        && i.RemainingAmount > 0
                        && i.Currency == AccountingCurrency.IQD)
            .SumAsync(i => (decimal?)i.RemainingAmount, cancellationToken) ?? 0m;

        var debtsUsd = await db.CustomsInvoices.AsNoTracking()
            .Where(i => i.Status == CustomsInvoiceStatus.Approved
                        && i.RemainingAmount > 0
                        && i.Currency == AccountingCurrency.USD)
            .SumAsync(i => (decimal?)i.RemainingAmount, cancellationToken) ?? 0m;

        var vehicleGroups = await db.CustomsVehicles.AsNoTracking()
            .Where(v => v.Invoice.Status != CustomsInvoiceStatus.Cancelled)
            .GroupBy(v => v.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var vehiclesInProgress = vehicleGroups.FirstOrDefault(x => x.Status == CustomsVehicleStatus.InProgress)?.Count ?? 0;
        var vehiclesExited = vehicleGroups.FirstOrDefault(x => x.Status == CustomsVehicleStatus.Exited)?.Count ?? 0;
        var vehiclesArrived = vehicleGroups.FirstOrDefault(x => x.Status == CustomsVehicleStatus.Arrived)?.Count ?? 0;
        var vehicleCount = vehicleGroups.Sum(x => x.Count);

        var receiptsIqd = await db.CustomsReceipts.AsNoTracking()
            .Where(r => r.ReceiptDate >= from && r.ReceiptDate < toExclusive
                        && r.Currency == AccountingCurrency.IQD)
            .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;

        var receiptsUsd = await db.CustomsReceipts.AsNoTracking()
            .Where(r => r.ReceiptDate >= from && r.ReceiptDate < toExclusive
                        && r.Currency == AccountingCurrency.USD)
            .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;

        var expensesIqd = await db.CustomsExpenses.AsNoTracking()
            .Where(e => e.ExpenseDate >= from && e.ExpenseDate < toExclusive
                        && e.Invoice.Status != CustomsInvoiceStatus.Cancelled
                        && e.Currency == AccountingCurrency.IQD)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        var expensesUsd = await db.CustomsExpenses.AsNoTracking()
            .Where(e => e.ExpenseDate >= from && e.ExpenseDate < toExclusive
                        && e.Invoice.Status != CustomsInvoiceStatus.Cancelled
                        && e.Currency == AccountingCurrency.USD)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        // H-01: profit = Approved revenue − same-currency expenses on those invoices (aggregate-equivalent).
        var revenueIqd = await db.CustomsInvoices.AsNoTracking()
            .Where(i => i.Status == CustomsInvoiceStatus.Approved
                        && i.InvoiceDate >= from && i.InvoiceDate < toExclusive
                        && i.Currency == AccountingCurrency.IQD)
            .SumAsync(i => (decimal?)i.TotalAmount, cancellationToken) ?? 0m;

        var revenueUsd = await db.CustomsInvoices.AsNoTracking()
            .Where(i => i.Status == CustomsInvoiceStatus.Approved
                        && i.InvoiceDate >= from && i.InvoiceDate < toExclusive
                        && i.Currency == AccountingCurrency.USD)
            .SumAsync(i => (decimal?)i.TotalAmount, cancellationToken) ?? 0m;

        var profitExpenseIqd = await db.CustomsExpenses.AsNoTracking()
            .Where(e => e.Invoice.Status == CustomsInvoiceStatus.Approved
                        && e.Invoice.InvoiceDate >= from && e.Invoice.InvoiceDate < toExclusive
                        && e.Invoice.Currency == AccountingCurrency.IQD
                        && e.Currency == AccountingCurrency.IQD)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        var profitExpenseUsd = await db.CustomsExpenses.AsNoTracking()
            .Where(e => e.Invoice.Status == CustomsInvoiceStatus.Approved
                        && e.Invoice.InvoiceDate >= from && e.Invoice.InvoiceDate < toExclusive
                        && e.Invoice.Currency == AccountingCurrency.USD
                        && e.Currency == AccountingCurrency.USD)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        var profitIqd = revenueIqd - profitExpenseIqd;
        var profitUsd = revenueUsd - profitExpenseUsd;

        var monthlyRaw = await db.CustomsInvoices.AsNoTracking()
            .Where(i => i.Status != CustomsInvoiceStatus.Cancelled
                        && i.InvoiceDate >= chartStart
                        && i.InvoiceDate < toExclusive)
            .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var monthlyLookup = monthlyRaw.ToDictionary(
            x => new DateTime(x.Year, x.Month, 1).ToString("yyyy/MM"),
            x => x.Count);

        var filledMonthly = new List<CustomsDashboardNameCountPoint>(12);
        for (var m = 0; m < 12; m++)
        {
            var month = chartStart.AddMonths(m);
            var key = month.ToString("yyyy/MM");
            filledMonthly.Add(new CustomsDashboardNameCountPoint
            {
                Name = key,
                Count = monthlyLookup.TryGetValue(key, out var c) ? c : 0
            });
        }

        return new CustomsDashboardStats
        {
            CustomerCount = customerCount,
            InvoiceCount = invoiceCount,
            VehicleCount = vehicleCount,
            VehiclesInProgress = vehiclesInProgress,
            VehiclesExited = vehiclesExited,
            VehiclesArrived = vehiclesArrived,
            UnpaidInvoiceCount = unpaidInvoiceCount,
            ReceiptsIqd = receiptsIqd,
            ReceiptsUsd = receiptsUsd,
            ExpensesIqd = _permissions.CanViewExpenses ? expensesIqd : 0,
            ExpensesUsd = _permissions.CanViewExpenses ? expensesUsd : 0,
            DebtsIqd = debtsIqd,
            DebtsUsd = debtsUsd,
            ProfitIqd = _permissions.CanViewProfit && _permissions.CanViewExpenses ? profitIqd : 0,
            ProfitUsd = _permissions.CanViewProfit && _permissions.CanViewExpenses ? profitUsd : 0,
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
