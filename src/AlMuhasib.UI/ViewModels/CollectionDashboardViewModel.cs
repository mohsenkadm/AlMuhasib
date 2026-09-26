using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class CollectionDashboardViewModel : ViewModelBase
{
    private readonly ICollectionDashboardService _dashboardService;
    private readonly IInstallmentService _installmentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IExportService _exportService;
    private List<CollectionInstallmentRow> _allRows = [];

    [ObservableProperty] private int _dueTodayCount;
    [ObservableProperty] private string _dueTodayAmountText = "0 د.ع";
    [ObservableProperty] private int _overdueCount;
    [ObservableProperty] private string _overdueAmountText = "0 د.ع";
    [ObservableProperty] private int _thisWeekCount;
    [ObservableProperty] private string _thisWeekAmountText = "0 د.ع";
    [ObservableProperty] private string? _selectedBucketFilter;
    [ObservableProperty] private CashBox? _paymentCashBox;

    public ObservableCollection<CollectionInstallmentRow> Rows { get; } = [];
    public ObservableCollection<CashBox> CashBoxes { get; } = [];

    public CollectionDashboardViewModel(
        ICollectionDashboardService dashboardService,
        IInstallmentService installmentService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IExportService exportService)
    {
        _dashboardService = dashboardService;
        _installmentService = installmentService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _exportService = exportService;
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
            DueTodayAmountText = FormatDualAmount(summary.DueTodayAmount, summary.DueTodayAmountUsd);
            OverdueCount = summary.OverdueCount;
            OverdueAmountText = FormatDualAmount(summary.OverdueAmount, summary.OverdueAmountUsd);
            ThisWeekCount = summary.ThisWeekCount;
            ThisWeekAmountText = FormatDualAmount(summary.ThisWeekAmount, summary.ThisWeekAmountUsd);
            Rows.Clear();
            _allRows = summary.Rows.ToList();
            ApplyRowFilters();
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

    protected override void OnColumnFiltersChanged() => ApplyRowFilters();

    private void ApplyRowFilters()
    {
        var filtered = ColumnFilterEngine.Apply(_allRows, ColumnFilters);
        Rows.Clear();
        foreach (var row in filtered)
            Rows.Add(row);
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        if (Rows.Count == 0) return;
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "لوحة_التحصيل.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "العميل", "الهاتف", "الاستحقاق", "الحالة", "المتبقي", "العملة" };
        var data = Rows.Select(r => new object[]
        {
            r.CustomerName, r.CustomerPhone ?? "", r.DueDate.ToString("yyyy/MM/dd"),
            r.StatusLabel, r.RemainingAmount.ToString("N0"), r.CurrencyLabel
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "لوحة التحصيل", cols, (IList<object[]>)data);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void PrintTable()
    {
        if (Rows.Count == 0) return;
        var cols = new[] { "العميل", "الهاتف", "الاستحقاق", "الحالة", "المتبقي", "العملة" };
        var data = Rows.Select(r => new object[]
        {
            r.CustomerName, r.CustomerPhone ?? "", r.DueDate.ToString("yyyy/MM/dd"),
            r.StatusLabel, r.RemainingAmount.ToString("N0"), r.CurrencyLabel
        }).ToList();
        _exportService.PrintTable("لوحة التحصيل اليومية", cols, (IList<object[]>)data);
    }

    [RelayCommand]
    private async Task FilterByBucketAsync(string? bucket)
    {
        SelectedBucketFilter = bucket;
        await RefreshAsync();
    }

    private static string FormatDualAmount(decimal iqd, decimal usd)
    {
        var text = $"{iqd:N0} د.ع";
        if (usd != 0)
            text += $" | {usd:N2} $";
        return text;
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
            BeautifulMessageDialog.ShowSuccess(
                $"تم تسديد {AlMuhasib.Core.Helpers.AccountingCurrencyHelper.Format(row.RemainingAmount, row.Currency)} — {row.CustomerName}");
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
