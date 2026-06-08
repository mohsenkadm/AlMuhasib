using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Services;

public interface INotificationCenterService
{
    Task<IReadOnlyList<AppNotificationItem>> RefreshAsync(CancellationToken cancellationToken = default);

    void MarkRead(AppNotificationItem item);

    void MarkAllRead(IEnumerable<AppNotificationItem> items);

    bool IsRead(string id, string fingerprint);
}
