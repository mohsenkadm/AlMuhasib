using AlMuhasib.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

public partial class UserTaskItem : ObservableObject
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Details { get; init; }
    public DateTime? DueDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? AssignedByDisplayName { get; init; }
    public bool IsAssignedByOther { get; init; }

    [ObservableProperty]
    private UserTaskStatus _status;

    public bool IsCompleted => Status == UserTaskStatus.Completed;

    public string StatusLabel => Status switch
    {
        UserTaskStatus.InProgress => "قيد التنفيذ",
        UserTaskStatus.Completed => "مكتملة",
        _ => "قيد الانتظار"
    };

    public string StatusColor => Status switch
    {
        UserTaskStatus.InProgress => "#1565C0",
        UserTaskStatus.Completed => "#2E7D32",
        _ => "#F9A825"
    };

    public string StatusBackground => Status switch
    {
        UserTaskStatus.InProgress => "#E3F2FD",
        UserTaskStatus.Completed => "#E8F5E9",
        _ => "#FFF8E1"
    };

    public bool IsOverdue =>
        DueDate.HasValue && DueDate.Value.Date < DateTime.Today && Status != UserTaskStatus.Completed;

    public string DueDateDisplay =>
        DueDate.HasValue ? DueDate.Value.ToString("yyyy/MM/dd") : "بدون تاريخ";

    partial void OnStatusChanged(UserTaskStatus value) =>
        OnPropertyChanged(nameof(IsCompleted));
}
