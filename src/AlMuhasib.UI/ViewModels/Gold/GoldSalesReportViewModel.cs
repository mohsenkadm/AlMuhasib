using System.Collections.ObjectModel;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldSalesReportViewModel : ViewModelBase
{
    private readonly IGoldReportService _reportService;
    private readonly IToastNotificationService _toast;
    private readonly ICurrentUserService _currentUserService;
    private readonly MainWindowViewModel _mainWindow;

    public ObservableCollection<GoldInvoiceListItem> Rows { get; } = [];

    public IReadOnlyList<GoldStatusFilterOption> StatusFilters { get; } =
    [
        new(null, "الكل"),
        new(GoldInvoiceStatus.Completed, "مكتمل"),
        new(GoldInvoiceStatus.Open, "مفتوح"),
        new(GoldInvoiceStatus.PartiallyPaid, "جزئي"),
        new(GoldInvoiceStatus.Cancelled, "ملغى")
    ];

    [ObservableProperty] private DateTime? _dateFrom = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime? _dateTo = DateTime.Today;
    [ObservableProperty] private GoldInvoiceStatus? _statusFilter;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private int _saleCount;
    [ObservableProperty] private decimal _totalSalesIqd;
    [ObservableProperty] private decimal _totalSalesUsd;
    [ObservableProperty] private decimal _totalWeightGrams;
    [ObservableProperty] private decimal _totalMakingIqd;
    [ObservableProperty] private GoldInvoiceListItem? _selectedRow;

    public GoldSalesReportViewModel(
        IGoldReportService reportService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService,
        MainWindowViewModel mainWindow)
    {
        _reportService = reportService;
        _toast = toast;
        _currentUserService = currentUserService;
        _mainWindow = mainWindow;
        PageTitle = "تقرير المبيعات";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.SalesReport);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var summary = await _reportService.GetSummaryAsync(DateFrom, DateTo);
            var rows = await _reportService.GetSalesReportAsync(DateFrom, DateTo, StatusFilter);

            Rows.Clear();
            foreach (var r in rows)
                Rows.Add(r);

            SaleCount = summary.SaleCount;
            TotalSalesIqd = summary.TotalSalesIqd;
            TotalSalesUsd = summary.TotalSalesUsd;
            TotalWeightGrams = summary.TotalWeightSoldGrams;
            TotalMakingIqd = summary.TotalMakingChargesIqd;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toast.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateReturnAsync(GoldInvoiceListItem? row)
    {
        row ??= SelectedRow;
        if (row is null)
        {
            _toast.ShowWarning("اختر فاتورة بيع أولاً");
            return;
        }

        await _mainWindow.OpenTabAsync(typeof(GoldSaleReturnViewModel), "مرتجع بيع", PackIconKind.BackupRestore);
        if (_mainWindow.SelectedTab?.ViewModel is GoldSaleReturnViewModel returnVm)
            await returnVm.PrepareFromSaleIdAsync(row.Id);
    }
}
