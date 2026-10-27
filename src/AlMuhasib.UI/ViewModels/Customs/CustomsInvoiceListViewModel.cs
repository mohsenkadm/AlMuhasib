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

public partial class CustomsInvoiceListViewModel : ViewModelBase
{
    private readonly ICustomsInvoiceService _invoiceService;
    private readonly ICustomsPrintService _printService;
    private readonly IExportService _exportService;
    private readonly ICustomsExpenseTypeService _expenseTypeService;
    private readonly ICustomsAttachmentService _attachmentService;
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

    [ObservableProperty] private bool _isDetailDialogOpen;
    [ObservableProperty] private CustomsInvoiceDetail? _detailInvoice;
    [ObservableProperty] private bool _detailIsClearanceGrid;
    [ObservableProperty] private bool _detailIsTransportGrid;
    [ObservableProperty] private bool _detailIsInternalTransportGrid;
    [ObservableProperty] private bool _detailIsExternalTransportGrid;
    [ObservableProperty] private bool _detailShowTransportRoute;
    [ObservableProperty] private bool _detailShowDelayPenalties;
    [ObservableProperty] private bool _detailShowFxRate;

    [ObservableProperty] private bool _isExpenseDialogOpen;
    [ObservableProperty] private CustomsInvoiceListItem? _expenseTargetInvoice;
    [ObservableProperty] private string _expenseDialogSubtitle = string.Empty;
    [ObservableProperty] private string _newExpenseTypeName = string.Empty;
    [ObservableProperty] private CustomsExpenseTypeItem? _selectedExpenseType;
    [ObservableProperty] private string _expenseDescription = string.Empty;
    [ObservableProperty] private decimal _expenseAmount;
    [ObservableProperty] private AccountingCurrency _expenseCurrency = AccountingCurrency.IQD;
    [ObservableProperty] private decimal _expenseFxRate = 1m;
    [ObservableProperty] private bool _showExpenseFxRate;
    [ObservableProperty] private DateTime _expenseDate = DateTime.Today;
    [ObservableProperty] private string _expenseRecipient = string.Empty;
    [ObservableProperty] private bool _isSavingExpense;

    [ObservableProperty] private bool _isAttachmentsDialogOpen;
    [ObservableProperty] private CustomsInvoiceListItem? _attachmentsTargetInvoice;
    [ObservableProperty] private string _attachmentsDialogSubtitle = string.Empty;
    [ObservableProperty] private bool _canManageListAttachments;

    [ObservableProperty] private bool _canViewExpenses;

    public ObservableCollection<CustomsInvoiceListItem> Invoices { get; } = [];
    public ObservableCollection<CustomsExpenseTypeItem> ExpenseTypes { get; } = [];
    public ObservableCollection<CustomsAttachmentItem> DialogAttachments { get; } = [];

    public IReadOnlyList<CustomsInvoiceStatusOption> StatusOptions { get; } =
    [
        new(null, "الكل"),
        new(CustomsInvoiceStatus.Draft, CustomsInvoiceStatusDisplay.ToArabic(CustomsInvoiceStatus.Draft)),
        new(CustomsInvoiceStatus.Approved, CustomsInvoiceStatusDisplay.ToArabic(CustomsInvoiceStatus.Approved)),
        new(CustomsInvoiceStatus.Cancelled, CustomsInvoiceStatusDisplay.ToArabic(CustomsInvoiceStatus.Cancelled))
    ];

    public IReadOnlyList<CustomsCurrencyOption> CurrencyOptions { get; } =
    [
        new(AccountingCurrency.IQD, "دينار عراقي"),
        new(AccountingCurrency.USD, "دولار أمريكي")
    ];

    public bool HasDetailInvoice => DetailInvoice is not null;
    public bool HasDialogAttachments => DialogAttachments.Count > 0;

    public CustomsInvoiceListViewModel(
        ICustomsInvoiceService invoiceService,
        ICustomsPrintService printService,
        IExportService exportService,
        ICustomsExpenseTypeService expenseTypeService,
        ICustomsAttachmentService attachmentService,
        ICurrentUserService currentUserService,
        MainWindowViewModel mainWindow)
    {
        _invoiceService = invoiceService;
        _printService = printService;
        _exportService = exportService;
        _expenseTypeService = expenseTypeService;
        _attachmentService = attachmentService;
        _currentUserService = currentUserService;
        _mainWindow = mainWindow;
        PageTitle = "الفواتير";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CustomsPermissionRegistry.Invoices);
        CanViewExpenses = _currentUserService.IsAdmin
            || _currentUserService.CanView(CustomsPermissionRegistry.Expenses);
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

    partial void OnExpenseCurrencyChanged(AccountingCurrency value)
    {
        ShowExpenseFxRate = value == AccountingCurrency.USD;
        if (value == AccountingCurrency.IQD)
            ExpenseFxRate = 1m;
    }

    partial void OnDetailInvoiceChanged(CustomsInvoiceDetail? value)
    {
        OnPropertyChanged(nameof(HasDetailInvoice));
        UpdateDetailUiFlags(value);
    }

    private void UpdateDetailUiFlags(CustomsInvoiceDetail? detail)
    {
        if (detail is null)
        {
            DetailIsClearanceGrid = false;
            DetailIsTransportGrid = false;
            DetailIsInternalTransportGrid = false;
            DetailIsExternalTransportGrid = false;
            DetailShowTransportRoute = false;
            DetailShowDelayPenalties = false;
            DetailShowFxRate = false;
            return;
        }

        DetailIsClearanceGrid = detail.ServiceType is CustomsServiceType.Clearance
            or CustomsServiceType.ClearanceAndTransport;
        DetailIsTransportGrid = detail.ServiceType == CustomsServiceType.Transport;
        DetailIsInternalTransportGrid = DetailIsTransportGrid
            && detail.TransportKind == CustomsTransportKind.Internal;
        DetailIsExternalTransportGrid = DetailIsTransportGrid
            && detail.TransportKind == CustomsTransportKind.External;
        DetailShowTransportRoute = DetailIsInternalTransportGrid;
        DetailShowDelayPenalties = DetailIsInternalTransportGrid;
        DetailShowFxRate = detail.Currency == AccountingCurrency.USD;
    }

    /// <summary>M-04: server-side sort from DataGrid header (allowlisted columns only).</summary>
    public Task ApplyServerSortAsync(string sortBy, bool sortDescending)
    {
        SortBy = sortBy;
        SortDescending = sortDescending;
        CurrentPage = 1;
        return LoadInvoicesAsync();
    }

    private void CloseAllDialogs()
    {
        IsCancelDialogOpen = false;
        IsDeleteDialogOpen = false;
        IsDetailDialogOpen = false;
        IsExpenseDialogOpen = false;
        IsAttachmentsDialogOpen = false;
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
        if (item is null) return;

        try
        {
            var detail = await _invoiceService.GetByIdAsync(item.Id);
            if (detail is null)
            {
                BeautifulMessageDialog.ShowError("الفاتورة غير موجودة");
                return;
            }

            CloseAllDialogs();
            DetailInvoice = detail;
            IsDetailDialogOpen = true;
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void CloseDetailDialog()
    {
        IsDetailDialogOpen = false;
        DetailInvoice = null;
    }

    [RelayCommand]
    private async Task EditFromDetailAsync()
    {
        if (DetailInvoice is null) return;
        var id = DetailInvoice.Id;
        var number = DetailInvoice.InvoiceNumber;
        var canEdit = DetailInvoice.CanEdit;
        CloseDetailDialog();

        var item = Invoices.FirstOrDefault(i => i.Id == id)
            ?? new CustomsInvoiceListItem { Id = id, InvoiceNumber = number, Status = canEdit ? CustomsInvoiceStatus.Approved : CustomsInvoiceStatus.Cancelled };

        if (canEdit && CanEdit)
            await EditInvoiceAsync(item);
        else
        {
            CustomsInvoiceNavigationBridge.PendingEditInvoiceId = id;
            await _mainWindow.OpenTabAsync(
                typeof(CustomsInvoiceFormViewModel),
                $"عرض {number}",
                PackIconKind.FileDocumentOutline,
                activateIfExists: false,
                permissionScreenName: CustomsPermissionRegistry.Invoices);
        }
    }

    [RelayCommand]
    private async Task EditInvoiceAsync(CustomsInvoiceListItem? item)
    {
        item ??= SelectedInvoice;
        if (item is null || !item.CanEdit || !CanEdit)
        {
            if (item is not null && !CanEdit)
                BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية تعديل الفاتورة");
            else if (item is not null && !item.CanEdit)
                BeautifulMessageDialog.ShowWarning("لا يمكن تعديل فاتورة ملغاة");
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
        CloseAllDialogs();
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
    private async Task OpenAddExpenseDialogAsync(CustomsInvoiceListItem? item)
    {
        item ??= SelectedInvoice;
        if (item is null) return;

        if (!CanViewExpenses)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية عرض/إضافة المصاريف");
            return;
        }

        if (!item.CanAddDirectExpense || !CanEdit)
        {
            BeautifulMessageDialog.ShowWarning(
                !item.CanAddDirectExpense
                    ? "لا يمكن إضافة مصروف لفاتورة ملغاة"
                    : "ليس لديك صلاحية تعديل الفاتورة لإضافة مصروف");
            return;
        }

        try
        {
            if (ExpenseTypes.Count == 0)
            {
                var types = await _expenseTypeService.GetActiveAsync();
                ExpenseTypes.Clear();
                foreach (var t in types)
                    ExpenseTypes.Add(t);
            }

            CloseAllDialogs();
            ExpenseTargetInvoice = item;
            ExpenseDialogSubtitle = $"فاتورة {item.InvoiceNumber} — {item.CustomerName}";
            ResetExpenseDialogFields(item.Currency);
            IsExpenseDialogOpen = true;
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void CloseExpenseDialog()
    {
        IsExpenseDialogOpen = false;
        ExpenseTargetInvoice = null;
        ResetExpenseDialogFields(AccountingCurrency.IQD);
    }

    private void ResetExpenseDialogFields(AccountingCurrency currency)
    {
        SelectedExpenseType = null;
        NewExpenseTypeName = string.Empty;
        ExpenseDescription = string.Empty;
        ExpenseAmount = 0;
        ExpenseCurrency = currency;
        ExpenseFxRate = 1m;
        ShowExpenseFxRate = currency == AccountingCurrency.USD;
        ExpenseDate = DateTime.Today;
        ExpenseRecipient = string.Empty;
    }

    [RelayCommand]
    private async Task AddExpenseTypeAsync()
    {
        if (string.IsNullOrWhiteSpace(NewExpenseTypeName)) return;
        try
        {
            var created = await _expenseTypeService.CreateAsync(NewExpenseTypeName, _currentUserService.Username);
            if (ExpenseTypes.All(t => t.Id != created.Id))
                ExpenseTypes.Add(created);
            SelectedExpenseType = ExpenseTypes.First(t => t.Id == created.Id);
            NewExpenseTypeName = string.Empty;
            BeautifulMessageDialog.ShowSuccess("تم إضافة نوع المصروف");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task SaveDirectExpenseAsync()
    {
        if (IsSavingExpense || ExpenseTargetInvoice is null) return;
        if (!CanViewExpenses || !CanEdit) return;

        if (SelectedExpenseType is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر نوع المصروف");
            return;
        }

        if (ExpenseAmount < 0)
        {
            BeautifulMessageDialog.ShowWarning("مبلغ المصروف لا يمكن أن يكون سالباً");
            return;
        }

        IsSavingExpense = true;
        try
        {
            var detail = await _invoiceService.GetByIdAsync(ExpenseTargetInvoice.Id);
            if (detail is null)
            {
                BeautifulMessageDialog.ShowError("الفاتورة غير موجودة");
                return;
            }

            if (!detail.CanEdit)
            {
                BeautifulMessageDialog.ShowWarning("لا يمكن إضافة مصروف لفاتورة ملغاة");
                return;
            }

            var fx = ExpenseCurrency == AccountingCurrency.IQD
                ? 1m
                : (ExpenseFxRate > 0 ? ExpenseFxRate : 1m);

            var expenses = detail.Expenses.Select(e => new CustomsInvoiceExpenseDraft
            {
                Id = e.Id,
                ExpenseTypeId = e.ExpenseTypeId,
                ExpenseTypeName = e.ExpenseTypeName,
                Description = e.Description,
                Amount = e.Amount,
                Currency = e.Currency,
                FxRate = e.FxRate,
                ExpenseDate = e.ExpenseDate,
                Recipient = e.Recipient
            }).ToList();

            expenses.Add(new CustomsInvoiceExpenseDraft
            {
                ExpenseTypeId = SelectedExpenseType.Id,
                ExpenseTypeName = SelectedExpenseType.Name,
                Description = ExpenseDescription?.Trim() ?? string.Empty,
                Amount = ExpenseAmount,
                Currency = ExpenseCurrency,
                FxRate = fx,
                ExpenseDate = ExpenseDate,
                Recipient = ExpenseRecipient?.Trim() ?? string.Empty
            });

            var request = ToSaveRequest(detail, expenses);
            await _invoiceService.UpdateAsync(detail.Id, request);
            CloseExpenseDialog();
            BeautifulMessageDialog.ShowSuccess("تم إضافة المصروف مباشرة على الفاتورة");
            await LoadInvoicesAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsSavingExpense = false;
        }
    }

    private CustomsInvoiceSaveRequest ToSaveRequest(
        CustomsInvoiceDetail detail,
        List<CustomsInvoiceExpenseDraft> expenses) =>
        new()
        {
            CustomerId = detail.CustomerId,
            InvoiceDate = detail.InvoiceDate,
            ServiceType = detail.ServiceType,
            TransportKind = detail.TransportKind,
            TransportRoute = detail.TransportRoute,
            Currency = detail.Currency,
            FxRate = detail.FxRate,
            Description = detail.Description,
            DiscountAmount = detail.DiscountAmount,
            InternalNotes = detail.InternalNotes,
            CustomerNotes = detail.CustomerNotes,
            UserName = _currentUserService.Username,
            Vehicles = detail.Vehicles.Select(v => new CustomsInvoiceVehicleDraft
            {
                Id = v.Id,
                SerialNumber = v.SerialNumber,
                IraqiPlate = v.IraqiPlate,
                IraqiDriver = v.IraqiDriver,
                SyrianPlate = v.SyrianPlate,
                SyrianDriver = v.SyrianDriver,
                CargoType = v.CargoType,
                LoadingDate = v.LoadingDate,
                ExitDate = v.ExitDate,
                WarehouseArrivalDate = v.WarehouseArrivalDate,
                UnloadingDate = v.UnloadingDate,
                VehiclePrice = v.VehiclePrice,
                DelayPenaltyAmount = v.DelayPenaltyAmount,
                Status = v.Status,
                Notes = v.Notes
            }).ToList(),
            Expenses = expenses
        };

    [RelayCommand]
    private async Task ViewInvoiceExpensesAsync(CustomsInvoiceListItem? item)
    {
        // Backward-compatible: open direct-add expense dialog (list no longer navigates into form).
        await OpenAddExpenseDialogAsync(item);
    }

    [RelayCommand]
    private async Task ViewInvoiceAttachmentsAsync(CustomsInvoiceListItem? item)
    {
        item ??= SelectedInvoice;
        if (item is null) return;

        try
        {
            var items = await _attachmentService.GetByOwnerAsync(CustomsAttachmentOwnerType.Invoice, item.Id);
            CloseAllDialogs();
            AttachmentsTargetInvoice = item;
            AttachmentsDialogSubtitle = $"فاتورة {item.InvoiceNumber} — {item.CustomerName}";
            CanManageListAttachments = CanEdit && item.CanEdit;
            DialogAttachments.Clear();
            foreach (var a in items)
                DialogAttachments.Add(a);
            OnPropertyChanged(nameof(HasDialogAttachments));
            IsAttachmentsDialogOpen = true;
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void CloseAttachmentsDialog()
    {
        IsAttachmentsDialogOpen = false;
        AttachmentsTargetInvoice = null;
        DialogAttachments.Clear();
        OnPropertyChanged(nameof(HasDialogAttachments));
    }

    [RelayCommand]
    private async Task AddListAttachmentAsync()
    {
        if (AttachmentsTargetInvoice is null || !CanManageListAttachments)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية إضافة مرفقات لهذه الفاتورة");
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "صور ومستندات|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.pdf;*.doc;*.docx;*.xls;*.xlsx|كل الملفات|*.*",
            Multiselect = true,
            Title = "اختر مرفقات الفاتورة"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            foreach (var path in dialog.FileNames)
            {
                await _attachmentService.AddFromFileAsync(
                    CustomsAttachmentOwnerType.Invoice,
                    AttachmentsTargetInvoice.Id,
                    path,
                    null,
                    _currentUserService.Username);
            }

            var items = await _attachmentService.GetByOwnerAsync(
                CustomsAttachmentOwnerType.Invoice, AttachmentsTargetInvoice.Id);
            DialogAttachments.Clear();
            foreach (var a in items)
                DialogAttachments.Add(a);
            OnPropertyChanged(nameof(HasDialogAttachments));
            BeautifulMessageDialog.ShowSuccess($"تم إرفاق {dialog.FileNames.Length} ملف");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void OpenListAttachment(CustomsAttachmentItem? item)
    {
        if (item is null || !File.Exists(item.FilePath))
        {
            BeautifulMessageDialog.ShowWarning("الملف غير موجود على الجهاز");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo { FileName = item.FilePath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteListAttachmentAsync(CustomsAttachmentItem? item)
    {
        if (item is null || !CanDelete || !CanManageListAttachments) return;
        if (!BeautifulMessageDialog.ShowConfirm($"حذف المرفق «{item.FileName}»؟"))
            return;

        try
        {
            await _attachmentService.SoftDeleteAsync(item.Id, _currentUserService.Username);
            DialogAttachments.Remove(item);
            OnPropertyChanged(nameof(HasDialogAttachments));
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
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
        if (item is null || !item.CanSoftDelete || !CanDelete) return;
        CloseAllDialogs();
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
