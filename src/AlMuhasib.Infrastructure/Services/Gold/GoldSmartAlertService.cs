using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldSmartAlertService : IGoldSmartAlertService
{
    private readonly IDbContextFactory<GoldDbContext> _contextFactory;
    private readonly IGoldScaleService? _scaleService;

    public GoldSmartAlertService(
        IDbContextFactory<GoldDbContext> contextFactory,
        IGoldScaleService? scaleService = null)
    {
        _contextFactory = contextFactory;
        _scaleService = scaleService;
    }

    public async Task<IReadOnlyList<GoldAlertItem>> GetAlertsAsync(CancellationToken cancellationToken = default)
    {
        await RefreshAlertsAsync(cancellationToken);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var notifications = await context.GoldNotifications.AsNoTracking()
            .Where(n => !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        return notifications.Select(ToAlert).ToList();
    }

    public async Task<IReadOnlyList<DailyTaskItem>> GetDailyTasksAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var today = DateTime.Today;
        var settings = await GoldSettingsService.EnsureSettingsAsync(context, cancellationToken);
        var tasks = new List<DailyTaskItem>();

        var pricesToday = await context.GoldMithqalPrices.AsNoTracking()
            .AnyAsync(p => p.PriceDate.Date == today, cancellationToken);
        if (!pricesToday)
        {
            tasks.Add(new DailyTaskItem
            {
                Title = "تحديث أسعار المثقال",
                Description = "أسعار اليوم غير مسجّلة — حدّث التسعير قبل البيع",
                Action = SmartAlertAction.OpenGoldMithqalPrices,
                Priority = 1
            });
        }

        var cutoff = today.AddDays(-(settings.OverdueDaysThreshold <= 0 ? 30 : settings.OverdueDaysThreshold));
        var overdueCount = await context.GoldInvoices.AsNoTracking()
            .Where(i => i.CustomerId.HasValue &&
                        i.RemainingAmount > 0 &&
                        i.Status != GoldInvoiceStatus.Cancelled &&
                        i.InvoiceDate.Date <= cutoff)
            .Select(i => i.CustomerId!.Value)
            .Distinct()
            .CountAsync(cancellationToken);
        if (overdueCount > 0)
        {
            tasks.Add(new DailyTaskItem
            {
                Title = "تحصيل الذمم المتأخرة",
                Description = $"{overdueCount} زبون لديهم ذمم متأخرة",
                Action = SmartAlertAction.OpenGoldCollection,
                Priority = 2
            });
        }

        var stockRows = await GoldInventoryService.BuildStockRowsAsync(context, null, cancellationToken);
        var lowStock = stockRows.Where(s => s.IsLowStock).ToList();
        if (lowStock.Count > 0)
        {
            var karatCount = lowStock.Select(s => s.KaratValue).Distinct().Count();
            var warehouseCount = lowStock.Select(s => s.WarehouseId).Distinct().Count();
            tasks.Add(new DailyTaskItem
            {
                Title = "مراجعة المخزون المنخفض",
                Description = $"{karatCount} عيار في {warehouseCount} مخزن تحت الحد",
                Action = SmartAlertAction.OpenGoldStock,
                Priority = 3
            });
        }

        var hasExpenseToday = await context.GoldExpenses.AsNoTracking()
            .AnyAsync(e => e.ExpenseDate.Date == today, cancellationToken);
        if (!hasExpenseToday)
        {
            tasks.Add(new DailyTaskItem
            {
                Title = "تسجيل مصروف اليوم",
                Description = "لا يوجد مصروف مسجّل اليوم — أضف مصروفاً إن وُجد",
                Action = SmartAlertAction.OpenGoldExpenses,
                Priority = 4
            });
        }

        return tasks.OrderBy(t => t.Priority).ToList();
    }

    public async Task RefreshAlertsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var today = DateTime.Today;
        var settings = await GoldSettingsService.EnsureSettingsAsync(context, cancellationToken);

        // Prices not updated today
        var pricesToday = await context.GoldMithqalPrices.AnyAsync(p => p.PriceDate.Date == today, cancellationToken);
        await UpsertOpenAlertAsync(
            context,
            GoldNotificationType.PriceNotUpdated,
            !pricesToday,
            "أسعار المثقال غير محدّثة",
            "لم يتم تحديث أسعار المثقال لليوم. يُفضّل تحديث الأسعار قبل البيع.",
            "GoldMithqalPrice",
            null,
            cancellationToken);

        // Overdue credits
        var cutoff = today.AddDays(-(settings.OverdueDaysThreshold <= 0 ? 30 : settings.OverdueDaysThreshold));
        var overdueCustomers = await context.GoldInvoices
            .Where(i => i.CustomerId.HasValue &&
                        i.RemainingAmount > 0 &&
                        i.Status != GoldInvoiceStatus.Cancelled &&
                        i.InvoiceDate.Date <= cutoff)
            .Select(i => i.CustomerId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        await UpsertOpenAlertAsync(
            context,
            GoldNotificationType.OverdueCredit,
            overdueCustomers.Count > 0,
            "ذمم متأخرة",
            $"يوجد {overdueCustomers.Count} زبون/زبائن لديهم ذمم متأخرة عن {settings.OverdueDaysThreshold} يوماً.",
            "GoldCustomer",
            overdueCustomers.Count > 0 ? overdueCustomers[0] : null,
            cancellationToken);

        // Low stock (karat)
        var stockRows = await GoldInventoryService.BuildStockRowsAsync(context, null, cancellationToken);
        var lowStock = stockRows.Where(s => s.IsLowStock).ToList();
        var lowKaratCount = lowStock.Select(s => s.KaratValue).Distinct().Count();
        await UpsertOpenAlertAsync(
            context,
            GoldNotificationType.LowStock,
            lowKaratCount > 0,
            "مخزون منخفض",
            lowKaratCount == 0
                ? string.Empty
                : $"يوجد {lowKaratCount} عيار بمخزون أقل من الحد ({settings.LowStockAlertGrams} غم).",
            "GoldStockBalance",
            lowStock.FirstOrDefault()?.KaratValue,
            cancellationToken);

        // Low warehouse stock
        var lowWarehouseIds = lowStock.Select(s => s.WarehouseId).Distinct().ToList();
        await UpsertOpenAlertAsync(
            context,
            GoldNotificationType.LowWarehouseStock,
            lowWarehouseIds.Count > 0,
            "مخزون مخزني منخفض",
            lowWarehouseIds.Count == 0
                ? string.Empty
                : $"يوجد {lowWarehouseIds.Count} مخزن/مخازن بأرصدة تحت الحد ({settings.LowStockAlertGrams} غم).",
            "GoldWarehouse",
            lowWarehouseIds.Count > 0 ? lowWarehouseIds[0] : null,
            cancellationToken);

        // No expense recorded today
        var hasExpenseToday = await context.GoldExpenses.AnyAsync(e => e.ExpenseDate.Date == today, cancellationToken);
        await UpsertOpenAlertAsync(
            context,
            GoldNotificationType.NoExpenseToday,
            !hasExpenseToday,
            "لا يوجد مصروف اليوم",
            "لم يُسجَّل أي مصروف لليوم. سجّل المصروفات اليومية إن وُجدت للحفاظ على دقة القاصة.",
            "GoldExpense",
            null,
            cancellationToken);

        // Negative cash
        var negativeCash = await context.GoldCashBoxes
            .Where(c => c.IsActive && c.Balance < 0)
            .ToListAsync(cancellationToken);
        await UpsertOpenAlertAsync(
            context,
            GoldNotificationType.NegativeCash,
            negativeCash.Count > 0,
            "رصيد صندوق سالب",
            negativeCash.Count == 0
                ? string.Empty
                : $"يوجد {negativeCash.Count} صندوق/صناديق برصيد سالب.",
            "GoldCashBox",
            negativeCash.FirstOrDefault()?.Id,
            cancellationToken);

        // Scale disconnected (informational)
        var scaleDisconnected = _scaleService is not null && !_scaleService.IsConnected;
        await UpsertOpenAlertAsync(
            context,
            GoldNotificationType.ScaleDisconnected,
            scaleDisconnected,
            "الميزان غير متصل",
            "ميزان الذهب غير متصل. يمكن إدخال الوزن يدوياً إن كان مسموحاً.",
            "GoldScale",
            null,
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<GoldNotification> Items, int TotalCount)> GetNotificationsPagedAsync(
        int page,
        int pageSize,
        bool unreadOnly = false,
        GoldNotificationType? type = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.GoldNotifications.AsNoTracking().AsQueryable();
        if (unreadOnly)
            query = query.Where(n => !n.IsRead);
        if (type.HasValue)
            query = query.Where(n => n.Type == type.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task MarkAsReadAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var notification = await context.GoldNotifications.FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken)
            ?? throw new InvalidOperationException("الإشعار غير موجود");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var unread = await context.GoldNotifications.Where(n => !n.IsRead).ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = now;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpsertOpenAlertAsync(
        GoldDbContext context,
        GoldNotificationType type,
        bool shouldExist,
        string title,
        string message,
        string? relatedEntity,
        int? relatedId,
        CancellationToken cancellationToken)
    {
        var open = await context.GoldNotifications
            .Where(n => n.Type == type && !n.IsRead)
            .OrderByDescending(n => n.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!shouldExist)
        {
            if (open is not null)
            {
                open.IsRead = true;
                open.ReadAt = DateTime.UtcNow;
            }
            return;
        }

        if (open is null)
        {
            await context.GoldNotifications.AddAsync(new GoldNotification
            {
                Type = type,
                Title = title,
                Message = message,
                RelatedEntity = relatedEntity,
                RelatedId = relatedId,
                IsRead = false
            }, cancellationToken);
            return;
        }

        open.Title = title;
        open.Message = message;
        open.RelatedEntity = relatedEntity;
        open.RelatedId = relatedId;
    }

    private static GoldAlertItem ToAlert(GoldNotification n) => new()
    {
        NotificationId = n.Id,
        Type = n.Type,
        Title = n.Title,
        Message = n.Message,
        RelatedEntity = n.RelatedEntity,
        RelatedId = n.RelatedId,
        CreatedAt = n.CreatedAt,
        IsRead = n.IsRead
    };
}
