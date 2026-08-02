using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.Core.Interfaces.Services.Gold;

public interface IGoldSmartAlertService
{
    Task<IReadOnlyList<GoldAlertItem>> GetAlertsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DailyTaskItem>> GetDailyTasksAsync(CancellationToken cancellationToken = default);
    Task RefreshAlertsAsync(CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<GoldNotification> Items, int TotalCount)> GetNotificationsPagedAsync(
        int page,
        int pageSize,
        bool unreadOnly = false,
        GoldNotificationType? type = null,
        CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(int notificationId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(CancellationToken cancellationToken = default);
}
