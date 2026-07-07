using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.CarTrade;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.CarTrade;

public partial class CarTradeListViewModel : PagedViewModelBase
{
    private readonly ICarTradeService _tradeService;
    private readonly ICarTradePrintService _printService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;
    private readonly MainWindowViewModel _mainWindow;
    private System.Timers.Timer? _debounceTimer;

    public ObservableCollection<CarTradeListItem> Transactions { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private DateTime? _dateFrom;
    [ObservableProperty] private DateTime? _dateTo;
    [ObservableProperty] private CarTradeStatusFilter _statusFilter = CarTradeStatusFilter.All;
    [ObservableProperty] private CarTradeType? _tradeTypeFilter;
    [ObservableProperty] private bool _unpaidOnly;
    [ObservableProperty] private CarTradeListItem? _selectedTransaction;
    [ObservableProperty] private bool _isPaymentDialogOpen;
    [ObservableProperty] private decimal _paymentAmount;
    [ObservableProperty] private DateTime _paymentDate = DateTime.Today;
    [ObservableProperty] private string _paymentNotes = string.Empty;
    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private CarTradeListItem? _transactionToDelete;
    [ObservableProperty] private bool _isDetailDialogOpen;
    [ObservableProperty] private CarTradeDetailDisplay? _detailTransaction;
    [ObservableProperty] private string _paymentTransactionSummary = string.Empty;
    [ObservableProperty] private int _summaryTotalCount;
    [ObservableProperty] private int _summaryBuyCount;
    [ObservableProperty] private int _summarySellCount;
    [ObservableProperty] private decimal _summaryTotalAmount;
    [ObservableProperty] private decimal _summaryTotalPaid;
    [ObservableProperty] private decimal _summaryTotalRemaining;

    public CarTradeListViewModel(
        ICarTradeService tradeService,
        ICarTradePrintService printService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast,
        MainWindowViewModel mainWindow)
    {
        _tradeService = tradeService;
        _printService = printService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _toast = toast;
        _mainWindow = mainWindow;
        PageTitle = "العمليات";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CarTradePermissionRegistry.CarTradeList);
        await LoadTransactionsAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(350) { AutoReset = false };
        _debounceTimer.Elapsed += async (_, _) =>
        {
            await App.Current.Dispatcher.InvokeAsync(async () =>
            {
                CurrentPage = 1;
                await LoadTransactionsAsync();
            });
        };
        _debounceTimer.Start();
    }

    partial void OnDateFromChanged(DateTime? value) => _ = ReloadFromFirstPageAsync();
    partial void OnDateToChanged(DateTime? value) => _ = ReloadFromFirstPageAsync();
    partial void OnStatusFilterChanged(CarTradeStatusFilter value) => _ = ReloadFromFirstPageAsync();
    partial void OnTradeTypeFilterChanged(CarTradeType? value) => _ = ReloadFromFirstPageAsync();
    partial void OnUnpaidOnlyChanged(bool value) => _ = ReloadFromFirstPageAsync();

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        _ = LoadTransactionsAsync();
    }

    protected override Task OnPageChangedAsync() => LoadTransactionsAsync();

    private async Task ReloadFromFirstPageAsync()
    {
        CurrentPage = 1;
        await LoadTransactionsAsync();
    }

    [RelayCommand]
    private async Task LoadTransactionsAsync()
    {
        IsBusy = true;
        try
        {
            var filter = BuildFilter();

            if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            {
                var (allItems, _) = await _tradeService.GetPagedAsync(1, int.MaxValue, filter);
                var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
                UpdateSummaryStats(filtered);
                MasterDataColumnFilterHelper.ApplyClientPagination(
                    filtered, Transactions, CurrentPage, PageSize,
                    out var filteredTotal, out var filteredPages, out var filteredText);
                TotalCount = filteredTotal;
                TotalPages = filteredPages;
                PaginationText = filteredText;
                return;
            }

            var summaryRows = await _tradeService.GetAllForExportAsync(filter);
            UpdateSummaryStats(summaryRows);

            var (items, total) = await _tradeService.GetPagedAsync(CurrentPage, PageSize, filter);
            Transactions.Clear();
            foreach (var item in items)
                Transactions.Add(item);
            ApplyPaginationStats(total);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateSummaryStats(IReadOnlyList<CarTradeListItem> rows)
    {
        SummaryTotalCount = rows.Count;
        SummaryBuyCount = rows.Count(r => r.TradeTypeValue == CarTradeType.Buy);
        SummarySellCount = rows.Count(r => r.TradeTypeValue == CarTradeType.Sell);
        SummaryTotalAmount = rows.Sum(r => r.TotalAmount);
        SummaryTotalPaid = rows.Sum(r => r.AmountPaid);
        SummaryTotalRemaining = rows.Sum(r => r.RemainingAmount);
    }

    [RelayCommand]
    private async Task OpenNewTransactionAsync() =>
        await _mainWindow.OpenTabAsync(typeof(CarTradeFormViewModel), "عملية جديدة", PackIconKind.SwapHorizontal, activateIfExists: false);

    [RelayCommand]
    private async Task EditTransactionAsync(CarTradeListItem? item)
    {
        item ??= SelectedTransaction;
        if (item is null || !CanEdit)
            return;

        CarTradeNavigationBridge.PendingEditTransactionId = item.Id;
        await _mainWindow.OpenTabAsync(typeof(CarTradeFormViewModel), $"تعديل {item.TransactionNumber}", PackIconKind.FileDocumentEdit, activateIfExists: false);
    }

    [RelayCommand]
    private async Task ViewTransactionDetailsAsync(CarTradeListItem? item)
    {
        item ??= SelectedTransaction;
        if (item is null)
            return;

        var transaction = await _tradeService.GetByIdAsync(item.Id);
        if (transaction is null)
        {
            _toast.ShowError("العملية غير موجودة");
            return;
        }

        SelectedTransaction = item;
        DetailTransaction = CarTradeDetailDisplay.FromEntity(transaction);
        IsDetailDialogOpen = true;
    }

    [RelayCommand]
    private void CloseDetailDialog()
    {
        IsDetailDialogOpen = false;
        DetailTransaction = null;
    }

    [RelayCommand]
    private async Task DetailPrintAsync()
    {
        if (DetailTransaction is null)
            return;

        await PrintTransactionAsync(Transactions.FirstOrDefault(t => t.Id == DetailTransaction.Id) ?? SelectedTransaction);
    }

    [RelayCommand]
    private async Task DetailEditAsync()
    {
        if (SelectedTransaction is null)
            return;

        IsDetailDialogOpen = false;
        await EditTransactionAsync(SelectedTransaction);
    }

    [RelayCommand]
    private void DetailDeleteAsync()
    {
        if (SelectedTransaction is null)
            return;

        IsDetailDialogOpen = false;
        ConfirmDelete(SelectedTransaction);
    }

    [RelayCommand]
    private void ConfirmDelete(CarTradeListItem? item)
    {
        if (item is null || !CanDelete)
            return;

        TransactionToDelete = item;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private void CancelDelete() => IsDeleteDialogOpen = false;

    [RelayCommand]
    private async Task DeleteConfirmedAsync()
    {
        if (TransactionToDelete is null)
            return;

        try
        {
            await _tradeService.DeleteAsync(TransactionToDelete.Id, _currentUserService.Username ?? "System");
            IsDeleteDialogOpen = false;
            TransactionToDelete = null;
            _toast.ShowSuccess("تم حذف العملية");
            await LoadTransactionsAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void OpenPaymentDialog(CarTradeListItem? item)
    {
        item ??= SelectedTransaction;
        if (item is null || !CanEdit || item.RemainingAmount <= 0)
            return;

        SelectedTransaction = item;
        PaymentAmount = item.RemainingAmount;
        PaymentDate = DateTime.Today;
        PaymentNotes = string.Empty;
        PaymentTransactionSummary = $"عملية {item.TransactionNumber} — {item.CarName} (متبقي: {item.RemainingAmount:N0})";
        IsPaymentDialogOpen = true;
    }

    [RelayCommand]
    private void ClosePaymentDialog() => IsPaymentDialogOpen = false;

    [RelayCommand]
    private async Task SubmitPaymentAsync()
    {
        if (SelectedTransaction is null)
            return;

        if (PaymentAmount <= 0)
        {
            _toast.ShowWarning("أدخل مبلغاً صحيحاً");
            return;
        }

        try
        {
            await _tradeService.RecordPaymentAsync(
                SelectedTransaction.Id, PaymentAmount, PaymentDate, PaymentNotes);
            IsPaymentDialogOpen = false;
            _toast.ShowSuccess("تم تسجيل التسديد بنجاح");

            if (CanPrint)
            {
                var updated = await _tradeService.GetByIdAsync(SelectedTransaction.Id);
                var payment = updated?.Payments.OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Id).FirstOrDefault();
                if (updated is not null && payment is not null)
                    _printService.PrintPaymentReceipt(updated, payment);
            }

            await LoadTransactionsAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task PrintTransactionAsync(CarTradeListItem? item)
    {
        item ??= SelectedTransaction;
        if (item is null || !CanPrint)
            return;

        var transaction = await _tradeService.GetByIdAsync(item.Id);
        if (transaction is not null)
            _printService.PrintTransaction(transaction);
    }

    [RelayCommand]
    private async Task PrintAllAsync()
    {
        if (!CanPrint)
            return;

        var rows = await _tradeService.GetAllForExportAsync(BuildFilter());
        foreach (var row in rows)
        {
            var transaction = await _tradeService.GetByIdAsync(row.Id);
            if (transaction is not null)
                _printService.PrintTransaction(transaction, 1);
        }
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        if (!CanExport)
            return;

        var rows = await _tradeService.GetAllForExportAsync(BuildFilter());
        var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"CarTrade_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };
        if (dialog.ShowDialog() != true)
            return;

        var headers = new[] { "رقم العملية", "التاريخ", "النوع", "السيارة", "البائع", "المشتري", "الإجمالي", "المدفوع", "المتبقي", "الحالة" };
        var data = rows.Select(r => new object?[]
        {
            r.TransactionNumber, r.TransactionDate.ToString("yyyy/MM/dd"), r.TradeType, r.CarName,
            r.SellerName, r.BuyerName, r.TotalAmount, r.AmountPaid, r.RemainingAmount, r.Status
        }).ToList();

        _exportService.ExportToExcel(dialog.FileName, "العمليات", headers, data);
        _toast.ShowSuccess("تم تصدير الملف بنجاح");
    }

    private CarTradeFilter BuildFilter() => new()
    {
        SearchText = SearchText,
        DateFrom = DateFrom,
        DateTo = DateTo,
        StatusFilter = StatusFilter,
        TradeType = TradeTypeFilter,
        UnpaidOnly = UnpaidOnly
    };
}
