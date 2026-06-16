using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Car;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.Car;

public partial class CarContractsReportViewModel : ViewModelBase
{
    private readonly ICarContractReportService _carReportService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;

    [ObservableProperty] private DateTime? _dateFrom;
    [ObservableProperty] private DateTime? _dateTo;
    [ObservableProperty] private CarContractStatusFilter _statusFilter = CarContractStatusFilter.All;
    [ObservableProperty] private decimal _totalCarValue;
    [ObservableProperty] private decimal _totalReceived;
    [ObservableProperty] private decimal _totalRemaining;
    [ObservableProperty] private ISeries[] _monthlySeries = [];
    [ObservableProperty] private ISeries[] _amountSeries = [];
    [ObservableProperty] private ISeries[] _typeSeries = [];
    [ObservableProperty] private Axis[] _monthlyXAxes = [];
    [ObservableProperty] private Axis[] _monthlyYAxes = [];

    public ObservableCollection<CarContractListItem> Rows { get; } = [];

    public CarContractsReportViewModel(
        ICarContractReportService reportService,
        IExportService exportService,
        ICurrentUserService currentUserService)
    {
        _carReportService = reportService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "تقرير العقود";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CarPermissionRegistry.CarContractReports);
        await LoadReportAsync();
    }

    [RelayCommand]
    private async Task LoadReportAsync()
    {
        IsBusy = true;
        try
        {
            var data = await _carReportService.GetReportAsync(new CarContractFilter
            {
                DateFrom = DateFrom,
                DateTo = DateTo,
                StatusFilter = StatusFilter
            });

            Rows.Clear();
            foreach (var row in data.Rows)
                Rows.Add(row);

            TotalCarValue = data.TotalCarValue;
            TotalReceived = data.TotalReceived;
            TotalRemaining = data.TotalRemaining;

            var monthly = data.MonthlyContracts;
            MonthlySeries = [ChartThemeConfig.Column(monthly.Select(m => (decimal)m.Count).ToArray(), "العقود", 0)];
            MonthlyXAxes = [ChartThemeConfig.CreateXAxis(monthly.Select(m => m.Name).ToArray(), -45)];
            MonthlyYAxes = [ChartThemeConfig.CreateYAxis()];

            AmountSeries = ChartThemeConfig.PieFromNameAmount(data.CollectedVsRemaining);
            TypeSeries = data.ByCarType.Select((p, i) =>
                (ISeries)ChartThemeConfig.Pie(p.Count, p.Name, i)).ToArray();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        if (!CanExport || Rows.Count == 0)
            return;

        var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"CarContractsReport_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };
        if (dialog.ShowDialog() != true)
            return;

        var headers = new[] { "رقم العقد", "التاريخ", "البائع", "المشتري", "النوع", "السعر", "الواصل", "المتبقي" };
        var data = Rows.Select(r => new object?[]
        {
            r.ContractNumber, r.ContractDate.ToString("yyyy/MM/dd"), r.SellerName, r.BuyerName,
            r.CarType, r.CarPrice, r.AmountReceived, r.RemainingAmount
        }).ToList();

        _exportService.ExportToExcel(dialog.FileName, "تقرير العقود", headers, data);
    }
}
