using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Car;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.Car;

public partial class CarDashboardViewModel : ViewModelBase
{
    private readonly ICarContractService _contractService;
    private readonly ICurrentUserService _currentUserService;
    private readonly MainWindowViewModel _mainWindow;

    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private int _todayContracts;
    [ObservableProperty] private int _monthContracts;
    [ObservableProperty] private int _unpaidContracts;
    [ObservableProperty] private decimal _totalCarValue;
    [ObservableProperty] private decimal _totalReceived;
    [ObservableProperty] private decimal _totalRemaining;
    [ObservableProperty] private ISeries[] _monthlySeries = [];
    [ObservableProperty] private ISeries[] _statusSeries = [];
    [ObservableProperty] private Axis[] _monthlyXAxes = [];
    [ObservableProperty] private Axis[] _monthlyYAxes = [];

    public ObservableCollection<CarContractListItem> RecentContracts { get; } = [];

    public CarDashboardViewModel(
        ICarContractService contractService,
        ICurrentUserService currentUserService,
        MainWindowViewModel mainWindow)
    {
        _contractService = contractService;
        _currentUserService = currentUserService;
        _mainWindow = mainWindow;
        PageTitle = "لوحة التحكم";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CarPermissionRegistry.Dashboard);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoaded = false;
        try
        {
            var stats = await _contractService.GetDashboardStatsAsync();
            TodayContracts = stats.TodayContracts;
            MonthContracts = stats.MonthContracts;
            UnpaidContracts = stats.UnpaidContracts;
            TotalCarValue = stats.TotalCarValue;
            TotalReceived = stats.TotalReceived;
            TotalRemaining = stats.TotalRemaining;

            RecentContracts.Clear();
            foreach (var item in stats.RecentContracts)
                RecentContracts.Add(item);

            var monthly = stats.MonthlyContracts;
            MonthlySeries =
            [
                    ChartThemeConfig.Column(monthly.Select(m => (decimal)m.Count).ToArray(), "العقود", 0)
            ];
            MonthlyXAxes = [ChartThemeConfig.CreateXAxis(monthly.Select(m => m.Name).ToArray(), -45)];
            MonthlyYAxes = [ChartThemeConfig.CreateYAxis()];

            StatusSeries = stats.PaymentStatusChart
                .Select((p, i) => (ISeries)ChartThemeConfig.Pie(p.Amount, p.Name, i))
                .ToArray();
        }
        finally
        {
            IsLoaded = true;
        }
    }

    [RelayCommand]
    private async Task OpenContractsAsync() =>
        await _mainWindow.OpenTabAsync(typeof(CarContractsViewModel), "العقود", PackIconKind.FormatListBulleted);

    [RelayCommand]
    private async Task OpenNewContractAsync() =>
        await _mainWindow.OpenTabAsync(typeof(CarContractFormViewModel), "عقد جديد", PackIconKind.FileDocumentPlus, activateIfExists: false);
}
