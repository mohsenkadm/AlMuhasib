using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class SmartAlertService : ISmartAlertService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private const decimal LowStockThreshold = 5m;

    public SmartAlertService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<SmartAlertSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var today = DateTime.Today;
        var alerts = new List<SmartAlert>();
        var tasks = new List<DailyTaskItem>();

        var overdue = await context.Installments.AsNoTracking()
            .Where(i => i.Status == InstallmentStatus.Overdue && i.RemainingAmount > 0)
            .ToListAsync(cancellationToken);

        if (overdue.Count > 0)
        {
            var total = overdue.Sum(i => i.RemainingAmount);
            alerts.Add(new SmartAlert
            {
                Title = "أقساط متأخرة",
                Message = $"{overdue.Count} قسط متأخر بإجمالي {total:N0} د.ع",
                Severity = SmartAlertSeverity.Critical,
                Action = SmartAlertAction.OpenInstallments,
                Count = overdue.Count,
                Amount = total
            });
            tasks.Add(new DailyTaskItem
            {
                Title = "تحصيل الأقساط المتأخرة",
                Description = $"{overdue.Count} قسط",
                Action = SmartAlertAction.OpenInstallments,
                Priority = 1
            });
        }

        var dueToday = await context.Installments.AsNoTracking()
            .Where(i => i.DueDate.Date == today
                        && i.RemainingAmount > 0
                        && i.Status != InstallmentStatus.Paid)
            .CountAsync(cancellationToken);

        if (dueToday > 0)
        {
            alerts.Add(new SmartAlert
            {
                Title = "أقساط مستحقة اليوم",
                Message = $"{dueToday} قسط يستحق التسديد اليوم",
                Severity = SmartAlertSeverity.Warning,
                Action = SmartAlertAction.OpenInstallments,
                Count = dueToday
            });
            tasks.Add(new DailyTaskItem
            {
                Title = "تسديد أقساط اليوم",
                Description = $"{dueToday} قسط",
                Action = SmartAlertAction.OpenInstallments,
                Priority = 2
            });
        }

        var weekEnd = today.AddDays(7);
        var dueThisWeek = await context.Installments.AsNoTracking()
            .Where(i => i.DueDate.Date > today
                        && i.DueDate.Date <= weekEnd
                        && i.RemainingAmount > 0
                        && i.Status != InstallmentStatus.Paid)
            .ToListAsync(cancellationToken);

        if (dueThisWeek.Count > 0)
        {
            var weekTotal = dueThisWeek.Sum(i => i.RemainingAmount);
            alerts.Add(new SmartAlert
            {
                Title = "أقساط هذا الأسبوع",
                Message = $"{dueThisWeek.Count} قسط بإجمالي {weekTotal:N0} د.ع",
                Severity = SmartAlertSeverity.Info,
                Action = SmartAlertAction.OpenCollectionDashboard,
                Count = dueThisWeek.Count,
                Amount = weekTotal
            });
        }

        // منتجات تحت الحد الأدنى المعرّف لكل مخزن
        var belowMin = await context.WarehouseStocks.AsNoTracking()
            .Where(ws => ws.MinQuantity > 0 && ws.Quantity < ws.MinQuantity)
            .Select(ws => ws.ProductId)
            .Distinct()
            .CountAsync(cancellationToken);

        // منتجات بلا حد أدنى معرّف وما زالت تحت العتبة الافتراضية
        var lowStockFallback = await context.WarehouseStocks.AsNoTracking()
            .GroupBy(ws => ws.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Qty = g.Sum(x => x.Quantity),
                HasMin = g.Any(x => x.MinQuantity > 0)
            })
            .Where(x => !x.HasMin && x.Qty > 0 && x.Qty <= LowStockThreshold)
            .CountAsync(cancellationToken);

        var lowStock = belowMin + lowStockFallback;
        if (lowStock > 0)
        {
            var message = belowMin > 0
                ? $"{belowMin} منتج تحت الحد الأدنى" + (lowStockFallback > 0 ? $" و{lowStockFallback} بكمية منخفضة" : "")
                : $"{lowStockFallback} منتج بكمية {LowStockThreshold:N0} أو أقل";

            alerts.Add(new SmartAlert
            {
                Title = "مخزون منخفض",
                Message = message,
                Severity = SmartAlertSeverity.Warning,
                Action = SmartAlertAction.OpenStockHealthReport,
                Count = lowStock
            });
            tasks.Add(new DailyTaskItem
            {
                Title = "مراجعة المخزون المنخفض",
                Description = $"{lowStock} منتج",
                Action = SmartAlertAction.OpenStockHealthReport,
                Priority = 3
            });
        }

        var unpaidSales = await context.Invoices.AsNoTracking()
            .Where(i => (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment)
                        && i.PaymentMethod == PaymentMethod.Credit
                        && i.RemainingAmount > 0)
            .CountAsync(cancellationToken);

        if (unpaidSales > 0)
        {
            alerts.Add(new SmartAlert
            {
                Title = "مبيعات آجلة غير مسددة",
                Message = $"{unpaidSales} فاتورة مبيعات بانتظار التحصيل",
                Severity = SmartAlertSeverity.Warning,
                Action = SmartAlertAction.OpenUnpaidSales,
                Count = unpaidSales
            });
            tasks.Add(new DailyTaskItem
            {
                Title = "تحصيل فواتير المبيعات الآجلة",
                Description = $"{unpaidSales} فاتورة",
                Action = SmartAlertAction.OpenUnpaidSales,
                Priority = 4
            });
        }

        var unpaidPurchases = await context.Invoices.AsNoTracking()
            .Where(i => i.InvoiceType == InvoiceType.Purchase
                        && i.PaymentMethod == PaymentMethod.Credit
                        && i.RemainingAmount > 0)
            .CountAsync(cancellationToken);

        if (unpaidPurchases > 0)
        {
            alerts.Add(new SmartAlert
            {
                Title = "مشتريات آجلة غير مسددة",
                Message = $"{unpaidPurchases} فاتورة مشتريات بانتظار السداد",
                Severity = SmartAlertSeverity.Info,
                Action = SmartAlertAction.OpenUnpaidPurchases,
                Count = unpaidPurchases
            });
        }

        return new SmartAlertSummary
        {
            Alerts = alerts,
            DailyTasks = tasks.OrderBy(t => t.Priority).ToList()
        };
    }
}
