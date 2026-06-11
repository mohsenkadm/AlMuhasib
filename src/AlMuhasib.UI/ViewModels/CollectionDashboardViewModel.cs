using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class CollectionDashboardViewModel : ViewModelBase
{
    private readonly ICollectionDashboardService _dashboardService;
    private readonly IInstallmentService _installmentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    [ObservableProperty] private int _dueTodayCount;
    [ObservableProperty] private decimal _dueTodayAmount;
    [ObservableProperty] private int _overdueCount;
    [ObservableProperty] private decimal _overdueAmount;
    [ObservableProperty] private int _thisWeekCount;
    [ObservableProperty] private decimal _thisWeekAmount;
    [ObservableProperty] private string? _selectedBucketFilter;
    [ObservableProperty] private CashBox? _paymentCashBox;

    public ObservableCollection<CollectionInstallmentRow> Rows { get; } = [];
    public ObservableCollection<CashBox> CashBoxes { get; } = [];

    public CollectionDashboardViewModel(
        ICollectionDashboardService dashboardService,
        IInstallmentService installmentService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _dashboardService = dashboardService;
        _installmentService = installmentService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        PageTitle = "لوحة التحصيل اليومية";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Installments");
        foreach (var cb in await _unitOfWork.CashBoxes.GetAllAsync())
            CashBoxes.Add(cb);
        if (CashBoxes.Count > 0)
            PaymentCashBox = CashBoxes[0];
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsBusy = true;
            await _installmentService.UpdateOverdueStatusesAsync();
            var summary = await _dashboardService.GetDashboardAsync(SelectedBucketFilter);
            DueTodayCount = summary.DueTodayCount;
            DueTodayAmount = summary.DueTodayAmount;
            OverdueCount = summary.OverdueCount;
            OverdueAmount = summary.OverdueAmount;
            ThisWeekCount = summary.ThisWeekCount;
            ThisWeekAmount = summary.ThisWeekAmount;
            Rows.Clear();
            foreach (var row in summary.Rows)
                Rows.Add(row);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task FilterByBucketAsync(string? bucket)
    {
        SelectedBucketFilter = bucket;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task QuickPayAsync(CollectionInstallmentRow? row)
    {
        if (row is null) return;
        if (PaymentCashBox is null)
        {
            BeautifulMessageDialog.ShowWarning("يرجى اختيار القاصة");
            return;
        }

        try
        {
            IsBusy = true;
            await _installmentService.PayInstallmentAsync(row.InstallmentId, row.RemainingAmount, PaymentCashBox.Id);
            BeautifulMessageDialog.ShowSuccess($"تم تسديد {row.RemainingAmount:N0} د.ع — {row.CustomerName}");
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
