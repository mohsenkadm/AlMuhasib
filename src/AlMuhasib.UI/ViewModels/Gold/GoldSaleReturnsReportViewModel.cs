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
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldSaleReturnsReportViewModel : GoldReportViewModelBase
{
    private readonly IGoldSaleService _saleService;
    private readonly IGoldCustomerService _customerService;
    private readonly IGoldWarehouseService _warehouseService;
    private readonly MainWindowViewModel _mainWindow;
    private List<GoldSaleReturnReportRow> _allRows = [];

    public ObservableCollection<GoldSaleReturnReportRow> Rows { get; } = [];
    public ObservableCollection<GoldCustomerListItem> Customers { get; } = [];
    public ObservableCollection<GoldWarehouse> Warehouses { get; } = [];

    public IReadOnlyList<GoldStatusFilterOption> StatusFilters { get; } =
    [
        new(null, "الكل"),
        new(GoldInvoiceStatus.Completed, "مكتمل"),
        new(GoldInvoiceStatus.Open, "مفتوح"),
        new(GoldInvoiceStatus.PartiallyPaid, "جزئي")
    ];

    [ObservableProperty] private GoldCustomerListItem? _selectedCustomer;
    [ObservableProperty] private GoldWarehouse? _selectedWarehouse;
    [ObservableProperty] private GoldInvoiceStatus? _statusFilter;
    [ObservableProperty] private string _relatedInvoiceSearch = string.Empty;
    [ObservableProperty] private string _userNameFilter = string.Empty;
    [ObservableProperty] private string _returnCount = "0";
    [ObservableProperty] private string _totalIqd = "0";
    [ObservableProperty] private string _totalUsd = "0";
    [ObservableProperty] private string _totalWeight = "0";

    public GoldSaleReturnsReportViewModel(
        IGoldReportService reportService,
        IExportService exportService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService,
        IGoldSaleService saleService,
        IGoldCustomerService customerService,
        IGoldWarehouseService warehouseService,
        MainWindowViewModel mainWindow)
        : base(reportService, exportService, toast, currentUserService)
    {
        _saleService = saleService;
        _customerService = customerService;
        _warehouseService = warehouseService;
        _mainWindow = mainWindow;
        PageTitle = "تقرير مرتجعات البيع";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(CurrentUserService, GoldShopPermissionRegistry.SaleReturnsReport);
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
            var (rows, summary) = await ReportService.GetSaleReturnsReportAsync(
                DateFrom, DateTo,
                SelectedCustomer?.Id,
                SelectedWarehouse?.Id,
                StatusFilter,
                string.IsNullOrWhiteSpace(RelatedInvoiceSearch) ? null : RelatedInvoiceSearch.Trim(),
                string.IsNullOrWhiteSpace(UserNameFilter) ? null : UserNameFilter.Trim());

            _allRows = rows.ToList();
            ReturnCount = summary.ReturnCount.ToString("N0");
            TotalIqd = FormatCurrency(summary.TotalAmountIqd);
            TotalUsd = $"{summary.TotalAmountUsd:N2} $";
            TotalWeight = summary.TotalWeightGrams.ToString("N3");
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
    private async Task ShowDetailsAsync(GoldSaleReturnReportRow? row)
    {
        if (row is null)
            return;

        var invoice = await _saleService.GetByIdAsync(row.Id);
        if (invoice is null)
        {
            Toast.ShowWarning("لم يتم العثور على المرتجع");
            return;
        }

        GoldInvoiceDetailDialog.Show(invoice);
    }

    [RelayCommand]
    private async Task CreateReturnFromSaleAsync(GoldSaleReturnReportRow? row)
    {
        if (row?.RelatedInvoiceId is not int saleId)
        {
            Toast.ShowWarning("لا توجد فاتورة أصلية مرتبطة");
            return;
        }

        await _mainWindow.OpenTabAsync(typeof(GoldSaleReturnViewModel), "مرتجع بيع", PackIconKind.BackupRestore);
        if (_mainWindow.SelectedTab?.ViewModel is GoldSaleReturnViewModel returnVm)
            await returnVm.PrepareFromSaleIdAsync(saleId);
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        var cols = new[] { "رقم المرتجع", "تاريخ", "زبون", "فاتورة أصلية", "وزن", "د.ع", "$", "حالة", "ملاحظات" };
        var rows = _allRows.Select(r => new object[]
        {
            r.InvoiceNumber, r.InvoiceDate.ToString("yyyy/MM/dd"), r.CustomerName ?? "—",
            r.RelatedInvoiceNumber ?? "—", r.TotalWeightGrams, r.TotalAmountIqd, r.TotalAmountUsd,
            r.Status.ToString(), r.Notes
        }).ToList();
        ExportTable("مرتجعات_البيع.xlsx", "تقرير مرتجعات البيع", cols, rows);
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "رقم", "تاريخ", "زبون", "فاتورة أصلية", "د.ع", "وزن" };
        var rows = _allRows.Select(r => new object[]
        {
            r.InvoiceNumber, r.InvoiceDate.ToString("yyyy/MM/dd"), r.CustomerName ?? "—",
            r.RelatedInvoiceNumber ?? "—", r.TotalAmountIqd, r.TotalWeightGrams
        }).ToList();
        PrintTable("تقرير مرتجعات البيع", cols, rows);
    }
}
