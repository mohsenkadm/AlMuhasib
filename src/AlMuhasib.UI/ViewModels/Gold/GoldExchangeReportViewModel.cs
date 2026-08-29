using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldExchangeReportViewModel : GoldReportViewModelBase
{
    private readonly IGoldExchangeService _exchangeService;
    private readonly IGoldCustomerService _customerService;
    private readonly IGoldWarehouseService _warehouseService;
    private List<GoldExchangeReportRow> _allRows = [];

    public ObservableCollection<GoldExchangeReportRow> Rows { get; } = [];
    public ObservableCollection<GoldCustomerListItem> Customers { get; } = [];
    public ObservableCollection<GoldWarehouse> Warehouses { get; } = [];

    public IReadOnlyList<GoldStatusFilterOption> StatusFilters { get; } =
    [
        new(null, "الكل"),
        new(GoldInvoiceStatus.Completed, "مكتمل"),
        new(GoldInvoiceStatus.Open, "مفتوح"),
        new(GoldInvoiceStatus.PartiallyPaid, "جزئي")
    ];

    public IReadOnlyList<GoldPaymentMethodFilterOption> PaymentMethodFilters { get; } =
    [
        new(null, "الكل"),
        new(GoldPaymentMethod.Cash, "نقدي"),
        new(GoldPaymentMethod.Credit, "آجل")
    ];

    public IReadOnlyList<GoldCurrencyFilterOption> CurrencyFilters { get; } =
    [
        new(null, "الكل"),
        new(GoldCurrency.IQD, "د.ع"),
        new(GoldCurrency.USD, "$")
    ];

    [ObservableProperty] private GoldCustomerListItem? _selectedCustomer;
    [ObservableProperty] private GoldWarehouse? _selectedWarehouse;
    [ObservableProperty] private GoldInvoiceStatus? _statusFilter;
    [ObservableProperty] private GoldPaymentMethodFilterOption? _selectedPaymentMethodFilter;
    [ObservableProperty] private GoldCurrencyFilterOption? _selectedCurrencyFilter;
    [ObservableProperty] private decimal? _cashDiffFrom;
    [ObservableProperty] private decimal? _cashDiffTo;
    [ObservableProperty] private string _exchangeCount = "0";
    [ObservableProperty] private string _totalCashDiff = "0";
    [ObservableProperty] private string _totalInWeight = "0";
    [ObservableProperty] private string _totalOutWeight = "0";

    public GoldExchangeReportViewModel(
        IGoldReportService reportService,
        IExportService exportService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService,
        IGoldExchangeService exchangeService,
        IGoldCustomerService customerService,
        IGoldWarehouseService warehouseService)
        : base(reportService, exportService, toast, currentUserService)
    {
        _exchangeService = exchangeService;
        _customerService = customerService;
        _warehouseService = warehouseService;
        PageTitle = "تقرير التبديل";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(CurrentUserService, GoldShopPermissionRegistry.ExchangeReport);
        Customers.Clear();
        var (customers, _) = await _customerService.GetPagedAsync(1, 500, activeOnly: true);
        foreach (var c in customers)
            Customers.Add(c);

        Warehouses.Clear();
        foreach (var w in await _warehouseService.GetAllAsync(activeOnly: true))
            Warehouses.Add(w);

        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var (rows, summary) = await ReportService.GetExchangeReportAsync(
                DateFrom, DateTo,
                SelectedCustomer?.Id,
                SelectedWarehouse?.Id,
                SelectedPaymentMethodFilter?.Value,
                SelectedCurrencyFilter?.Value,
                StatusFilter,
                CashDiffFrom,
                CashDiffTo);

            _allRows = rows.ToList();
            ExchangeCount = summary.ExchangeCount.ToString("N0");
            TotalCashDiff = FormatCurrency(summary.TotalCashDifferenceIqd);
            TotalInWeight = summary.TotalInWeightGrams.ToString("N3");
            TotalOutWeight = summary.TotalOutWeightGrams.ToString("N3");
            CurrentPage = 1;
            UpdatePagination(_allRows, Rows);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Toast.ShowError(ex.Message);
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override void OnPageChanged() => UpdatePagination(_allRows, Rows);

    [RelayCommand]
    private async Task ShowDetailsAsync(GoldExchangeReportRow? row)
    {
        if (row is null)
            return;

        var invoice = await _exchangeService.GetByIdAsync(row.Id);
        if (invoice is null)
        {
            Toast.ShowWarning("لم يتم العثور على الفاتورة");
            return;
        }

        GoldInvoiceDetailDialog.Show(invoice);
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        var cols = new[] { "رقم", "تاريخ", "زبون", "وزن وارد", "وزن صادر", "قيمة وارد", "قيمة صادر", "فرق نقدي", "مدفوع", "متبقي", "مستخدم" };
        var rows = _allRows.Select(r => new object[]
        {
            r.InvoiceNumber, r.InvoiceDate.ToString("yyyy/MM/dd"), r.CustomerName ?? "—",
            r.InWeightGrams, r.OutWeightGrams, r.InTotalValue, r.OutTotalValue,
            r.ExchangeCashDifference, r.PaidAmount, r.RemainingAmount, r.CreatedBy ?? "—"
        }).ToList();
        ExportTable("تبديل_الذهب.xlsx", "تقرير التبديل", cols, rows);
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "رقم", "تاريخ", "زبون", "فرق نقدي", "مدفوع", "متبقي" };
        var rows = _allRows.Select(r => new object[]
        {
            r.InvoiceNumber, r.InvoiceDate.ToString("yyyy/MM/dd"), r.CustomerName ?? "—",
            r.ExchangeCashDifference, r.PaidAmount, r.RemainingAmount
        }).ToList();
        PrintTable("تقرير تبديل الذهب", cols, rows);
    }
}
