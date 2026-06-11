namespace AlMuhasib.UI.Services;

public interface IOfflineReminderService
{
    void Start();
    void Stop();
    event Action<OfflineReminderEvent>? ReminderRaised;
}

public sealed class OfflineReminderEvent
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool IsOverdue { get; init; }
}
