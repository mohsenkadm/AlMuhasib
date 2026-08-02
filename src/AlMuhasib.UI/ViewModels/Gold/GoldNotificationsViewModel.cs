using System.Collections.ObjectModel;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldNotificationsViewModel : PagedViewModelBase
{
    private readonly IGoldSmartAlertService _alertService;
    private readonly IToastNotificationService _toast;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<GoldAlertItem> Alerts { get; } = [];

    [ObservableProperty] private bool _unreadOnly;
    [ObservableProperty] private GoldAlertItem? _selectedAlert;
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private int _unreadCount;

    public GoldNotificationsViewModel(
        IGoldSmartAlertService alertService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
    {
        _alertService = alertService;
        _toast = toast;
        _currentUserService = currentUserService;
        PageTitle = "التنبيهات";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.Notifications);
        await LoadAsync();
    }

    protected override Task OnPageChangedAsync() => LoadAsync();

    partial void OnUnreadOnlyChanged(bool value) => _ = ReloadAsync();

    private async Task ReloadAsync()
    {
        CurrentPage = 1;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var alerts = await _alertService.GetAlertsAsync();
            if (UnreadOnly)
                alerts = alerts.Where(a => !a.IsRead).ToList();

            ApplyPaginationStats(alerts.Count);
            var page = alerts.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
            Alerts.Clear();
            foreach (var a in page)
                Alerts.Add(a);

            UnreadCount = alerts.Count(a => !a.IsRead);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toast.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshGenerateAsync()
    {
        IsBusy = true;
        try
        {
            await _alertService.RefreshAlertsAsync();
            Message = "تم تحديث وتوليد التنبيهات";
            _toast.ShowSuccess(Message);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toast.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task MarkReadAsync(GoldAlertItem? alert)
    {
        alert ??= SelectedAlert;
        if (alert?.NotificationId is not int id)
        {
            _toast.ShowWarning("لا يمكن تعليم هذا التنبيه كمقروء");
            return;
        }

        try
        {
            await _alertService.MarkAsReadAsync(id);
            alert.IsRead = true;
            UnreadCount = Math.Max(0, UnreadCount - 1);
            _toast.ShowSuccess("تم التعليم كمقروء");
            if (UnreadOnly)
                await LoadAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task MarkAllReadAsync()
    {
        try
        {
            await _alertService.MarkAllAsReadAsync();
            Message = "تم تعليم الكل كمقروء";
            _toast.ShowSuccess(Message);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }
}
