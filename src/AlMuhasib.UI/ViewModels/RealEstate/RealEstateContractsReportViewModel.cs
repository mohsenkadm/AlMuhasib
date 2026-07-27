using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.RealEstate;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.RealEstate;

public partial class RealEstateContractsReportViewModel : ViewModelBase
{
    private readonly IRealEstateContractReportService _reportService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;

    public ObservableCollection<RealEstateContractListItem> Rows { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private DateTime? _dateFrom = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime? _dateTo = DateTime.Today;
    [ObservableProperty] private decimal _totalValue;
    [ObservableProperty] private decimal _totalReceived;
    [ObservableProperty] private decimal _totalRemaining;
    [ObservableProperty] private string _totalValueText = "0";
    [ObservableProperty] private string _totalReceivedText = "0";
    [ObservableProperty] private string _totalRemainingText = "0";
    [ObservableProperty] private string _contractsCountText = "0";
    [ObservableProperty] private string _unpaidCountText = "0";
    [ObservableProperty] private ISeries[] _monthlySeries = [];
    [ObservableProperty] private ISeries[] _typeSeries = [];
    [ObservableProperty] private ISeries[] _contractTypeSeries = [];
    [ObservableProperty] private Axis[] _monthlyXAxes = [];
    [ObservableProperty] private Axis[] _monthlyYAxes = [];
    [ObservableProperty] private bool _isCardView;
    [ObservableProperty] private string _tableSearchText = string.Empty;

    private List<RealEstateContractListItem> _allRows = [];

    public ObservableCollection<RealEstateContractListItem> FilteredRows { get; } = [];

    public RealEstateContractsReportViewModel(
        IRealEstateContractReportService reportService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast)
    {
        _reportService = reportService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _toast = toast;
        PageTitle = "تقرير العقود";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, RealEstatePermissionRegistry.Reports);
        await LoadReportAsync();
    }

    [RelayCommand]
    private async Task LoadReportAsync()
    {
        IsBusy = true;
        try
        {
            var data = await _reportService.GetReportAsync(new RealEstateContractFilter
            {
                SearchText = SearchText,
                DateFrom = DateFrom,
                DateTo = DateTo
            });

            Rows.Clear();
            _allRows = data.Rows.ToList();
            foreach (var row in data.Rows)
                Rows.Add(row);
            ApplyTableSearch();

            TotalValue = data.TotalValue;
            TotalReceived = data.TotalReceived;
            TotalRemaining = data.TotalRemaining;
            TotalValueText = data.TotalValue.ToString("N0");
            TotalReceivedText = data.TotalReceived.ToString("N0");
            TotalRemainingText = data.TotalRemaining.ToString("N0");
            ContractsCountText = data.Rows.Count.ToString("N0");
            UnpaidCountText = data.Rows.Count(r => r.RemainingAmount > 0).ToString("N0");

            MonthlySeries = [ChartThemeConfig.Column(data.MonthlyContracts.Select(m => (decimal)m.Count).ToArray(), "العقود", 0)];
            MonthlyXAxes = [ChartThemeConfig.CreateXAxis(data.MonthlyContracts.Select(m => m.Name).ToArray(), -45)];
            MonthlyYAxes = [ChartThemeConfig.CreateYAxis()];
            TypeSeries = data.ByPropertyType
                .Select((p, i) => (ISeries)ChartThemeConfig.Pie(p.Count, p.Name, i))
                .ToArray();
            ContractTypeSeries = data.ByContractType
                .Select((p, i) => (ISeries)ChartThemeConfig.Pie(p.Count, p.Name, i))
                .ToArray();
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnTableSearchTextChanged(string value) => ApplyTableSearch();

    protected override void OnColumnFiltersChanged() => ApplyTableSearch();

    private void ApplyTableSearch()
    {
        FilteredRows.Clear();
        IEnumerable<RealEstateContractListItem> source = _allRows;
        if (!string.IsNullOrWhiteSpace(TableSearchText))
        {
            var term = TableSearchText.Trim();
            source = source.Where(r =>
                r.ContractNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.SellerName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.BuyerName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.ContractType.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.PropertyLocation.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            source = ColumnFilterEngine.Apply(source, ColumnFilters);
        foreach (var row in source)
            FilteredRows.Add(row);
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        if (!CanExport) return;
        var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"RealEstateReport_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };
        if (dialog.ShowDialog() != true) return;
        var headers = new[] { "رقم العقد", "التاريخ", "النوع", "البائع", "المشتري", "السعر", "المدفوع", "المتبقي" };
        var data = Rows.Select(r => new object?[]
        {
            r.ContractNumber, r.ContractDate.ToString("yyyy/MM/dd"), r.ContractType,
            r.SellerName, r.BuyerName, r.TotalPrice, r.AmountPaid, r.RemainingAmount
        }).ToList();
        _exportService.ExportToExcel(dialog.FileName, "التقرير", headers, data);
        _toast.ShowSuccess("تم التصدير");
    }

    [RelayCommand]
    private void PrintReport()
    {
        if (!CanPrint) return;
        var headers = new[] { "رقم العقد", "التاريخ", "البائع", "المشتري", "السعر", "المتبقي" };
        var data = Rows.Select(r => new object?[]
        {
            r.ContractNumber, r.ContractDate.ToString("yyyy/MM/dd"),
            r.SellerName, r.BuyerName, r.TotalPrice, r.RemainingAmount
        }).ToList();
        _exportService.PrintTable("تقرير عقود العقارات", headers, data);
    }
}
