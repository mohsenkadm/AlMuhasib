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

public partial class ReceivablesAgingReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalOutstanding = "0";
    [ObservableProperty] private string _rowCount = "0";
    [ObservableProperty] private string _customerCount = "0";
    [ObservableProperty] private string _bucketCount = "0";

    [ObservableProperty] private DateTime? _asOfDate = DateTime.Today;
    [ObservableProperty] private int? _selectedCustomerId;
    public ObservableCollection<Customer> Customers { get; } = [];

    [ObservableProperty] private ISeries[] _pieSeries = [];

    private List<ReceivablesAgingRow> _allRows = [];
    public ObservableCollection<ReceivablesAgingRow> Rows { get; } = [];

    public ReceivablesAgingReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService,
        IInvoiceService invoiceService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "أعمار الذمم المدينة";
        InitReportActionServices(invoiceService);
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
            var result = await _reportService.GetReceivablesAgingReportAsync(AsOfDate ?? DateTime.Today, SelectedCustomerId);

            TotalOutstanding = FormatCurrency(result.TotalOutstanding);
            RowCount = result.RowCount.ToString("N0");
            CustomerCount = result.CustomerCount.ToString("N0");
            BucketCount = result.Buckets.Count.ToString("N0");
            if (result.Buckets.Count > 0)
                PieSeries = ChartThemeConfig.PieFromNameAmount(result.Buckets.Select(b => new NameAmountPoint { Name = b.BucketName, Amount = b.Amount }).ToList());
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "أعمار_الذمم_المدينة.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "النوع", "العميل", "الاستحقاق", "المتبقي", "أيام التأخير", "الشريحة" };
        var rows = _allRows.Select(r => new object[] { r.SourceType, r.CustomerName, r.DueDate.ToString("yyyy/MM/dd"), r.RemainingAmount, r.DaysOverdue, r.AgingBucket }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "أعمار الذمم المدينة", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "النوع", "العميل", "الاستحقاق", "المتبقي", "أيام التأخير", "الشريحة" };
        var rows = _allRows.Select(r => new object[] { r.SourceType, r.CustomerName, r.DueDate.ToString("yyyy/MM/dd"), r.RemainingAmount, r.DaysOverdue, r.AgingBucket }).ToList();
        _exportService.PrintTable("أعمار الذمم المدينة", cols, rows);
    }
}
