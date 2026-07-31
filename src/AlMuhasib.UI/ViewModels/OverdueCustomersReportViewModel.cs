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

public partial class OverdueCustomersReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalOverdue = "0";
    [ObservableProperty] private string _customerCount = "0";
    [ObservableProperty] private string _itemCount = "0";
    [ObservableProperty] private string _averageDaysOverdue = "0";

    [ObservableProperty] private DateTime? _asOfDate = DateTime.Today;
    [ObservableProperty] private int? _selectedCustomerId;
    public ObservableCollection<Customer> Customers { get; } = [];
    [ObservableProperty] private int? _minDaysOverdue = 1;

    [ObservableProperty] private ISeries[] _pieSeries = [];

    private List<OverdueCustomerRow> _allRows = [];
    public ObservableCollection<OverdueCustomerRow> Rows { get; } = [];

    public OverdueCustomersReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "العملاء المتأخرون";
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
            var result = await _reportService.GetOverdueCustomersReportAsync(AsOfDate ?? DateTime.Today, MinDaysOverdue, SelectedCustomerId);

            TotalOverdue = FormatCurrency(result.TotalOverdue);
            CustomerCount = result.CustomerCount.ToString("N0");
            ItemCount = result.ItemCount.ToString("N0");
            AverageDaysOverdue = result.AverageDaysOverdue.ToString("N1");
            if (result.ByCustomerChart.Count > 0)
                PieSeries = ChartThemeConfig.PieFromNameAmount(result.ByCustomerChart);
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "العملاء_المتأخرون.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "العميل", "الهاتف", "النوع", "الاستحقاق", "المبلغ", "أيام التأخير" };
        var rows = _allRows.Select(r => new object[] { r.CustomerName, r.Phone, r.SourceType, r.DueDate.ToString("yyyy/MM/dd"), r.OverdueAmount, r.DaysOverdue }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "العملاء المتأخرون", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "العميل", "الهاتف", "النوع", "الاستحقاق", "المبلغ", "أيام التأخير" };
        var rows = _allRows.Select(r => new object[] { r.CustomerName, r.Phone, r.SourceType, r.DueDate.ToString("yyyy/MM/dd"), r.OverdueAmount, r.DaysOverdue }).ToList();
        _exportService.PrintTable("العملاء المتأخرون", cols, rows);
    }
}
