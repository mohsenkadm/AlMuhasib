using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using AlMuhasib.Core.Enums;
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
        IExportService exportService,
        ICurrentUserService currentUserService,
        MainWindowViewModel mainWindow)
    {
        _receiptService = receiptService;
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
                CurrentPage, PageSize, search, DateFrom, DateTo, null, CurrencyFilter);

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
    private async Task OpenNewReceiptAsync() =>
        await _mainWindow.OpenTabAsync(
            typeof(CustomsReceiptFormViewModel),
            "وصل قبض جديد",
            PackIconKind.CashPlus,
            activateIfExists: false,
            permissionScreenName: CustomsPermissionRegistry.Receipts);

    [RelayCommand]
    private async Task EditReceiptAsync(CustomsReceiptListItem? item)
    {
        item ??= SelectedReceipt;
        if (item is null || !item.CanEdit) return;

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
        if (ReceiptToCancel is null) return;
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
            var model = CustomsReceiptPrintHelper.ToVoucherPrintModel(detail);
            var path = _exportService.ExportVoucherToPdf(model);
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
        if (item is null || !CanPrint) return;
        try
        {
            var detail = await _receiptService.GetByIdAsync(item.Id);
            if (detail is null) return;
            var model = CustomsReceiptPrintHelper.ToVoucherPrintModel(detail);
            var path = _exportService.ExportVoucherToPdf(model);

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
            var exportData = allItems.Select(r => new
            {
                الرقم = r.ReceiptNumber,
                التاريخ = r.ReceiptDate.ToString("yyyy/MM/dd"),
                العميل = r.CustomerName,
                المبلغ = r.Amount,
                العملة = r.CurrencyLabel,
                الطريقة = r.PaymentMethodLabel,
                المستلم = r.ReceiverName,
                الفواتير = r.AllocationCount
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"وصولات_القبض_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "الوصولات");
                BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء التصدير: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PrintTable()
    {
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
            _exportService.PrintTable("وصولات القبض — التخليص الكمركي", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }
}

public sealed record CustomsCurrencyFilterOption(AccountingCurrency? Value, string Label);

public static class CustomsReceiptPrintHelper
{
    public static VoucherPrintModel ToVoucherPrintModel(CustomsReceiptDetail detail)
    {
        var allocNotes = detail.Allocations.Count == 0
            ? detail.Notes
            : string.Join(" | ", detail.Allocations.Select(a => $"{a.InvoiceNumber}:{a.Amount:N0}"))
              + (string.IsNullOrWhiteSpace(detail.Notes) ? string.Empty : $" — {detail.Notes}");

        return new VoucherPrintModel
        {
            Title = "وصل قبض — التخليص الكمركي",
            VoucherNumber = detail.ReceiptNumber,
            VoucherTypeLabel = detail.PaymentMethodLabel,
            Date = detail.ReceiptDate,
            Amount = detail.Amount,
            CurrencyLabel = detail.CurrencyLabel,
            PartyLabel = "العميل",
            PartyName = detail.CustomerName,
            PartyPhone = detail.CustomerPhone,
            Notes = allocNotes
        };
    }
}
