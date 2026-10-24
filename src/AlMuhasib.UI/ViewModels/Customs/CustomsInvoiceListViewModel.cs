using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
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

public partial class CustomsInvoiceListViewModel : ViewModelBase
{
    private readonly ICustomsInvoiceService _invoiceService;
    private readonly ICustomsPrintService _printService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly MainWindowViewModel _mainWindow;
    private System.Timers.Timer? _debounceTimer;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private DateTime? _dateFrom;
    [ObservableProperty] private DateTime? _dateTo;
    [ObservableProperty] private CustomsInvoiceStatus? _statusFilter;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private CustomsInvoiceListItem? _selectedInvoice;
    [ObservableProperty] private string? _sortBy;
    [ObservableProperty] private bool _sortDescending = true;

    [ObservableProperty] private bool _isCancelDialogOpen;
    [ObservableProperty] private CustomsInvoiceListItem? _invoiceToCancel;
    [ObservableProperty] private string _cancelReason = string.Empty;

    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private CustomsInvoiceListItem? _invoiceToDelete;

    public ObservableCollection<CustomsInvoiceListItem> Invoices { get; } = [];

    public IReadOnlyList<CustomsInvoiceStatusOption> StatusOptions { get; } =
    [
        new(null, "الكل"),
        new(CustomsInvoiceStatus.Draft, CustomsInvoiceStatusDisplay.ToArabic(CustomsInvoiceStatus.Draft)),
        new(CustomsInvoiceStatus.Approved, CustomsInvoiceStatusDisplay.ToArabic(CustomsInvoiceStatus.Approved)),
        new(CustomsInvoiceStatus.Cancelled, CustomsInvoiceStatusDisplay.ToArabic(CustomsInvoiceStatus.Cancelled))
    ];

    public CustomsInvoiceListViewModel(
        ICustomsInvoiceService invoiceService,
        ICustomsPrintService printService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        MainWindowViewModel mainWindow)
    {
        _invoiceService = invoiceService;
        _printService = printService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _mainWindow = mainWindow;
        PageTitle = "الفواتير";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CustomsPermissionRegistry.Invoices);
        await LoadInvoicesAsync();
    }

    private async Task LoadInvoicesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
            var (items, totalCount) = await _invoiceService.GetPagedAsync(
                CurrentPage, PageSize, search, StatusFilter, DateFrom, DateTo,
                customerId: null, sortBy: SortBy, sortDescending: SortDescending);

            TotalCount = totalCount;
            TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
            PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);

            Invoices.Clear();
            foreach (var item in items)
                Invoices.Add(item);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر تحميل الفواتير:\n{ex.Message}");
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
                await LoadInvoicesAsync();
            });
        };
        _debounceTimer.Start();
    }

    partial void OnStatusFilterChanged(CustomsInvoiceStatus? value)
    {
        CurrentPage = 1;
        _ = LoadInvoicesAsync();
    }

    partial void OnDateFromChanged(DateTime? value)
    {
        CurrentPage = 1;
        _ = LoadInvoicesAsync();
    }

    partial void OnDateToChanged(DateTime? value)
    {
        CurrentPage = 1;
        _ = LoadInvoicesAsync();
    }

    /// <summary>M-04: server-side sort from DataGrid header (allowlisted columns only).</summary>
    public Task ApplyServerSortAsync(string sortBy, bool sortDescending)
    {
        SortBy = sortBy;
        SortDescending = sortDescending;
        CurrentPage = 1;
        return LoadInvoicesAsync();
    }

    [RelayCommand]
    private async Task FirstPage() { CurrentPage = 1; await LoadInvoicesAsync(); }

    [RelayCommand]
    private async Task PreviousPage() { if (CurrentPage > 1) { CurrentPage--; await LoadInvoicesAsync(); } }

    [RelayCommand]
    private async Task NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; await LoadInvoicesAsync(); } }

    [RelayCommand]
    private async Task LastPage() { CurrentPage = TotalPages; await LoadInvoicesAsync(); }

    [RelayCommand]
    private async Task Refresh()
    {
        CurrentPage = 1;
        await LoadInvoicesAsync();
    }

    [RelayCommand]
    private async Task OpenNewInvoiceAsync()
    {
        if (!CanAdd)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية إنشاء فاتورة");
            return;
        }

        await _mainWindow.OpenTabAsync(
            typeof(CustomsInvoiceFormViewModel),
            "فاتورة جديدة",
            PackIconKind.FileDocumentPlus,
            activateIfExists: false,
            permissionScreenName: CustomsPermissionRegistry.Invoices);
    }

    [RelayCommand]
    private async Task ViewDetailsAsync(CustomsInvoiceListItem? item)
    {
        item ??= SelectedInvoice;
        if (item is null)
            return;

        CustomsInvoiceNavigationBridge.PendingEditInvoiceId = item.Id;
        await _mainWindow.OpenTabAsync(
            typeof(CustomsInvoiceFormViewModel),
            item.CanEdit ? $"فاتورة {item.InvoiceNumber}" : $"عرض {item.InvoiceNumber}",
            PackIconKind.FileDocumentOutline,
            activateIfExists: false,
            permissionScreenName: CustomsPermissionRegistry.Invoices);
    }

    [RelayCommand]
    private async Task EditInvoiceAsync(CustomsInvoiceListItem? item)
    {
        item ??= SelectedInvoice;
        if (item is null || !item.CanEdit || !CanEdit)
        {
            if (item is not null && !CanEdit)
                BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية تعديل الفاتورة");
            return;
        }

        CustomsInvoiceNavigationBridge.PendingEditInvoiceId = item.Id;
        await _mainWindow.OpenTabAsync(
            typeof(CustomsInvoiceFormViewModel),
            $"تعديل {item.InvoiceNumber}",
            PackIconKind.FileDocumentEdit,
            activateIfExists: false,
            permissionScreenName: CustomsPermissionRegistry.Invoices);
    }

    [RelayCommand]
    private async Task ApproveInvoiceAsync(CustomsInvoiceListItem? item)
    {
        item ??= SelectedInvoice;
        if (item is null || !item.CanApprove || !CanEdit)
        {
            if (item is not null && !item.CanApprove)
                BeautifulMessageDialog.ShowWarning("يمكن اعتماد المسودات فقط");
            else if (item is not null)
                BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية اعتماد الفاتورة");
            return;
        }

        if (!BeautifulMessageDialog.ShowConfirm(
                $"اعتماد الفاتورة {item.InvoiceNumber}؟",
                "تأكيد الاعتماد"))
            return;

        try
        {
            await _invoiceService.ApproveAsync(item.Id, _currentUserService.Username);
            BeautifulMessageDialog.ShowSuccess("تم اعتماد الفاتورة");
            await LoadInvoicesAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void ConfirmCancel(CustomsInvoiceListItem? item)
    {
        if (item is null || !item.CanCancel || !CanEdit) return;
        InvoiceToCancel = item;
        CancelReason = string.Empty;
        IsCancelDialogOpen = true;
    }

    [RelayCommand]
    private async Task PrintInvoiceAsync(CustomsInvoiceListItem? item)
    {
        item ??= SelectedInvoice;
        if (item is null) return;
        if (!CanPrint)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية الطباعة");
            return;
        }

        try
        {
            var detail = await _invoiceService.GetByIdAsync(item.Id);
            if (detail is null)
            {
                BeautifulMessageDialog.ShowError("الفاتورة غير موجودة");
                return;
            }

            _printService.PrintCustomerInvoice(detail, null);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task PreviewInvoiceAsync(CustomsInvoiceListItem? item)
    {
        item ??= SelectedInvoice;
        if (item is null) return;
        if (!CanPrint && !CanExport)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية المعاينة أو التصدير");
            return;
        }

        try
        {
            var detail = await _invoiceService.GetByIdAsync(item.Id);
            if (detail is null)
            {
                BeautifulMessageDialog.ShowError("الفاتورة غير موجودة");
                return;
            }

            var temp = _printService.ExportCustomerInvoicePdf(detail, null);
            Process.Start(new ProcessStartInfo { FileName = temp, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ExportInvoicePdfAsync(CustomsInvoiceListItem? item)
    {
        item ??= SelectedInvoice;
        if (item is null) return;
        if (!CanExport)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية التصدير");
            return;
        }

        try
        {
            var detail = await _invoiceService.GetByIdAsync(item.Id);
            if (detail is null)
            {
                BeautifulMessageDialog.ShowError("الفاتورة غير موجودة");
                return;
            }

            var temp = _printService.ExportCustomerInvoicePdf(detail, null);
            var dlg = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"{detail.InvoiceNumber}.pdf",
                DefaultExt = ".pdf"
            };
            if (dlg.ShowDialog() == true)
            {
                File.Copy(temp, dlg.FileName, overwrite: true);
                BeautifulMessageDialog.ShowSuccess("تم حفظ ملف PDF");
            }
            else
            {
                Process.Start(new ProcessStartInfo { FileName = temp, UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ViewInvoiceExpensesAsync(CustomsInvoiceListItem? item)
    {
        item ??= SelectedInvoice;
        if (item is null) return;
        CustomsInvoiceNavigationBridge.PendingEditInvoiceId = item.Id;
        CustomsInvoiceNavigationBridge.PendingFocusSection = "expenses";
        await _mainWindow.OpenTabAsync(
            typeof(CustomsInvoiceFormViewModel),
            $"مصاريف {item.InvoiceNumber}",
            PackIconKind.CashMinus,
            activateIfExists: false,
            permissionScreenName: CustomsPermissionRegistry.Invoices);
    }

    [RelayCommand]
    private async Task ViewInvoiceAttachmentsAsync(CustomsInvoiceListItem? item)
    {
        item ??= SelectedInvoice;
        if (item is null) return;
        CustomsInvoiceNavigationBridge.PendingEditInvoiceId = item.Id;
        CustomsInvoiceNavigationBridge.PendingFocusSection = "attachments";
        await _mainWindow.OpenTabAsync(
            typeof(CustomsInvoiceFormViewModel),
            $"مرفقات {item.InvoiceNumber}",
            PackIconKind.Paperclip,
            activateIfExists: false,
            permissionScreenName: CustomsPermissionRegistry.Invoices);
    }

    [RelayCommand]
    private async Task ViewInvoiceVehiclesAsync(CustomsInvoiceListItem? item)
    {
        item ??= SelectedInvoice;
        if (item is null) return;
        CustomsInvoiceNavigationBridge.PendingEditInvoiceId = item.Id;
        CustomsInvoiceNavigationBridge.PendingFocusSection = "vehicles";
        await _mainWindow.OpenTabAsync(
            typeof(CustomsInvoiceFormViewModel),
            $"مركبات {item.InvoiceNumber}",
            PackIconKind.Car,
            activateIfExists: false,
            permissionScreenName: CustomsPermissionRegistry.Invoices);
    }

    [RelayCommand]
    private void CloseCancelDialog()
    {
        IsCancelDialogOpen = false;
        InvoiceToCancel = null;
        CancelReason = string.Empty;
    }

    [RelayCommand]
    private async Task ExecuteCancelAsync()
    {
        if (InvoiceToCancel is null || !CanEdit) return;
        try
        {
            await _invoiceService.CancelAsync(
                InvoiceToCancel.Id,
                _currentUserService.Username,
                string.IsNullOrWhiteSpace(CancelReason) ? null : CancelReason.Trim());
            CloseCancelDialog();
            BeautifulMessageDialog.ShowSuccess("تم إلغاء الفاتورة");
            await LoadInvoicesAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void ConfirmDelete(CustomsInvoiceListItem? item)
    {
        if (item is null || item.Status != CustomsInvoiceStatus.Draft || !CanDelete) return;
        InvoiceToDelete = item;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteDialogOpen = false;
        InvoiceToDelete = null;
    }

    [RelayCommand]
    private async Task ExecuteDeleteAsync()
    {
        if (InvoiceToDelete is null || !CanDelete) return;
        try
        {
            await _invoiceService.SoftDeleteAsync(InvoiceToDelete.Id, _currentUserService.Username);
            CancelDelete();
            BeautifulMessageDialog.ShowSuccess("تم حذف المسودة");
            await LoadInvoicesAsync();
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
            var (allItems, _) = await _invoiceService.GetPagedAsync(1, int.MaxValue, null, StatusFilter, DateFrom, DateTo);
            var columns = new[] { "الرقم", "التاريخ", "العميل", "الخدمة", "العملة", "الإجمالي", "المدفوع", "المتبقي", "الحالة" };
            IList<object[]> rows = allItems.Select(i => new object[]
            {
                i.InvoiceNumber,
                i.InvoiceDate.ToString("yyyy/MM/dd"),
                i.CustomerName,
                i.ServiceTypeLabel,
                i.CurrencyLabel,
                i.TotalAmount,
                i.PaidAmount,
                i.RemainingAmount,
                i.StatusLabel
            }).ToList();
            ListTableExportHelper.ExportExcel(
                _exportService, toast: null, CanExport,
                "فواتير_التخليص", "الفواتير", columns, rows);
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
            var (allItems, _) = await _invoiceService.GetPagedAsync(1, int.MaxValue, null, StatusFilter, DateFrom, DateTo);
            var columns = new[] { "الرقم", "التاريخ", "العميل", "الخدمة", "العملة", "الإجمالي", "المتبقي", "الحالة" };
            IList<object[]> rows = allItems.Select(i => new object[]
            {
                i.InvoiceNumber,
                i.InvoiceDate.ToString("yyyy/MM/dd"),
                i.CustomerName,
                i.ServiceTypeLabel,
                i.CurrencyLabel,
                i.TotalAmount.ToString("N0"),
                i.RemainingAmount.ToString("N0"),
                i.StatusLabel
            }).ToList();
            _printService.PrintReportTable(
                CustomsPermissionRegistry.Invoices,
                "فواتير التخليص الكمركي",
                columns,
                rows,
                kind: CustomsPrintDocumentKind.InvoicesList,
                fromDate: DateFrom,
                toDate: DateTo);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }
}

public sealed record CustomsInvoiceStatusOption(CustomsInvoiceStatus? Value, string Label);
