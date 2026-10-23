using System.Collections.ObjectModel;
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
        IExportService exportService,
        ICurrentUserService currentUserService,
        MainWindowViewModel mainWindow)
    {
        _invoiceService = invoiceService;
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
                CurrentPage, PageSize, search, StatusFilter, DateFrom, DateTo);

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
    private void ConfirmCancel(CustomsInvoiceListItem? item)
    {
        if (item is null || !item.CanCancel || !CanEdit) return;
        InvoiceToCancel = item;
        CancelReason = string.Empty;
        IsCancelDialogOpen = true;
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
            ListTableExportHelper.Print(
                _exportService, CanPrint,
                "فواتير التخليص الكمركي", columns, rows,
                warnOnDenied: true);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }
}

public sealed record CustomsInvoiceStatusOption(CustomsInvoiceStatus? Value, string Label);
