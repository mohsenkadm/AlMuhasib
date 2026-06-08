using AlMuhasib.Core.Models.Ux;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

public partial class AppNotificationItem : ObservableObject
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public SmartAlertSeverity Severity { get; init; }
    public SmartAlertAction Action { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public string Fingerprint { get; init; } = string.Empty;

    [ObservableProperty]
    private bool _isRead;

    public string TimeAgoText => FormatTimeAgo(CreatedAt);

    public string SeverityIconKind => Severity switch
    {
        SmartAlertSeverity.Critical => "AlertOctagon",
        SmartAlertSeverity.Warning => "AlertCircleOutline",
        _ => "InformationOutline"
    };

    private static string FormatTimeAgo(DateTime createdAt)
    {
        var span = DateTime.Now - createdAt;
        if (span.TotalMinutes < 1)
            return "الآن";
        if (span.TotalMinutes < 60)
            return $"منذ {(int)span.TotalMinutes} د";
        if (span.TotalHours < 24)
            return $"منذ {(int)span.TotalHours} س";
        if (span.TotalDays < 7)
            return $"منذ {(int)span.TotalDays} ي";
        return createdAt.ToString("yyyy/MM/dd HH:mm");
    }
}
