using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldNotificationsViewModel : PagedViewModelBase
{
    private readonly IGoldSmartAlertService _alertService;
    private readonly IExportService _exportService;
    private readonly IToastNotificationService _toast;
    private readonly ICurrentUserService _currentUserService;
    private List<GoldAlertItem> _allAlerts = [];

    public ObservableCollection<GoldAlertItem> Alerts { get; } = [];

    [ObservableProperty] private bool _unreadOnly;
    [ObservableProperty] private GoldAlertItem? _selectedAlert;
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private int _unreadCount;

    public GoldNotificationsViewModel(
        IGoldSmartAlertService alertService,
        IExportService exportService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
    {
        _alertService = alertService;
        _exportService = exportService;
        _toast = toast;
        _currentUserService = currentUserService;
        PageTitle = "التنبيهات";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.Notifications);
        await LoadAsync();
    }

    protected override Task OnPageChangedAsync()
    {
        ApplyDisplay();
        return Task.CompletedTask;
    }

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        ApplyDisplay();
    }

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

            _allAlerts = alerts.ToList();
            UnreadCount = _allAlerts.Count(a => !a.IsRead);
            ApplyDisplay();
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

    private void ApplyDisplay()
    {
        var filtered = MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters)
            ? ColumnFilterEngine.Apply(_allAlerts, ColumnFilters)
            : _allAlerts.ToList();

        ApplyPaginationStats(filtered.Count);
        var page = filtered.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
        Alerts.Clear();
        foreach (var a in page)
            Alerts.Add(a);
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

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            if (_allAlerts.Count == 0)
                await LoadAsync();

            var exportData = _allAlerts.Select(a => new
            {
                النوع = a.Type.ToString(),
                العنوان = a.Title,
                الرسالة = a.Message,
                التاريخ = a.CreatedAt.ToString("yyyy/MM/dd HH:mm"),
                مقروء = a.IsRead ? "نعم" : "لا"
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"تنبيهات_الذهب_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "التنبيهات");
                BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء التصدير: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PrintTable()
    {
        try
        {
            if (_allAlerts.Count == 0)
                await LoadAsync();

            var columns = new[] { "النوع", "العنوان", "الرسالة", "التاريخ", "مقروء" };
            IList<object[]> rows = _allAlerts.Select(a => new object[]
            {
                a.Type.ToString(),
                a.Title,
                a.Message,
                a.CreatedAt.ToString("yyyy/MM/dd HH:mm"),
                a.IsRead ? "نعم" : "لا"
            }).ToList();
            _exportService.PrintTable("قائمة التنبيهات", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }
}
