using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldSmartAlertBridge : ISmartAlertService
{
    private readonly IGoldSmartAlertService _goldAlerts;

    public GoldSmartAlertBridge(IGoldSmartAlertService goldAlerts)
    {
        _goldAlerts = goldAlerts;
    }

    public async Task<SmartAlertSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var goldAlerts = await _goldAlerts.GetAlertsAsync(cancellationToken);
        var alerts = goldAlerts.Select(MapAlert).ToList();
        var tasks = alerts
            .Where(a => a.Severity is SmartAlertSeverity.Warning or SmartAlertSeverity.Critical)
            .Select((a, i) => new DailyTaskItem
            {
                Title = a.Title,
                Description = a.Message,
                Action = a.Action,
                Priority = i + 1
            })
            .ToList();

        return new SmartAlertSummary
        {
            Alerts = alerts,
            DailyTasks = tasks
        };
    }

    private static SmartAlert MapAlert(GoldAlertItem source)
    {
        var severity = source.Type switch
        {
            Core.Enums.Gold.GoldNotificationType.OverdueCredit => SmartAlertSeverity.Critical,
            Core.Enums.Gold.GoldNotificationType.NegativeCash => SmartAlertSeverity.Critical,
            Core.Enums.Gold.GoldNotificationType.LowStock => SmartAlertSeverity.Warning,
            Core.Enums.Gold.GoldNotificationType.PriceNotUpdated => SmartAlertSeverity.Warning,
            Core.Enums.Gold.GoldNotificationType.ScaleDisconnected => SmartAlertSeverity.Info,
            _ => SmartAlertSeverity.Info
        };

        return new SmartAlert
        {
            Title = source.Title,
            Message = source.Message,
            Severity = severity,
            Action = SmartAlertAction.None,
            Count = 1
        };
    }
}
