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

public partial class SupplierPaymentsReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalPaid = "0";
    [ObservableProperty] private string _voucherPayments = "0";
    [ObservableProperty] private string _cashPurchases = "0";
    [ObservableProperty] private string _rowCount = "0";

    [ObservableProperty] private int? _selectedSupplierId;
    public ObservableCollection<Supplier> Suppliers { get; } = [];

    [ObservableProperty] private ISeries[] _pieSeries = [];
    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];

    private List<SupplierPaymentRow> _allRows = [];
    public ObservableCollection<SupplierPaymentRow> Rows { get; } = [];

    public SupplierPaymentsReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "كشف مدفوعات الموردين";
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        foreach (var x in await _unitOfWork.Suppliers.GetAllAsync()) Suppliers.Add(x);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetSupplierPaymentsReportAsync(DateFrom, DateTo, SelectedSupplierId);

            TotalPaid = FormatCurrency(result.TotalPaid);
            VoucherPayments = FormatCurrency(result.VoucherPayments);
            CashPurchases = FormatCurrency(result.CashPurchases);
            RowCount = result.RowCount.ToString("N0");
            if (result.BySupplierChart.Count > 0)
                PieSeries = ChartThemeConfig.PieFromNameAmount(result.BySupplierChart);
            if (result.DailyChart.Count > 0)
            {
                DailySeries = [ChartThemeConfig.Column(result.DailyChart.Select(d => d.Amount).ToArray(), "القيمة", 2)];
                DailyXAxes = [ChartThemeConfig.CreateXAxis(result.DailyChart.Select(d => d.Date.ToString("MM/dd")).ToArray())];
                DailyYAxes = [ChartThemeConfig.CreateYAxis()];
            }
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "مدفوعات_الموردين.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "التاريخ", "النوع", "المورد", "المرجع", "المبلغ", "الحساب" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.SourceType, r.SupplierName, r.Reference, r.Amount, r.AccountName }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "كشف مدفوعات الموردين", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "التاريخ", "النوع", "المورد", "المرجع", "المبلغ", "الحساب" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.SourceType, r.SupplierName, r.Reference, r.Amount, r.AccountName }).ToList();
        _exportService.PrintTable("كشف مدفوعات الموردين", cols, rows);
    }
}
