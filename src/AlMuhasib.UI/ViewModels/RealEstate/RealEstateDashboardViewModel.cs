using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.RealEstate;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.RealEstate;

public partial class RealEstateDashboardViewModel : ViewModelBase
{
    private readonly IRealEstateContractService _contractService;
    private readonly ICurrentUserService _currentUserService;
    private readonly MainWindowViewModel _mainWindow;

    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private int _todayContracts;
    [ObservableProperty] private int _monthContracts;
    [ObservableProperty] private int _unpaidContracts;
    [ObservableProperty] private int _overdueDebts;
    [ObservableProperty] private decimal _totalValue;
    [ObservableProperty] private decimal _totalReceived;
    [ObservableProperty] private decimal _totalRemaining;
    [ObservableProperty] private ISeries[] _monthlySeries = [];
    [ObservableProperty] private ISeries[] _statusSeries = [];
    [ObservableProperty] private Axis[] _monthlyXAxes = [];
    [ObservableProperty] private Axis[] _monthlyYAxes = [];

    public ObservableCollection<RealEstateContractListItem> RecentContracts { get; } = [];

    public RealEstateDashboardViewModel(
        IRealEstateContractService contractService,
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
        LoadPermissions(_currentUserService, RealEstatePermissionRegistry.Dashboard);
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
            OverdueDebts = stats.OverdueDebts;
            TotalValue = stats.TotalValue;
            TotalReceived = stats.TotalReceived;
            TotalRemaining = stats.TotalRemaining;

            RecentContracts.Clear();
            foreach (var item in stats.RecentContracts)
                RecentContracts.Add(item);

            var monthly = stats.MonthlyContracts;
            MonthlySeries = [ChartThemeConfig.Column(monthly.Select(m => (decimal)m.Count).ToArray(), "العقود", 0)];
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
        await _mainWindow.OpenTabAsync(typeof(RealEstateContractsViewModel), "العقود", PackIconKind.FormatListBulleted);

    [RelayCommand]
    private async Task OpenNewContractAsync() =>
        await _mainWindow.OpenTabAsync(typeof(RealEstateContractFormViewModel), "عقد جديد", PackIconKind.FileDocumentPlus, activateIfExists: false);

    [RelayCommand]
    private async Task OpenDebtsAsync() =>
        await _mainWindow.OpenTabAsync(typeof(RealEstateDebtsViewModel), "كشف المدينين", PackIconKind.CashClock);
}
