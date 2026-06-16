using System.IO;
using System.Text.Json;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.UI.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AlMuhasib.UI.Services;

public sealed class NotificationCenterService : INotificationCenterService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IInvoiceQueueService _queueService;
    private readonly ISystemProfileService _systemProfile;
    private readonly string _statePath;
    private Dictionary<string, string> _readFingerprints = new(StringComparer.Ordinal);

    public NotificationCenterService(
        IServiceScopeFactory scopeFactory,
        IInvoiceQueueService queueService,
        ISystemProfileService systemProfile)
    {
        _scopeFactory = scopeFactory;
        _queueService = queueService;
        _systemProfile = systemProfile;
        _statePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlMuhasib",
            "notification-state.json");
        LoadState();
    }

    public async Task<IReadOnlyList<AppNotificationItem>> RefreshAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var smartAlertService = scope.ServiceProvider.GetRequiredService<ISmartAlertService>();

        if (_systemProfile.ActiveSystem != ApplicationSystemType.HotelManagement)
        {
            var installmentService = scope.ServiceProvider.GetRequiredService<IInstallmentService>();
            await installmentService.UpdateOverdueStatusesAsync();
        }

        var summary = await smartAlertService.GetSummaryAsync(cancellationToken);

        var items = new List<AppNotificationItem>();
        var now = DateTime.Now;

        foreach (var alert in summary.Alerts)
        {
            var id = MapAlertId(alert);
            var fingerprint = BuildFingerprint(alert.Count, alert.Amount, alert.Message);
            items.Add(new AppNotificationItem
            {
                Id = id,
                Title = alert.Title,
                Message = alert.Message,
                Severity = alert.Severity,
                Action = alert.Action,
                CreatedAt = now,
                Fingerprint = fingerprint,
                IsRead = IsRead(id, fingerprint)
            });
        }

        if (_systemProfile.ActiveSystem != ApplicationSystemType.HotelManagement)
            AppendQueueNotifications(items, now);

        return items
            .OrderBy(i => i.IsRead)
            .ThenByDescending(i => (int)i.Severity)
            .ThenBy(i => i.Title)
            .ToList();
    }

    public void MarkRead(AppNotificationItem item)
    {
        _readFingerprints[item.Id] = item.Fingerprint;
        item.IsRead = true;
        SaveState();
    }

    public void MarkAllRead(IEnumerable<AppNotificationItem> items)
    {
        foreach (var item in items.Where(i => !i.IsRead))
        {
            _readFingerprints[item.Id] = item.Fingerprint;
            item.IsRead = true;
        }

        SaveState();
    }

    public bool IsRead(string id, string fingerprint) =>
        _readFingerprints.TryGetValue(id, out var saved) && saved == fingerprint;

    private void AppendQueueNotifications(List<AppNotificationItem> items, DateTime now)
    {
        AddQueueNotification(items, InvoiceQueueKind.Sales, "queue-sales",
            "فواتير مبيعات في الانتظار", SmartAlertAction.OpenSalesInvoiceQueue,
            SmartAlertSeverity.Warning, now);
        AddQueueNotification(items, InvoiceQueueKind.Purchase, "queue-purchase",
            "فواتير مشتريات في الانتظار", SmartAlertAction.OpenPurchaseInvoiceQueue,
            SmartAlertSeverity.Info, now);
        AddQueueNotification(items, InvoiceQueueKind.Installment, "queue-installment",
            "فواتير أقساط في الانتظار", SmartAlertAction.OpenInstallmentInvoiceQueue,
            SmartAlertSeverity.Warning, now);
    }

    private void AddQueueNotification(
        List<AppNotificationItem> items,
        InvoiceQueueKind kind,
        string id,
        string title,
        SmartAlertAction action,
        SmartAlertSeverity severity,
        DateTime now)
    {
        var queueItems = _queueService.GetItems(kind);
        if (queueItems.Count == 0)
            return;

        var total = queueItems.Sum(x => x.TotalAmount);
        var message = queueItems.Count == 1
            ? $"فاتورة واحدة بانتظار الإكمال ({queueItems[0].Name})"
            : $"{queueItems.Count} فواتير بانتظار الإكمال بإجمالي {total:N0} د.ع";

        var fingerprint = BuildFingerprint(queueItems.Count, total, message);
        items.Add(new AppNotificationItem
        {
            Id = id,
            Title = title,
            Message = message,
            Severity = severity,
            Action = action,
            CreatedAt = now,
            Fingerprint = fingerprint,
            IsRead = IsRead(id, fingerprint)
        });
    }

    private static string MapAlertId(SmartAlert alert) => alert.Action switch
    {
        SmartAlertAction.OpenInstallments when alert.Severity == SmartAlertSeverity.Critical => "overdue-installments",
        SmartAlertAction.OpenInstallments => "due-today-installments",
        SmartAlertAction.OpenStockHealthReport => "low-stock",
        SmartAlertAction.OpenUnpaidSales => "unpaid-sales",
        SmartAlertAction.OpenUnpaidPurchases => "unpaid-purchases",
        SmartAlertAction.OpenHotelCheckInOut => "hotel-checkinout",
        SmartAlertAction.OpenHotelRooms => "hotel-rooms",
        SmartAlertAction.OpenHotelHousekeeping => "hotel-housekeeping",
        _ => alert.Id
    };

    private static string BuildFingerprint(int count, decimal? amount, string message) =>
        $"{count}|{amount?.ToString("F0") ?? "0"}|{message}";

    private void LoadState()
    {
        try
        {
            if (!File.Exists(_statePath))
                return;

            var json = File.ReadAllText(_statePath);
            var state = JsonSerializer.Deserialize<NotificationState>(json);
            _readFingerprints = state?.ReadFingerprints ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }
        catch
        {
            _readFingerprints = new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    private void SaveState()
    {
        try
        {
            var dir = Path.GetDirectoryName(_statePath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(new NotificationState { ReadFingerprints = _readFingerprints });
            File.WriteAllText(_statePath, json);
        }
        catch
        {
            // ignore persistence failures
        }
    }

    private sealed class NotificationState
    {
        public Dictionary<string, string> ReadFingerprints { get; set; } = new(StringComparer.Ordinal);
    }
}
