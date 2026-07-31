using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Charts;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class OpeningInstallmentBalancesReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalAmount = "0";
    [ObservableProperty] private string _totalPaid = "0";
    [ObservableProperty] private string _totalRemaining = "0";
    [ObservableProperty] private string _planCount = "0";

    [ObservableProperty] private int? _selectedCustomerId;
    public ObservableCollection<Customer> Customers { get; } = [];

    [ObservableProperty] private ISeries[] _pieSeries = [];

    private List<OpeningInstallmentBalanceRow> _allRows = [];
    public ObservableCollection<OpeningInstallmentBalanceRow> Rows { get; } = [];

    public OpeningInstallmentBalancesReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "ملخص أرصدة افتتاحية الأقساط";
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        foreach (var x in await _unitOfWork.Customers.GetAllAsync()) Customers.Add(x);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetOpeningInstallmentBalancesReportAsync(DateFrom, DateTo, SelectedCustomerId);

            TotalAmount = FormatCurrency(result.TotalAmount);
            TotalPaid = FormatCurrency(result.TotalPaid);
            TotalRemaining = FormatCurrency(result.TotalRemaining);
            PlanCount = result.PlanCount.ToString("N0");
            if (result.StatusChart.Count > 0)
                PieSeries = ChartThemeConfig.PieFromNameAmount(result.StatusChart);
            _allRows = result.Rows;

            CurrentPage = 1;
            UpdatePaginationWithFilters(_allRows, Rows);
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    protected override void OnPageChanged() => UpdatePaginationWithFilters(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "أرصدة_افتتاحية_أقساط.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "التاريخ", "العميل", "الهاتف", "الإجمالي", "المسدد", "المتبقي", "الحالة" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.CustomerName, r.Phone, r.TotalAmount, r.PaidAmount, r.RemainingAmount, r.Status }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "ملخص أرصدة افتتاحية الأقساط", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "التاريخ", "العميل", "الهاتف", "الإجمالي", "المسدد", "المتبقي", "الحالة" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.CustomerName, r.Phone, r.TotalAmount, r.PaidAmount, r.RemainingAmount, r.Status }).ToList();
        _exportService.PrintTable("ملخص أرصدة افتتاحية الأقساط", cols, rows);
    }
}
