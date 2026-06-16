using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.Infrastructure.Services.Hotel;

public sealed class HotelSmartAlertBridge : ISmartAlertService
{
    private readonly IHotelSmartAlertService _hotelAlerts;

    public HotelSmartAlertBridge(IHotelSmartAlertService hotelAlerts)
    {
        _hotelAlerts = hotelAlerts;
    }

    public async Task<SmartAlertSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var hotelAlerts = await _hotelAlerts.GetAlertsAsync(cancellationToken);
        var alerts = hotelAlerts.Select(MapAlert).ToList();
        var tasks = alerts
            .Where(a => a.Action != SmartAlertAction.None)
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

    private static SmartAlert MapAlert(SmartAlert source) => new()
    {
        Id = source.Id,
        Title = source.Title,
        Message = source.Message,
        Severity = source.Severity,
        Action = source.Action != SmartAlertAction.None ? source.Action : source.Title switch
        {
            "تأخر في المغادرة" => SmartAlertAction.OpenHotelCheckInOut,
            "وصول اليوم" => SmartAlertAction.OpenHotelCheckInOut,
            "غرف تحتاج تنظيف" => SmartAlertAction.OpenHotelHousekeeping,
            _ => SmartAlertAction.None
        },
        Count = source.Count,
        Amount = source.Amount
    };
}
