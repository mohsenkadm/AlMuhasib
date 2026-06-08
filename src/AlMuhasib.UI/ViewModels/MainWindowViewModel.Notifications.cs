using System.Collections.ObjectModel;
using System.Windows.Threading;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class MainWindowViewModel
{
    private readonly INotificationCenterService _notificationCenter;
    private DispatcherTimer? _notificationReminderTimer;
    private bool _notificationStartupDone;

    [ObservableProperty] private bool _isNotificationPanelOpen;
    [ObservableProperty] private bool _isNotificationCenterLoading;
    [ObservableProperty] private int _unreadNotificationCount;
    [ObservableProperty] private string _unreadNotificationBadgeText = "0";
    [ObservableProperty] private bool _isNotificationReminderVisible;
    [ObservableProperty] private string _notificationReminderTitle = string.Empty;
    [ObservableProperty] private string _notificationReminderMessage = string.Empty;

    public ObservableCollection<AppNotificationItem> UnreadNotifications { get; } = [];
    public ObservableCollection<AppNotificationItem> ReadNotifications { get; } = [];

    public bool HasUnreadNotifications => UnreadNotificationCount > 0;

    partial void OnUnreadNotificationCountChanged(int value)
    {
        UnreadNotificationBadgeText = value > 99 ? "99+" : value.ToString();
        OnPropertyChanged(nameof(HasUnreadNotifications));
    }

    public async Task InitializeNotificationCenterAsync()
    {
        StartNotificationReminderTimer();
        await RefreshNotificationsAsync();

        if (!_notificationStartupDone && UnreadNotificationCount > 0)
        {
            _notificationStartupDone = true;
            PresentNotificationReminder(isStartup: true);
        }
    }

    public void ResetNotificationSession()
    {
        _notificationStartupDone = false;
        IsNotificationPanelOpen = false;
        IsNotificationReminderVisible = false;
    }

    [RelayCommand]
    private async Task ToggleNotificationPanelAsync()
    {
        if (IsNotificationPanelOpen)
        {
            IsNotificationPanelOpen = false;
            return;
        }

        IsMenuCustomizerOpen = false;
        IsQuickAssistOpen = false;
        IsSmartAssistantOpen = false;
        IsGlobalSearchOpen = false;
        IsTasksPanelOpen = false;
        IsNotesPanelOpen = false;
        IsNotificationPanelOpen = true;
        await RefreshNotificationsAsync();
    }

    [RelayCommand]
    private void CloseNotificationPanel() => IsNotificationPanelOpen = false;

    [RelayCommand]
    private async Task ReloadNotificationsPanelAsync() => await RefreshNotificationsAsync();

    [RelayCommand]
    private void MarkAllNotificationsRead()
    {
        if (UnreadNotifications.Count == 0)
            return;

        var unread = UnreadNotifications.ToList();
        _notificationCenter.MarkAllRead(unread);

        foreach (var item in unread)
        {
            UnreadNotifications.Remove(item);
            ReadNotifications.Insert(0, item);
        }

        UpdateUnreadCount();
        if (UnreadNotificationCount == 0)
            IsNotificationReminderVisible = false;
    }

    [RelayCommand]
    private void MarkNotificationRead(AppNotificationItem? item)
    {
        if (item is null || item.IsRead)
            return;

        _notificationCenter.MarkRead(item);
        UnreadNotifications.Remove(item);
        ReadNotifications.Insert(0, item);
        UpdateUnreadCount();

        if (UnreadNotificationCount == 0)
            IsNotificationReminderVisible = false;
    }

    [RelayCommand]
    private async Task OpenNotificationAsync(AppNotificationItem? item)
    {
        if (item is null)
            return;

        if (!item.IsRead)
            MarkNotificationRead(item);

        IsNotificationPanelOpen = false;
        IsNotificationReminderVisible = false;

        if (item.Action != SmartAlertAction.None)
            await ExecuteDailyTaskAsync(item.Action);
    }

    [RelayCommand]
    private async Task OpenNotificationsFromReminderAsync()
    {
        IsNotificationReminderVisible = false;
        if (!IsNotificationPanelOpen)
            await ToggleNotificationPanelAsync();
    }

    [RelayCommand]
    private void DismissNotificationReminder() => IsNotificationReminderVisible = false;

    public async Task RefreshNotificationsAsync()
    {
        IsNotificationCenterLoading = true;
        try
        {
            var items = await _notificationCenter.RefreshAsync();
            RebuildNotificationLists(items);
            UpdateUnreadCount();
        }
        catch
        {
            // لا تعطل الواجهة
        }
        finally
        {
            IsNotificationCenterLoading = false;
        }
    }

    private void RebuildNotificationLists(IEnumerable<AppNotificationItem> items)
    {
        var list = items.ToList();
        UnreadNotifications.Clear();
        ReadNotifications.Clear();

        foreach (var item in list.Where(i => !i.IsRead))
            UnreadNotifications.Add(item);

        foreach (var item in list.Where(i => i.IsRead))
            ReadNotifications.Add(item);
    }

    private void UpdateUnreadCount() => UnreadNotificationCount = UnreadNotifications.Count;

    private void StartNotificationReminderTimer()
    {
        _notificationReminderTimer?.Stop();
        _notificationReminderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromHours(1)
        };
        _notificationReminderTimer.Tick += async (_, _) =>
        {
            await RefreshNotificationsAsync();
            if (UnreadNotificationCount > 0)
                PresentNotificationReminder(isStartup: false);
        };
        _notificationReminderTimer.Start();
    }

    private void PresentNotificationReminder(bool isStartup)
    {
        if (UnreadNotificationCount == 0)
            return;

        _sound.Play(SoundEffect.Notification);

        var top = UnreadNotifications.FirstOrDefault();
        NotificationReminderTitle = isStartup
            ? $"لديك {UnreadNotificationCount} {NotificationCountLabel(UnreadNotificationCount)} يحتاج متابعة"
            : "تذكير: تنبيهات غير مقروءة";

        NotificationReminderMessage = top is null
            ? "اضغط لعرض التفاصيل"
            : $"{top.Title} — {top.Message}";

        IsNotificationReminderVisible = true;

        if (isStartup)
        {
            var summary = UnreadNotificationCount == 1
                ? top?.Message ?? "يوجد تنبيه يحتاج متابعتك"
                : $"لديك {UnreadNotificationCount} تنبيهات: {string.Join("، ", UnreadNotifications.Take(3).Select(n => n.Title))}";
            _toast.ShowWarning(summary, "تنبيهات النظام");
        }
    }

    partial void OnIsNotificationPanelOpenChanged(bool value)
    {
        if (value)
            IsNotificationReminderVisible = false;
    }

    private static string NotificationCountLabel(int count) => count switch
    {
        1 => "تنبيه",
        2 => "تنبيهان",
        _ => "تنبيهات"
    };
}
