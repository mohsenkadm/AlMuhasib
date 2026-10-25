using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace AlMuhasib.UI.ViewModels;

public partial class MaintenanceReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalCount = "0";
    [ObservableProperty] private string _workingCount = "0";
    [ObservableProperty] private string _notWorkingCount = "0";
    [ObservableProperty] private string _underRepairCount = "0";
    [ObservableProperty] private string _deliveredCount = "0";
    [ObservableProperty] private string _totalCostAmount = "0";
    [ObservableProperty] private string _totalProfit = "0";

    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private string _customerSearchText = string.Empty;
    [ObservableProperty] private MaintenanceStatus? _selectedStatus;
    [ObservableProperty] private string _productFilter = string.Empty;
    [ObservableProperty] private string _serialFilter = string.Empty;

    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<Customer> FilteredCustomers { get; } = [];
    public IReadOnlyList<MaintenanceStatusOption> StatusFilterOptions { get; } = MaintenanceStatusHelper.FilterOptions;

    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];
    [ObservableProperty] private ISeries[] _statusSeries = [];
    [ObservableProperty] private Axis[] _statusXAxes = [];
    [ObservableProperty] private Axis[] _statusYAxes = [];

    private List<MaintenanceReportRow> _allRows = [];
    public ObservableCollection<MaintenanceReportRow> Rows { get; } = [];

    public MaintenanceReportViewModel(
        IReportService reportService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "تقرير الصيانة";
        DateFrom = DateTime.Today.AddMonths(-1);
        DateTo = DateTime.Today;
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, ScreenPermissionRegistry.MaintenanceReport);
        Customers.Clear();
        FilteredCustomers.Clear();
        foreach (var c in (await _unitOfWork.Customers.GetAllAsync()).OrderBy(c => c.Name))
        {
            Customers.Add(c);
            FilteredCustomers.Add(c);
        }

        await LoadDataAsync();
    }

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        if (value is not null)
            CustomerSearchText = value.Name;
    }

    partial void OnCustomerSearchTextChanged(string value)
    {
        if (SelectedCustomer is not null && SelectedCustomer.Name == value)
            return;
        SelectedCustomer = null;
        CustomerComboBoxFilter.Apply(Customers, FilteredCustomers, value);
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetMaintenanceReportAsync(
                DateFrom, DateTo, SelectedCustomer?.Id, SelectedStatus,
                string.IsNullOrWhiteSpace(ProductFilter) ? null : ProductFilter.Trim(),
                string.IsNullOrWhiteSpace(SerialFilter) ? null : SerialFilter.Trim());

            TotalCount = result.TotalCount.ToString("N0");
            WorkingCount = result.WorkingCount.ToString("N0");
            NotWorkingCount = result.NotWorkingCount.ToString("N0");
            UnderRepairCount = result.UnderRepairCount.ToString("N0");
            DeliveredCount = result.DeliveredCount.ToString("N0");
            TotalCostAmount = FormatCurrency(result.TotalCostAmount);
            TotalProfit = FormatCurrency(result.TotalProfit);

            if (result.DailyChart.Count > 0)
            {
                DailySeries = [ChartThemeConfig.Line(result.DailyChart.Select(d => d.Amount).ToArray(), "تكاليف الصيانة", 0)];
                DailyXAxes = [ChartThemeConfig.CreateXAxis(result.DailyChart.Select(d => d.Date.ToString("MM/dd")).ToArray())];
                DailyYAxes = [ChartThemeConfig.CreateYAxis()];
            }
            else
            {
                DailySeries = [];
                DailyXAxes = [];
                DailyYAxes = [];
            }

            if (result.StatusChart.Count > 0)
            {
                StatusSeries = [ChartThemeConfig.Column(result.StatusChart.Select(s => s.Amount).ToArray(), "حسب الحالة", 1)];
                StatusXAxes = [ChartThemeConfig.CreateXAxis(result.StatusChart.Select(s => s.Name).ToArray())];
                StatusYAxes = [ChartThemeConfig.CreateYAxis()];
            }
            else
            {
                StatusSeries = [];
                StatusXAxes = [];
                StatusYAxes = [];
            }

            _allRows = result.Rows;
            CurrentPage = 1;
            UpdatePaginationWithFilters(_allRows, Rows);
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

    protected override void OnPageChanged() => UpdatePaginationWithFilters(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "تقرير_الصيانة.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "الرقم", "التاريخ", "الزبون", "المنتج", "سيريال السابق", "سيريال الجديد", "المبلغ", "الحالة", "انتهاء الضمان", "حالة الضمان", "المتبقي", "ملاحظات" };
        var rows = _allRows.Select(r => new object[]
        {
            r.RecordNumber, r.Date.ToString("yyyy/MM/dd"), r.CustomerName, r.ProductName,
            r.PreviousSerialNumber ?? "", r.SerialNumber,
            r.CostAmount, r.StatusLabel, r.WarrantyEndDate?.ToString("yyyy/MM/dd") ?? "",
            r.WarrantyStatusLabel, r.WarrantyRemainingLabel, r.Notes ?? ""
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "الصيانة", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "الرقم", "التاريخ", "الزبون", "المنتج", "سيريال السابق", "سيريال الجديد", "المبلغ", "الحالة", "الضمان" };
        var rows = _allRows.Select(r => new object[]
        {
            r.RecordNumber, r.Date.ToString("yyyy/MM/dd"), r.CustomerName, r.ProductName,
            r.PreviousSerialNumber ?? "", r.SerialNumber,
            r.CostAmount.ToString("N0"), r.StatusLabel, WarrantyStatusHelper.CombinedLabel(r.WarrantyEndDate)
        }).ToList();
        _exportService.PrintTable("تقرير الصيانة", cols, rows);
    }
}
