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

public enum CarTradePaymentDialogKind
{
    None,
    Purchase,
    Sale
}

public partial class CarTradeListViewModel : PagedViewModelBase
{
    private readonly ICarTradeService _tradeService;
    private readonly ICarTradePrintService _printService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserPreferencesService _userPreferences;
    private readonly IToastNotificationService _toast;
    private readonly MainWindowViewModel _mainWindow;
    private System.Timers.Timer? _debounceTimer;

    public ObservableCollection<CarTradeListItem> Transactions { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private DateTime? _dateFrom;
    [ObservableProperty] private DateTime? _dateTo;
    [ObservableProperty] private CarTradeStatusFilter _statusFilter = CarTradeStatusFilter.All;
    [ObservableProperty] private CarTradeSoldFilter _soldFilter = CarTradeSoldFilter.All;
    [ObservableProperty] private bool _unpaidOnly;
    [ObservableProperty] private bool _isCardView;
    [ObservableProperty] private CarTradeListItem? _selectedTransaction;
    [ObservableProperty] private bool _isPaymentDialogOpen;
    [ObservableProperty] private CarTradePaymentDialogKind _paymentDialogKind;
    [ObservableProperty] private decimal _paymentAmount;
    [ObservableProperty] private DateTime _paymentDate = DateTime.Today;
    [ObservableProperty] private string _paymentNotes = string.Empty;
    [ObservableProperty] private string _paymentDialogTitle = string.Empty;
    [ObservableProperty] private string _paymentTransactionSummary = string.Empty;
    [ObservableProperty] private bool _isSellDialogOpen;
    [ObservableProperty] private string _sellBuyerName = string.Empty;
    [ObservableProperty] private string _sellBuyerPhone = string.Empty;
    [ObservableProperty] private decimal _sellPrice;
    [ObservableProperty] private CarTradePaymentMode _sellPaymentMode = CarTradePaymentMode.FullCash;
    [ObservableProperty] private decimal _sellAmountPaid;
    [ObservableProperty] private decimal _sellRemainingAmount;
    [ObservableProperty] private DateTime _sellDate = DateTime.Today;
    [ObservableProperty] private string _sellNotes = string.Empty;
    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private CarTradeListItem? _transactionToDelete;
    [ObservableProperty] private bool _isDetailDialogOpen;
    [ObservableProperty] private CarTradeDetailDisplay? _detailTransaction;
    [ObservableProperty] private int _summaryTotalCount;
    [ObservableProperty] private int _summaryAvailableCount;
    [ObservableProperty] private int _summarySoldCount;
    [ObservableProperty] private decimal _summaryTotalAmount;
    [ObservableProperty] private decimal _summaryTotalPaid;
    [ObservableProperty] private decimal _summaryTotalRemaining;
    [ObservableProperty] private decimal _summarySaleRemaining;

    public bool IsSellCash => SellPaymentMode == CarTradePaymentMode.FullCash;
    public bool IsSellCredit => SellPaymentMode == CarTradePaymentMode.Partial;
    public bool IsSoldFilterAll => SoldFilter == CarTradeSoldFilter.All;
    public bool IsSoldFilterAvailable => SoldFilter == CarTradeSoldFilter.Available;
    public bool IsSoldFilterSold => SoldFilter == CarTradeSoldFilter.Sold;

    private bool _suppressSellAmountRecalc;

    public CarTradeListViewModel(
        ICarTradeService tradeService,
        ICarTradePrintService printService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IUserPreferencesService userPreferences,
        IToastNotificationService toast,
        MainWindowViewModel mainWindow)
    {
        _tradeService = tradeService;
        _printService = printService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _userPreferences = userPreferences;
        _toast = toast;
        _mainWindow = mainWindow;
        PageTitle = "العمليات";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CarTradePermissionRegistry.CarTradeList);
        IsCardView = ListViewModeHelper.LoadIsCardView(_userPreferences, ListViewModeKeys.CarTradeTransactions);
        await LoadTransactionsAsync();
    }

    partial void OnIsCardViewChanged(bool value) =>
        ListViewModeHelper.SaveIsCardView(_userPreferences, ListViewModeKeys.CarTradeTransactions, value);

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
    partial void OnSoldFilterChanged(CarTradeSoldFilter value)
    {
        OnPropertyChanged(nameof(IsSoldFilterAll));
        OnPropertyChanged(nameof(IsSoldFilterAvailable));
        OnPropertyChanged(nameof(IsSoldFilterSold));
        _ = ReloadFromFirstPageAsync();
    }
    partial void OnUnpaidOnlyChanged(bool value) => _ = ReloadFromFirstPageAsync();

    partial void OnSellPriceChanged(decimal value)
    {
        if (_suppressSellAmountRecalc)
            return;

        if (SellPaymentMode == CarTradePaymentMode.FullCash)
            SetSellAmountPaidInternal(SellPrice);
        else if (SellAmountPaid > SellPrice)
            SetSellAmountPaidInternal(SellPrice);

        SellRemainingAmount = Math.Max(0, SellPrice - SellAmountPaid);
    }

    partial void OnSellPaymentModeChanged(CarTradePaymentMode value)
    {
        if (_suppressSellAmountRecalc)
            return;

        if (value == CarTradePaymentMode.FullCash)
            SetSellAmountPaidInternal(SellPrice);

        SellRemainingAmount = Math.Max(0, SellPrice - SellAmountPaid);
        OnPropertyChanged(nameof(IsSellCash));
        OnPropertyChanged(nameof(IsSellCredit));
    }

    partial void OnSellAmountPaidChanged(decimal value)
    {
        if (_suppressSellAmountRecalc)
            return;

        if (SellAmountPaid > SellPrice && SellPrice > 0)
            SetSellAmountPaidInternal(SellPrice);

        SyncSellPaymentModeFromPaidAmount();
        SellRemainingAmount = Math.Max(0, SellPrice - SellAmountPaid);
    }

    private void SetSellAmountPaidInternal(decimal value)
    {
        _suppressSellAmountRecalc = true;
        SellAmountPaid = value;
        _suppressSellAmountRecalc = false;
    }

    private void SyncSellPaymentModeFromPaidAmount()
    {
        var newMode = SellPrice > 0 && SellAmountPaid >= SellPrice
            ? CarTradePaymentMode.FullCash
            : CarTradePaymentMode.Partial;

        if (SellPaymentMode == newMode)
            return;

        _suppressSellAmountRecalc = true;
        SellPaymentMode = newMode;
        _suppressSellAmountRecalc = false;
        OnPropertyChanged(nameof(IsSellCash));
        OnPropertyChanged(nameof(IsSellCredit));
    }

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
        SummaryAvailableCount = rows.Count(r => !r.IsSold);
        SummarySoldCount = rows.Count(r => r.IsSold);
        SummaryTotalAmount = rows.Sum(r => r.PurchasePrice);
        SummaryTotalPaid = rows.Sum(r => r.AmountPaid) + rows.Sum(r => r.SaleAmountPaid);
        SummaryTotalRemaining = rows.Sum(r => r.RemainingAmount);
        SummarySaleRemaining = rows.Sum(r => r.SaleRemainingAmount);
    }

    [RelayCommand]
    private async Task OpenNewTransactionAsync() =>
        await _mainWindow.OpenTabAsync(typeof(CarTradeFormViewModel), "شراء سيارة", PackIconKind.CarArrowRight, activateIfExists: false);

    [RelayCommand]
    private async Task EditTransactionAsync(CarTradeListItem? item)
    {
        item ??= SelectedTransaction;
        if (item is null || !CanEdit || item.IsSold)
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
    private void SetSoldFilterAll() => SoldFilter = CarTradeSoldFilter.All;

    [RelayCommand]
    private void SetSoldFilterAvailable() => SoldFilter = CarTradeSoldFilter.Available;

    [RelayCommand]
    private void SetSoldFilterSold() => SoldFilter = CarTradeSoldFilter.Sold;

    [RelayCommand]
    private void OpenPurchasePaymentDialog(CarTradeListItem? item)
    {
        item ??= SelectedTransaction;
        if (item is null || !CanEdit || !item.CanPaySeller)
            return;

        SelectedTransaction = item;
        PaymentDialogKind = CarTradePaymentDialogKind.Purchase;
        PaymentDialogTitle = "تسديد للبائع";
        PaymentAmount = item.RemainingAmount;
        PaymentDate = DateTime.Today;
        PaymentNotes = string.Empty;
        PaymentTransactionSummary = $"عملية {item.TransactionNumber} — {item.CarName} — متبقي للبائع: {item.RemainingAmount:N0}";
        IsPaymentDialogOpen = true;
    }

    [RelayCommand]
    private void OpenSalePaymentDialog(CarTradeListItem? item)
    {
        item ??= SelectedTransaction;
        if (item is null || !CanEdit || !item.CanPayBuyer)
            return;

        SelectedTransaction = item;
        PaymentDialogKind = CarTradePaymentDialogKind.Sale;
        PaymentDialogTitle = "تسديد من المشتري";
        PaymentAmount = item.SaleRemainingAmount;
        PaymentDate = DateTime.Today;
        PaymentNotes = string.Empty;
        PaymentTransactionSummary = $"عملية {item.TransactionNumber} — {item.CarName} — متبقي على المشتري: {item.SaleRemainingAmount:N0}";
        IsPaymentDialogOpen = true;
    }

    [RelayCommand]
    private void ClosePaymentDialog()
    {
        IsPaymentDialogOpen = false;
        PaymentDialogKind = CarTradePaymentDialogKind.None;
    }

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
            var paymentKind = PaymentDialogKind;
            if (paymentKind == CarTradePaymentDialogKind.Sale)
            {
                await _tradeService.RecordSalePaymentAsync(
                    SelectedTransaction.Id, PaymentAmount, PaymentDate, PaymentNotes);
            }
            else
            {
                await _tradeService.RecordPurchasePaymentAsync(
                    SelectedTransaction.Id, PaymentAmount, PaymentDate, PaymentNotes);
            }

            IsPaymentDialogOpen = false;
            PaymentDialogKind = CarTradePaymentDialogKind.None;
            _toast.ShowSuccess("تم تسجيل التسديد بنجاح");

            if (CanPrint)
            {
                var updated = await _tradeService.GetByIdAsync(SelectedTransaction.Id);
                var kind = paymentKind == CarTradePaymentDialogKind.Sale
                    ? CarTradePaymentKind.Sale
                    : CarTradePaymentKind.Purchase;
                var payment = updated?.Payments
                    .Where(p => p.PaymentKind == kind)
                    .OrderByDescending(p => p.PaymentDate)
                    .ThenByDescending(p => p.Id)
                    .FirstOrDefault();
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
    private void OpenSellDialog(CarTradeListItem? item)
    {
        item ??= SelectedTransaction;
        if (item is null || !CanEdit || !item.CanSell)
            return;

        SelectedTransaction = item;
        SellBuyerName = string.Empty;
        SellBuyerPhone = string.Empty;
        SellPrice = 0;
        SellPaymentMode = CarTradePaymentMode.FullCash;
        SellAmountPaid = 0;
        SellRemainingAmount = 0;
        SellDate = DateTime.Today;
        SellNotes = string.Empty;
        IsSellDialogOpen = true;
    }

    [RelayCommand]
    private void CloseSellDialog() => IsSellDialogOpen = false;

    [RelayCommand]
    private void SetSellCash()
    {
        SellPaymentMode = CarTradePaymentMode.FullCash;
        SetSellAmountPaidInternal(SellPrice);
        SellRemainingAmount = 0;
        OnPropertyChanged(nameof(IsSellCash));
        OnPropertyChanged(nameof(IsSellCredit));
    }

    [RelayCommand]
    private void SetSellCredit()
    {
        SellPaymentMode = CarTradePaymentMode.Partial;
        SellRemainingAmount = Math.Max(0, SellPrice - SellAmountPaid);
        OnPropertyChanged(nameof(IsSellCash));
        OnPropertyChanged(nameof(IsSellCredit));
    }

    [RelayCommand]
    private async Task SubmitSellAsync()
    {
        if (SelectedTransaction is null)
            return;

        if (string.IsNullOrWhiteSpace(SellBuyerName))
        {
            _toast.ShowWarning("اسم المشتري مطلوب");
            return;
        }

        if (SellPrice <= 0)
        {
            _toast.ShowWarning("سعر البيع يجب أن يكون أكبر من صفر");
            return;
        }

        if (SellAmountPaid > SellPrice)
        {
            _toast.ShowWarning("المبلغ المدفوع أكبر من سعر البيع");
            return;
        }

        try
        {
            await _tradeService.SellCarAsync(SelectedTransaction.Id, new CarTradeSellRequest
            {
                BuyerName = SellBuyerName,
                BuyerPhone = SellBuyerPhone,
                SalePrice = SellPrice,
                SalePaymentMode = SellPaymentMode,
                SaleAmountPaid = SellAmountPaid,
                SaleDate = SellDate,
                Notes = SellNotes
            });

            IsSellDialogOpen = false;
            _toast.ShowSuccess("تم تسجيل بيع السيارة بنجاح");

            if (CanPrint)
            {
                var updated = await _tradeService.GetByIdAsync(SelectedTransaction.Id);
                if (updated is not null)
                    _printService.PrintSale(updated);
            }

            await LoadTransactionsAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task PrintPurchaseAsync(CarTradeListItem? item)
    {
        item ??= SelectedTransaction;
        if (item is null || !CanPrint)
            return;

        var transaction = await _tradeService.GetByIdAsync(item.Id);
        if (transaction is not null)
            _printService.PrintPurchase(transaction);
    }

    [RelayCommand]
    private async Task PrintSaleAsync(CarTradeListItem? item)
    {
        item ??= SelectedTransaction;
        if (item is null || !CanPrint)
            return;

        if (!item.IsSold)
        {
            _toast.ShowWarning("السيارة غير مباعة بعد — لا يوجد وصل بيع");
            return;
        }

        var transaction = await _tradeService.GetByIdAsync(item.Id);
        if (transaction is not null)
            _printService.PrintSale(transaction);
    }

    [RelayCommand]
    private async Task PrintTransactionAsync(CarTradeListItem? item)
    {
        item ??= SelectedTransaction;
        if (item is null || !CanPrint)
            return;

        if (item.IsSold)
            await PrintSaleAsync(item);
        else
            await PrintPurchaseAsync(item);
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
            if (transaction is null)
                continue;

            if (transaction.IsSold)
                _printService.PrintSale(transaction, 1);
            else
                _printService.PrintPurchase(transaction, 1);
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

        var headers = new[] { "رقم العملية", "التاريخ", "السيارة", "البائع", "المشتري", "حالة البيع", "سعر الشراء", "متبقي بائع", "سعر البيع", "متبقي مشتري", "الحالة" };
        var data = rows.Select(r => new object?[]
        {
            r.TransactionNumber, r.TransactionDate.ToString("yyyy/MM/dd"), r.CarName,
            r.SellerName, r.BuyerName, r.SoldStatus, r.PurchasePrice, r.RemainingAmount,
            r.IsSold ? r.SalePrice : null, r.SaleRemainingAmount, r.Status
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
        UnpaidOnly = UnpaidOnly,
        SoldFilter = SoldFilter
    };
}
