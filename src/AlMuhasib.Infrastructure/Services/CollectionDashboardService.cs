using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class CollectionDashboardService : ICollectionDashboardService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public CollectionDashboardService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<CollectionDashboardSummary> GetDashboardAsync(
        string? bucketFilter = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var today = DateTime.Today;
        var weekEnd = today.AddDays(7);

        var query = context.Installments.AsNoTracking()
            .Include(i => i.InstallmentPlan)
            .ThenInclude(p => p!.Customer)
            .Include(i => i.InstallmentPlan)
            .ThenInclude(p => p!.Invoice)
            .Where(i => i.RemainingAmount > 0 && i.Status != InstallmentStatus.Paid);

        var items = await query.ToListAsync(cancellationToken);

        var rows = new List<CollectionInstallmentRow>();
        foreach (var inst in items)
        {
            var bucket = ClassifyBucket(inst, today, weekEnd);
            if (bucket is null) continue;

            var currency = inst.InstallmentPlan?.Invoice?.Currency ?? AccountingCurrency.IQD;
            rows.Add(new CollectionInstallmentRow
            {
                InstallmentId = inst.Id,
                PlanId = inst.InstallmentPlanId,
                InvoiceId = inst.InstallmentPlan?.InvoiceId,
                CustomerId = inst.InstallmentPlan?.CustomerId ?? 0,
                CustomerName = inst.InstallmentPlan?.Customer?.Name ?? "—",
                CustomerFileNumber = inst.InstallmentPlan?.Customer?.FileNumber,
                CustomerPhone = inst.InstallmentPlan?.Customer?.Phone,
                DueDate = inst.DueDate,
                RemainingAmount = inst.RemainingAmount,
                Currency = currency,
                Bucket = bucket,
                StatusLabel = bucket switch
                {
                    "Overdue" => "متأخر",
                    "Today" => "مستحق اليوم",
                    _ => "هذا الأسبوع"
                }
            });
        }

        if (!string.IsNullOrEmpty(bucketFilter))
            rows = rows.Where(r => r.Bucket == bucketFilter).ToList();

        rows = rows
            .OrderBy(r => r.Bucket == "Overdue" ? 0 : r.Bucket == "Today" ? 1 : 2)
            .ThenBy(r => r.DueDate)
            .ToList();

        static decimal SumIqd(IEnumerable<CollectionInstallmentRow> src) =>
            src.Where(r => r.Currency == AccountingCurrency.IQD).Sum(r => r.RemainingAmount);
        static decimal SumUsd(IEnumerable<CollectionInstallmentRow> src) =>
            src.Where(r => r.Currency == AccountingCurrency.USD).Sum(r => r.RemainingAmount);

        var todayRows = rows.Where(r => r.Bucket == "Today").ToList();
        var overdueRows = rows.Where(r => r.Bucket == "Overdue").ToList();
        var weekRows = rows.Where(r => r.Bucket == "ThisWeek").ToList();

        return new CollectionDashboardSummary
        {
            DueTodayCount = todayRows.Count,
            DueTodayAmount = SumIqd(todayRows),
            DueTodayAmountUsd = SumUsd(todayRows),
            OverdueCount = overdueRows.Count,
            OverdueAmount = SumIqd(overdueRows),
            OverdueAmountUsd = SumUsd(overdueRows),
            ThisWeekCount = weekRows.Count,
            ThisWeekAmount = SumIqd(weekRows),
            ThisWeekAmountUsd = SumUsd(weekRows),
            Rows = rows
        };
    }

    private static string? ClassifyBucket(Core.Entities.Installment inst, DateTime today, DateTime weekEnd)
    {
        if (inst.Status == InstallmentStatus.Overdue || inst.DueDate.Date < today)
            return "Overdue";
        if (inst.DueDate.Date == today)
            return "Today";
        if (inst.DueDate.Date > today && inst.DueDate.Date <= weekEnd)
            return "ThisWeek";
        return null;
    }
}
