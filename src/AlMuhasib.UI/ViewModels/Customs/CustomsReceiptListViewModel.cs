using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Customs;
using AlMuhasib.Core.Models.Customs;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels.Customs;

public partial class CustomsReceiptListViewModel : ViewModelBase
{
    private readonly ICustomsReceiptService _receiptService;
    private readonly ICustomsPrintService _printService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly MainWindowViewModel _mainWindow;
    private System.Timers.Timer? _debounceTimer;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private DateTime? _dateFrom;
    [ObservableProperty] private DateTime? _dateTo;
    [ObservableProperty] private AccountingCurrency? _currencyFilter;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private CustomsReceiptListItem? _selectedReceipt;
    [ObservableProperty] private string? _sortBy;
    [ObservableProperty] private bool _sortDescending = true;

    [ObservableProperty] private bool _isCancelDialogOpen;
    [ObservableProperty] private CustomsReceiptListItem? _receiptToCancel;

    public ObservableCollection<CustomsReceiptListItem> Receipts { get; } = [];

    public IReadOnlyList<CustomsCurrencyFilterOption> CurrencyOptions { get; } =
    [
        new(null, "الكل"),
        new(AccountingCurrency.IQD, "دينار عراقي"),
        new(AccountingCurrency.USD, "دولار أمريكي")
    ];

    public CustomsReceiptListViewModel(
        ICustomsReceiptService receiptService,
        ICustomsPrintService printService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        MainWindowViewModel mainWindow)
    {
        _receiptService = receiptService;
        _printService = printService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _mainWindow = mainWindow;
        PageTitle = "وصولات القبض";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CustomsPermissionRegistry.Receipts);
        await LoadReceiptsAsync();
    }

    private async Task LoadReceiptsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
            var (items, totalCount) = await _receiptService.GetPagedAsync(
                CurrentPage, PageSize, search, DateFrom, DateTo, null, CurrencyFilter,
                sortBy: SortBy, sortDescending: SortDescending);

            TotalCount = totalCount;
            TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
            PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);

            Receipts.Clear();
            foreach (var item in items)
                Receipts.Add(item);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر تحميل الوصولات:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>M-04: server-side sort from DataGrid header (allowlisted columns only).</summary>
    public Task ApplyServerSortAsync(string sortBy, bool sortDescending)
    {
        SortBy = sortBy;
        SortDescending = sortDescending;
        CurrentPage = 1;
        return LoadReceiptsAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(400) { AutoReset = false };
        _debounceTimer.Elapsed += (_, _) =>
        {
            _debounceTimer?.Stop();
            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                CurrentPage = 1;
                await LoadReceiptsAsync();
            });
        };
        _debounceTimer.Start();
    }

    partial void OnCurrencyFilterChanged(AccountingCurrency? value)
    {
        CurrentPage = 1;
        _ = LoadReceiptsAsync();
    }

    partial void OnDateFromChanged(DateTime? value)
    {
        CurrentPage = 1;
        _ = LoadReceiptsAsync();
    }

    partial void OnDateToChanged(DateTime? value)
    {
        CurrentPage = 1;
        _ = LoadReceiptsAsync();
    }

    [RelayCommand]
    private async Task FirstPage() { CurrentPage = 1; await LoadReceiptsAsync(); }

    [RelayCommand]
    private async Task PreviousPage() { if (CurrentPage > 1) { CurrentPage--; await LoadReceiptsAsync(); } }

    [RelayCommand]
    private async Task NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; await LoadReceiptsAsync(); } }

    [RelayCommand]
    private async Task LastPage() { CurrentPage = TotalPages; await LoadReceiptsAsync(); }

    [RelayCommand]
    private async Task Refresh()
    {
        CurrentPage = 1;
        await LoadReceiptsAsync();
    }

    [RelayCommand]
    private async Task OpenNewReceiptAsync()
    {
        if (!CanAdd)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية إنشاء وصل قبض");
            return;
        }

        await _mainWindow.OpenTabAsync(
            typeof(CustomsReceiptFormViewModel),
            "وصل قبض جديد",
            PackIconKind.CashPlus,
            activateIfExists: false,
            permissionScreenName: CustomsPermissionRegistry.Receipts);
    }

    [RelayCommand]
    private async Task EditReceiptAsync(CustomsReceiptListItem? item)
    {
        item ??= SelectedReceipt;
        if (item is null || !item.CanEdit || !CanEdit)
        {
            if (item is not null && !CanEdit)
                BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية تعديل الوصل");
            return;
        }

        CustomsReceiptNavigationBridge.PendingEditReceiptId = item.Id;
        await _mainWindow.OpenTabAsync(
            typeof(CustomsReceiptFormViewModel),
            $"تعديل {item.ReceiptNumber}",
            PackIconKind.Cash,
            activateIfExists: false,
            permissionScreenName: CustomsPermissionRegistry.Receipts);
    }

    [RelayCommand]
    private void ConfirmCancel(CustomsReceiptListItem? item)
    {
        if (item is null || !item.CanCancel || !CanDelete) return;
        ReceiptToCancel = item;
        IsCancelDialogOpen = true;
    }

    [RelayCommand]
    private void CloseCancelDialog()
    {
        IsCancelDialogOpen = false;
        ReceiptToCancel = null;
    }

    [RelayCommand]
    private async Task ExecuteCancelAsync()
    {
        if (ReceiptToCancel is null || !CanDelete) return;
        try
        {
            await _receiptService.CancelAsync(ReceiptToCancel.Id, _currentUserService.Username);
            CloseCancelDialog();
            BeautifulMessageDialog.ShowSuccess("تم إلغاء الوصل وعكس التوزيعات");
            await LoadReceiptsAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task PrintReceiptAsync(CustomsReceiptListItem? item)
    {
        item ??= SelectedReceipt;
        if (item is null || !CanPrint) return;
        try
        {
            var detail = await _receiptService.GetByIdAsync(item.Id);
            if (detail is null) return;
            var path = _printService.ExportReceiptPdf(detail);
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ExportReceiptPdfAsync(CustomsReceiptListItem? item)
    {
        item ??= SelectedReceipt;
        if (item is null || !CanExport) return;
        try
        {
            var detail = await _receiptService.GetByIdAsync(item.Id);
            if (detail is null) return;
            var path = _printService.ExportReceiptPdf(detail);

            var dialog = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"{detail.ReceiptNumber}.pdf",
                DefaultExt = ".pdf"
            };
            if (dialog.ShowDialog() == true)
            {
                File.Copy(path, dialog.FileName, overwrite: true);
                BeautifulMessageDialog.ShowSuccess("تم حفظ ملف PDF");
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            var (allItems, _) = await _receiptService.GetPagedAsync(
                1, int.MaxValue, null, DateFrom, DateTo, null, CurrencyFilter);
            var columns = new[] { "الرقم", "التاريخ", "العميل", "المبلغ", "العملة", "الطريقة", "المستلم", "الفواتير" };
            IList<object[]> rows = allItems.Select(r => new object[]
            {
                r.ReceiptNumber,
                r.ReceiptDate.ToString("yyyy/MM/dd"),
                r.CustomerName,
                r.Amount,
                r.CurrencyLabel,
                r.PaymentMethodLabel,
                r.ReceiverName,
                r.AllocationCount
            }).ToList();
            ListTableExportHelper.ExportExcel(
                _exportService, toast: null, CanExport,
                "وصولات_القبض", "الوصولات", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء التصدير: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PrintTable()
    {
        if (!CanPrint)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية الطباعة");
            return;
        }

        try
        {
            var (allItems, _) = await _receiptService.GetPagedAsync(
                1, int.MaxValue, null, DateFrom, DateTo, null, CurrencyFilter);
            var columns = new[] { "الرقم", "التاريخ", "العميل", "المبلغ", "العملة", "الطريقة", "المستلم" };
            IList<object[]> rows = allItems.Select(r => new object[]
            {
                r.ReceiptNumber,
                r.ReceiptDate.ToString("yyyy/MM/dd"),
                r.CustomerName,
                r.Amount.ToString("N0"),
                r.CurrencyLabel,
                r.PaymentMethodLabel,
                r.ReceiverName
            }).ToList();
            _printService.PrintReportTable(
                CustomsPermissionRegistry.Receipts,
                "وصولات القبض — التخليص الكمركي",
                columns,
                rows,
                kind: CustomsPrintDocumentKind.ReceiptsList,
                fromDate: DateFrom,
                toDate: DateTo);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }
}

public sealed record CustomsCurrencyFilterOption(AccountingCurrency? Value, string Label);
