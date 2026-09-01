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
            .Where(i => i.RemainingAmount > 0 && i.Status != InstallmentStatus.Paid);

        var items = await query.ToListAsync(cancellationToken);

        var rows = new List<CollectionInstallmentRow>();
        foreach (var inst in items)
        {
            var bucket = ClassifyBucket(inst, today, weekEnd);
            if (bucket is null) continue;

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

        return new CollectionDashboardSummary
        {
            DueTodayCount = rows.Count(r => r.Bucket == "Today"),
            DueTodayAmount = rows.Where(r => r.Bucket == "Today").Sum(r => r.RemainingAmount),
            OverdueCount = rows.Count(r => r.Bucket == "Overdue"),
            OverdueAmount = rows.Where(r => r.Bucket == "Overdue").Sum(r => r.RemainingAmount),
            ThisWeekCount = rows.Count(r => r.Bucket == "ThisWeek"),
            ThisWeekAmount = rows.Where(r => r.Bucket == "ThisWeek").Sum(r => r.RemainingAmount),
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
