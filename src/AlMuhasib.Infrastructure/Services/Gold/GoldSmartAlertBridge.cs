using AlMuhasib.Core.Enums.Gold;
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
        var tasks = await _goldAlerts.GetDailyTasksAsync(cancellationToken);
        var alerts = goldAlerts.Select(MapAlert).ToList();

        return new SmartAlertSummary
        {
            Alerts = alerts,
            DailyTasks = tasks.ToList()
        };
    }

    private static SmartAlert MapAlert(GoldAlertItem source)
    {
        var (severity, action) = source.Type switch
        {
            GoldNotificationType.OverdueCredit => (SmartAlertSeverity.Critical, SmartAlertAction.OpenGoldCollection),
            GoldNotificationType.NegativeCash => (SmartAlertSeverity.Critical, SmartAlertAction.OpenGoldExpenses),
            GoldNotificationType.LowStock => (SmartAlertSeverity.Warning, SmartAlertAction.OpenGoldStock),
            GoldNotificationType.LowWarehouseStock => (SmartAlertSeverity.Warning, SmartAlertAction.OpenGoldWarehouses),
            GoldNotificationType.PriceNotUpdated => (SmartAlertSeverity.Warning, SmartAlertAction.OpenGoldMithqalPrices),
            GoldNotificationType.NoExpenseToday => (SmartAlertSeverity.Info, SmartAlertAction.OpenGoldExpenses),
            GoldNotificationType.ScaleDisconnected => (SmartAlertSeverity.Info, SmartAlertAction.None),
            _ => (SmartAlertSeverity.Info, SmartAlertAction.None)
        };

        return new SmartAlert
        {
            Title = source.Title,
            Message = source.Message,
            Severity = severity,
            Action = action,
            Count = 1
        };
    }
}
