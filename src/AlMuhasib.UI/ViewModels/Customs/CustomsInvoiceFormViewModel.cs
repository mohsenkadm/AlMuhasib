using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Customs;
using AlMuhasib.Core.Models.Customs;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels.Customs;

public partial class CustomsInvoiceFormViewModel : ViewModelBase
{
    private readonly ICustomsInvoiceService _invoiceService;
    private readonly ICustomsCustomerService _customerService;
    private readonly ICustomsExpenseTypeService _expenseTypeService;
    private readonly ICustomsAttachmentService _attachmentService;
    private readonly ICustomsPrintService _printService;
    private readonly ICurrentUserService _currentUserService;
    private readonly MainWindowViewModel _mainWindow;
    private int? _editId;

    [ObservableProperty] private string _invoiceNumber = "يُنشأ تلقائياً";
    [ObservableProperty] private DateTime _invoiceDate = DateTime.Today;
    [ObservableProperty] private CustomsCustomerListItem? _selectedCustomer;
    [ObservableProperty] private CustomsServiceType _serviceType = CustomsServiceType.Clearance;
    [ObservableProperty] private CustomsTransportKind _transportKind = CustomsTransportKind.Internal;
    [ObservableProperty] private bool _showTransportKind;
    [ObservableProperty] private bool _showTransportRoute;
    [ObservableProperty] private bool _showDelayPenaltyColumn;
    [ObservableProperty] private string _transportRoute = string.Empty;
    [ObservableProperty] private AccountingCurrency _currency = AccountingCurrency.IQD;
    [ObservableProperty] private decimal _fxRate = 1m;
    [ObservableProperty] private bool _showFxRate;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private decimal _discountAmount;
    [ObservableProperty] private string _internalNotes = string.Empty;
    [ObservableProperty] private string _customerNotes = string.Empty;
    [ObservableProperty] private CustomsInvoiceStatus _status = CustomsInvoiceStatus.Draft;
    [ObservableProperty] private string _statusLabel = "مسودة";
    [ObservableProperty] private decimal _paidAmount;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private bool _canApprove;
    [ObservableProperty] private bool _canCancelInvoice;
    [ObservableProperty] private string _formError = string.Empty;

    [ObservableProperty] private decimal _vehiclesSum;
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private decimal _remainingAmount;
    [ObservableProperty] private decimal _expensesSameCurrencySum;
    [ObservableProperty] private decimal _expensesOtherCurrencySum;
    [ObservableProperty] private decimal _internalProfit;

    [ObservableProperty] private string _vehicleSearchText = string.Empty;
    [ObservableProperty] private bool _isVehicleDetailDialogOpen;
    [ObservableProperty] private CustomsInvoiceVehicleRow? _detailVehicle;

    [ObservableProperty] private bool _isExpenseDialogOpen;
    [ObservableProperty] private bool _isExpenseEditMode;
    [ObservableProperty] private string _expenseDialogTitle = "إضافة مصروف";
    [ObservableProperty] private string _newExpenseTypeName = string.Empty;
    [ObservableProperty] private CustomsExpenseTypeItem? _selectedExpenseType;
    [ObservableProperty] private string _expenseDescription = string.Empty;
    [ObservableProperty] private decimal _expenseAmount;
    [ObservableProperty] private AccountingCurrency _expenseCurrency = AccountingCurrency.IQD;
    [ObservableProperty] private decimal _expenseFxRate = 1m;
    [ObservableProperty] private bool _showExpenseFxRate;
    [ObservableProperty] private DateTime _expenseDate = DateTime.Today;
    [ObservableProperty] private string _expenseRecipient = string.Empty;

    private CustomsInvoiceExpenseRow? _editingExpenseRow;

    public ObservableCollection<CustomsCustomerListItem> Customers { get; } = [];
    public ObservableCollection<CustomsInvoiceVehicleRow> Vehicles { get; } = [];
    public ObservableCollection<CustomsInvoiceVehicleRow> FilteredVehicles { get; } = [];
    public ObservableCollection<CustomsInvoiceExpenseRow> Expenses { get; } = [];
    public ObservableCollection<CustomsAttachmentItem> Attachments { get; } = [];
    public ObservableCollection<CustomsAttachmentItem> VehicleAttachments { get; } = [];
    public ObservableCollection<CustomsAttachmentItem> ExpenseAttachments { get; } = [];
    public ObservableCollection<CustomsExpenseTypeItem> ExpenseTypes { get; } = [];

    public IReadOnlyList<CustomsServiceTypeOption> ServiceTypeOptions { get; } =
    [
        new(CustomsServiceType.Clearance, CustomsServiceTypeDisplay.ToArabic(CustomsServiceType.Clearance)),
        new(CustomsServiceType.Transport, CustomsServiceTypeDisplay.ToArabic(CustomsServiceType.Transport)),
        new(CustomsServiceType.ClearanceAndTransport, CustomsServiceTypeDisplay.ToArabic(CustomsServiceType.ClearanceAndTransport))
    ];

    public IReadOnlyList<CustomsTransportKindOption> TransportKindOptions { get; } =
    [
        new(CustomsTransportKind.Internal, CustomsTransportKindDisplay.ToArabic(CustomsTransportKind.Internal)),
        new(CustomsTransportKind.External, CustomsTransportKindDisplay.ToArabic(CustomsTransportKind.External))
    ];

    public IReadOnlyList<CustomsCurrencyOption> CurrencyOptions { get; } =
    [
        new(AccountingCurrency.IQD, "دينار عراقي"),
        new(AccountingCurrency.USD, "دولار أمريكي")
    ];

    public IReadOnlyList<CustomsVehicleStatusOption> VehicleStatusOptions { get; } =
    [
        new(CustomsVehicleStatus.InProgress, CustomsVehicleStatusDisplay.ToArabic(CustomsVehicleStatus.InProgress)),
        new(CustomsVehicleStatus.Exited, CustomsVehicleStatusDisplay.ToArabic(CustomsVehicleStatus.Exited)),
        new(CustomsVehicleStatus.Arrived, CustomsVehicleStatusDisplay.ToArabic(CustomsVehicleStatus.Arrived))
    ];

    public bool IsEditMode => _editId.HasValue;
    public bool CanSave => (IsEditMode ? CanEdit : CanAdd) && Status != CustomsInvoiceStatus.Cancelled;
    public bool HasSavedInvoice => _editId.HasValue;
    public bool CanPrintDocument => HasSavedInvoice && CanPrint;
    public bool CanExportDocument => HasSavedInvoice && CanExport;
    public bool CanManageAttachments => CanEdit && Status != CustomsInvoiceStatus.Cancelled;
    /// <summary>التعديل/الإضافة مسموحان إذا كانت الفاتورة قابلة للحفظ (إضافة أو تعديل حسب الوضع).</summary>
    public bool IsVehiclesReadOnly => !CanSave;
    public bool CanAddVehicle => CanSave;
    public bool CanDeleteVehicle => CanDelete && Status != CustomsInvoiceStatus.Cancelled;
    public bool HasSavedDetailVehicle => DetailVehicle?.Id is > 0;
    public bool CanAddExpense => CanViewExpenses && CanSave;
    public bool CanEditExpense => CanViewExpenses && CanSave;
    public bool CanDeleteExpense => CanViewExpenses && CanDelete && Status != CustomsInvoiceStatus.Cancelled;
    public bool HasSavedEditingExpense => _editingExpenseRow?.Id is > 0;
    public string InvoiceCurrencyLabel => Currency == AccountingCurrency.USD ? "$" : "د.ع";

    [ObservableProperty] private bool _canViewExpenses;
    [ObservableProperty] private bool _canViewProfit;

    /// <summary>UI banner after navigation from list (expenses / attachments / vehicles).</summary>
    [ObservableProperty] private string _focusSectionHint = string.Empty;
    public bool HasFocusSectionHint => !string.IsNullOrWhiteSpace(FocusSectionHint);

    public CustomsInvoiceFormViewModel(
        ICustomsInvoiceService invoiceService,
        ICustomsCustomerService customerService,
        ICustomsExpenseTypeService expenseTypeService,
        ICustomsAttachmentService attachmentService,
        ICustomsPrintService printService,
        ICurrentUserService currentUserService,
        MainWindowViewModel mainWindow)
    {
        _invoiceService = invoiceService;
        _customerService = customerService;
        _expenseTypeService = expenseTypeService;
        _attachmentService = attachmentService;
        _printService = printService;
        _currentUserService = currentUserService;
        _mainWindow = mainWindow;
        PageTitle = "فاتورة تخليص";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CustomsPermissionRegistry.Invoices);
        CanViewExpenses = _currentUserService.IsAdmin
            || _currentUserService.CanView(CustomsPermissionRegistry.Expenses);
        CanViewProfit = _currentUserService.IsAdmin
            || _currentUserService.CanView(CustomsPermissionRegistry.ProfitReport);
        await LoadLookupsAsync();

        var pendingId = CustomsInvoiceNavigationBridge.TakePendingEditInvoiceId();
        var pendingFocus = CustomsInvoiceNavigationBridge.TakePendingFocusSection();
        if (pendingId is int editId)
            await LoadInvoiceAsync(editId);
        else
            await ResetForNewAsync();

        ApplyPendingServiceType();
        ApplyFocusSectionHint(pendingFocus);
    }

    private void ApplyPendingServiceType()
    {
        UpdateTransportUiFlags();
        var pendingType = CustomsInvoiceNavigationBridge.TakePendingServiceType();
        if (pendingType is null || _editId.HasValue)
            return;
        ServiceType = pendingType.Value;
    }

    private void ApplyFocusSectionHint(string? section)
    {
        FocusSectionHint = section switch
        {
            "expenses" => "انتقل إلى قسم المصاريف أدناه لعرض مصاريف هذه الفاتورة.",
            "attachments" => "انتقل إلى قسم المرفقات أدناه لعرض مرفقات هذه الفاتورة.",
            "vehicles" => "انتقل إلى جدول السيارات والشحنات أدناه.",
            _ => string.Empty
        };
        OnPropertyChanged(nameof(HasFocusSectionHint));
    }

    partial void OnFocusSectionHintChanged(string value) =>
        OnPropertyChanged(nameof(HasFocusSectionHint));

    private async Task LoadLookupsAsync()
    {
        var (customers, _) = await _customerService.GetPagedAsync(1, 500, activeOnly: true);
        Customers.Clear();
        foreach (var c in customers)
            Customers.Add(c);

        ExpenseTypes.Clear();
        if (CanViewExpenses)
        {
            var types = await _expenseTypeService.GetActiveAsync();
            foreach (var t in types)
                ExpenseTypes.Add(t);
        }
    }

    private async Task ResetForNewAsync()
    {
        _editId = null;
        PageTitle = "فاتورة جديدة";
        InvoiceNumber = await _invoiceService.PreviewNextInvoiceNumberAsync();
        InvoiceDate = DateTime.Today;
        SelectedCustomer = null;
        ServiceType = CustomsServiceType.Clearance;
        TransportKind = CustomsTransportKind.Internal;
        TransportRoute = string.Empty;
        UpdateTransportUiFlags();
        Currency = AccountingCurrency.IQD;
        FxRate = 1m;
        ShowFxRate = false;
        Description = string.Empty;
        DiscountAmount = 0;
        InternalNotes = string.Empty;
        CustomerNotes = string.Empty;
        Status = CustomsInvoiceStatus.Draft;
        StatusLabel = CustomsInvoiceStatusDisplay.ToArabic(Status);
        PaidAmount = 0;
        CanApprove = false;
        CanCancelInvoice = false;
        FormError = string.Empty;
        Vehicles.Clear();
        FilteredVehicles.Clear();
        Expenses.Clear();
        Attachments.Clear();
        VehicleAttachments.Clear();
        ExpenseAttachments.Clear();
        CloseVehicleDetail();
        CloseExpenseDialog();
        AddVehicleRow();
        RecalculateTotals();
        NotifyFormStateChanged();
    }

    private async Task LoadInvoiceAsync(int id)
    {
        try
        {
            var detail = await _invoiceService.GetByIdAsync(id);
            if (detail is null)
            {
                BeautifulMessageDialog.ShowError("الفاتورة غير موجودة");
                await ResetForNewAsync();
                return;
            }

            _editId = detail.Id;
            PageTitle = $"تعديل {detail.InvoiceNumber}";
            InvoiceNumber = detail.InvoiceNumber;
            InvoiceDate = detail.InvoiceDate;
            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == detail.CustomerId)
                ?? new CustomsCustomerListItem { Id = detail.CustomerId, Name = detail.CustomerName };
            if (SelectedCustomer is not null && Customers.All(c => c.Id != SelectedCustomer.Id))
                Customers.Insert(0, SelectedCustomer);

            ServiceType = detail.ServiceType;
            TransportKind = detail.TransportKind ?? CustomsTransportKind.Internal;
            TransportRoute = detail.TransportRoute ?? string.Empty;
            UpdateTransportUiFlags();
            Currency = detail.Currency;
            FxRate = detail.FxRate;
            ShowFxRate = detail.Currency == AccountingCurrency.USD;
            Description = detail.Description;
            DiscountAmount = detail.DiscountAmount;
            InternalNotes = detail.InternalNotes;
            CustomerNotes = detail.CustomerNotes;
            Status = detail.Status;
            StatusLabel = detail.StatusLabel;
            PaidAmount = detail.PaidAmount;
            CanApprove = detail.CanApprove && CanEdit;
            CanCancelInvoice = detail.CanCancel && CanEdit;

            Vehicles.Clear();
            foreach (var v in detail.Vehicles)
                Vehicles.Add(CreateVehicleRow(v));
            if (Vehicles.Count == 0)
                AddVehicleRow();
            else
                RefreshFilteredVehicles();

            Expenses.Clear();
            foreach (var e in detail.Expenses)
                Expenses.Add(CustomsInvoiceExpenseRow.FromDraft(e));

            Attachments.Clear();
            foreach (var a in detail.Attachments)
                Attachments.Add(a);

            RecalculateTotals();
            NotifyFormStateChanged();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    partial void OnCurrencyChanged(AccountingCurrency value)
    {
        ShowFxRate = value == AccountingCurrency.USD;
        if (value == AccountingCurrency.IQD)
            FxRate = 1m;
        RecalculateTotals();
    }

    partial void OnServiceTypeChanged(CustomsServiceType value)
    {
        if (value == CustomsServiceType.Transport && TransportKind == default)
            TransportKind = CustomsTransportKind.Internal;
        UpdateTransportUiFlags();
        RecalculateTotals();
    }

    partial void OnTransportKindChanged(CustomsTransportKind value)
    {
        UpdateTransportUiFlags();
        RecalculateTotals();
    }

    private void UpdateTransportUiFlags()
    {
        ShowTransportKind = ServiceType == CustomsServiceType.Transport;
        ShowTransportRoute = ShowTransportKind && TransportKind == CustomsTransportKind.Internal;
        ShowDelayPenaltyColumn = ShowTransportRoute;
    }

    partial void OnDiscountAmountChanged(decimal value) => RecalculateTotals();

    partial void OnVehicleSearchTextChanged(string value) => RefreshFilteredVehicles();

    protected override void OnColumnFiltersChanged() => RefreshFilteredVehicles();

    private void NotifyFormStateChanged()
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(HasSavedInvoice));
        OnPropertyChanged(nameof(CanPrintDocument));
        OnPropertyChanged(nameof(CanExportDocument));
        OnPropertyChanged(nameof(CanManageAttachments));
        OnPropertyChanged(nameof(IsVehiclesReadOnly));
        OnPropertyChanged(nameof(CanAddVehicle));
        OnPropertyChanged(nameof(CanDeleteVehicle));
        OnPropertyChanged(nameof(CanAddExpense));
        OnPropertyChanged(nameof(CanEditExpense));
        OnPropertyChanged(nameof(CanDeleteExpense));
        OnPropertyChanged(nameof(InvoiceCurrencyLabel));
    }

    private CustomsInvoiceVehicleRow CreateVehicleRow(CustomsInvoiceVehicleDraft? draft = null)
    {
        var row = draft is null
            ? new CustomsInvoiceVehicleRow { Status = CustomsVehicleStatus.InProgress }
            : CustomsInvoiceVehicleRow.FromDraft(draft);
        row.PriceChanged = RecalculateTotals;
        return row;
    }

    private void RecalculateTotals()
    {
        VehiclesSum = Vehicles.Sum(v => v.VehiclePrice);
        var delaySum = ShowDelayPenaltyColumn
            ? Vehicles.Sum(v => v.DelayPenaltyAmount)
            : 0m;
        // Revenue = vehicles (+ internal delay penalties) − discount
        TotalAmount = Math.Max(0, VehiclesSum + delaySum - DiscountAmount);
        RemainingAmount = Math.Max(0, TotalAmount - PaidAmount);
        RecalculateInternalProfit();
    }

    private void RecalculateInternalProfit()
    {
        ExpensesSameCurrencySum = Expenses
            .Where(e => e.Currency == Currency)
            .Sum(e => e.Amount);
        ExpensesOtherCurrencySum = Expenses
            .Where(e => e.Currency != Currency)
            .Sum(e => e.Amount);
        // InternalProfit = Revenue − same-currency expenses only (never PaidAmount / customer balance)
        InternalProfit = TotalAmount - ExpensesSameCurrencySum;
    }

    private void RefreshFilteredVehicles()
    {
        IEnumerable<CustomsInvoiceVehicleRow> query = Vehicles;

        if (!string.IsNullOrWhiteSpace(VehicleSearchText))
        {
            var term = VehicleSearchText.Trim();
            query = query.Where(v =>
                (v.SerialNumber?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (v.IraqiPlate?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (v.IraqiDriver?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (v.SyrianPlate?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (v.SyrianDriver?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (v.CargoType?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (v.StatusLabel?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (v.Notes?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var filtered = ColumnFilterEngine.Apply(query, ColumnFilters);
        FilteredVehicles.Clear();
        foreach (var row in filtered)
            FilteredVehicles.Add(row);
    }

    [RelayCommand]
    private void AddVehicleRow()
    {
        if (!CanAddVehicle) return;
        Vehicles.Add(CreateVehicleRow());
        RefreshFilteredVehicles();
        RecalculateTotals();
    }

    [RelayCommand]
    private void RemoveVehicleRow(CustomsInvoiceVehicleRow? row)
    {
        if (row is null || !CanDeleteVehicle) return;
        if (!BeautifulMessageDialog.ShowConfirm("حذف هذه السيارة من الفاتورة؟"))
            return;

        Vehicles.Remove(row);
        if (ReferenceEquals(DetailVehicle, row))
            CloseVehicleDetail();

        if (Vehicles.Count == 0)
            AddVehicleRow();
        else
            RefreshFilteredVehicles();
        RecalculateTotals();
    }

    [RelayCommand]
    private void VehiclePriceChanged() => RecalculateTotals();

    [RelayCommand]
    private void OpenVehicleDetails(CustomsInvoiceVehicleRow? row)
    {
        if (row is null) return;
        DetailVehicle = row;
        IsVehicleDetailDialogOpen = true;
        OnPropertyChanged(nameof(HasSavedDetailVehicle));
        _ = LoadVehicleAttachmentsForDetailAsync();
    }

    [RelayCommand]
    private void CloseVehicleDetail()
    {
        IsVehicleDetailDialogOpen = false;
        DetailVehicle = null;
        OnPropertyChanged(nameof(HasSavedDetailVehicle));
        RecalculateTotals();
        RefreshFilteredVehicles();
    }

    [RelayCommand]
    private void ApplyVehicleDetail()
    {
        RecalculateTotals();
        RefreshFilteredVehicles();
        IsVehicleDetailDialogOpen = false;
        DetailVehicle = null;
        OnPropertyChanged(nameof(HasSavedDetailVehicle));
    }

    private async Task LoadVehicleAttachmentsForDetailAsync()
    {
        VehicleAttachments.Clear();
        if (DetailVehicle?.Id is not int id || id <= 0) return;
        try
        {
            var items = await _attachmentService.GetByOwnerAsync(CustomsAttachmentOwnerType.Vehicle, id);
            foreach (var item in items)
                VehicleAttachments.Add(item);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    private async Task LoadVehicleAttachmentsAsync(int vehicleId)
    {
        VehicleAttachments.Clear();
        var items = await _attachmentService.GetByOwnerAsync(CustomsAttachmentOwnerType.Vehicle, vehicleId);
        foreach (var item in items)
            VehicleAttachments.Add(item);
    }

    [RelayCommand]
    private async Task AddVehicleAttachmentAsync()
    {
        if (!CanManageAttachments)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية إضافة مرفقات");
            return;
        }

        var vehicleId = DetailVehicle?.Id;
        if (vehicleId is null or <= 0)
        {
            BeautifulMessageDialog.ShowWarning("احفظ الفاتورة أولاً قبل إرفاق ملفات للسيارة");
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "صور ومستندات|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.pdf;*.doc;*.docx;*.xls;*.xlsx|كل الملفات|*.*",
            Multiselect = true,
            Title = "اختر مرفقات السيارة"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            foreach (var path in dialog.FileNames)
            {
                await _attachmentService.AddFromFileAsync(
                    CustomsAttachmentOwnerType.Vehicle,
                    vehicleId.Value,
                    path,
                    null,
                    _currentUserService.Username);
            }

            await LoadVehicleAttachmentsAsync(vehicleId.Value);
            BeautifulMessageDialog.ShowSuccess($"تم إرفاق {dialog.FileNames.Length} ملف");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteVehicleAttachmentAsync(CustomsAttachmentItem? item)
    {
        if (item is null || !CanDelete) return;
        if (!BeautifulMessageDialog.ShowConfirm($"حذف المرفق «{item.FileName}»؟"))
            return;

        try
        {
            await _attachmentService.SoftDeleteAsync(item.Id, _currentUserService.Username);
            VehicleAttachments.Remove(item);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    partial void OnExpenseCurrencyChanged(AccountingCurrency value)
    {
        ShowExpenseFxRate = value == AccountingCurrency.USD;
        if (value == AccountingCurrency.IQD)
            ExpenseFxRate = 1m;
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
    private void OpenAddExpenseDialog()
    {
        if (!CanAddExpense) return;
        _editingExpenseRow = null;
        IsExpenseEditMode = false;
        ExpenseDialogTitle = "إضافة مصروف";
        ResetExpenseDialogFields();
        ExpenseCurrency = Currency;
        ExpenseAttachments.Clear();
        OnPropertyChanged(nameof(HasSavedEditingExpense));
        IsExpenseDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditExpenseDialog(CustomsInvoiceExpenseRow? row)
    {
        if (row is null || !CanEditExpense) return;
        _editingExpenseRow = row;
        IsExpenseEditMode = true;
        ExpenseDialogTitle = "تعديل مصروف";
        SelectedExpenseType = ExpenseTypes.FirstOrDefault(t => t.Id == row.ExpenseTypeId)
            ?? new CustomsExpenseTypeItem { Id = row.ExpenseTypeId, Name = row.ExpenseTypeName, IsActive = true };
        if (SelectedExpenseType is not null && ExpenseTypes.All(t => t.Id != SelectedExpenseType.Id))
            ExpenseTypes.Insert(0, SelectedExpenseType);

        ExpenseDescription = row.Description;
        ExpenseAmount = row.Amount;
        ExpenseCurrency = row.Currency;
        ExpenseFxRate = row.FxRate > 0 ? row.FxRate : 1m;
        ShowExpenseFxRate = row.Currency == AccountingCurrency.USD;
        ExpenseDate = row.ExpenseDate;
        ExpenseRecipient = row.Recipient;
        NewExpenseTypeName = string.Empty;
        OnPropertyChanged(nameof(HasSavedEditingExpense));
        IsExpenseDialogOpen = true;
        _ = LoadExpenseAttachmentsForEditAsync();
    }

    [RelayCommand]
    private void CloseExpenseDialog()
    {
        IsExpenseDialogOpen = false;
        _editingExpenseRow = null;
        IsExpenseEditMode = false;
        ExpenseAttachments.Clear();
        ResetExpenseDialogFields();
        OnPropertyChanged(nameof(HasSavedEditingExpense));
    }

    private async Task LoadExpenseAttachmentsForEditAsync()
    {
        ExpenseAttachments.Clear();
        if (_editingExpenseRow?.Id is not int id || id <= 0) return;
        try
        {
            var items = await _attachmentService.GetByOwnerAsync(CustomsAttachmentOwnerType.Expense, id);
            foreach (var item in items)
                ExpenseAttachments.Add(item);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    private async Task LoadExpenseAttachmentsAsync(int expenseId)
    {
        ExpenseAttachments.Clear();
        var items = await _attachmentService.GetByOwnerAsync(CustomsAttachmentOwnerType.Expense, expenseId);
        foreach (var item in items)
            ExpenseAttachments.Add(item);
    }

    [RelayCommand]
    private async Task AddExpenseAttachmentAsync()
    {
        if (!CanManageAttachments || !CanViewExpenses)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية إضافة مرفقات المصروف");
            return;
        }

        var expenseId = _editingExpenseRow?.Id;
        if (expenseId is null or <= 0)
        {
            BeautifulMessageDialog.ShowWarning("احفظ الفاتورة أولاً قبل إرفاق ملفات للمصروف");
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "صور ومستندات|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.pdf;*.doc;*.docx;*.xls;*.xlsx|كل الملفات|*.*",
            Multiselect = true,
            Title = "اختر مرفقات المصروف"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            foreach (var path in dialog.FileNames)
            {
                await _attachmentService.AddFromFileAsync(
                    CustomsAttachmentOwnerType.Expense,
                    expenseId.Value,
                    path,
                    null,
                    _currentUserService.Username);
            }

            await LoadExpenseAttachmentsAsync(expenseId.Value);
            BeautifulMessageDialog.ShowSuccess($"تم إرفاق {dialog.FileNames.Length} ملف");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteExpenseAttachmentAsync(CustomsAttachmentItem? item)
    {
        if (item is null || !CanDelete) return;
        if (!BeautifulMessageDialog.ShowConfirm($"حذف المرفق «{item.FileName}»؟"))
            return;

        try
        {
            await _attachmentService.SoftDeleteAsync(item.Id, _currentUserService.Username);
            ExpenseAttachments.Remove(item);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    private void ResetExpenseDialogFields()
    {
        SelectedExpenseType = null;
        NewExpenseTypeName = string.Empty;
        ExpenseDescription = string.Empty;
        ExpenseAmount = 0;
        ExpenseCurrency = Currency;
        ExpenseFxRate = 1m;
        ShowExpenseFxRate = ExpenseCurrency == AccountingCurrency.USD;
        ExpenseDate = DateTime.Today;
        ExpenseRecipient = string.Empty;
    }

    [RelayCommand]
    private void SaveExpenseDialog()
    {
        if (_editingExpenseRow is null)
        {
            if (!CanAddExpense) return;
        }
        else if (!CanEditExpense)
        {
            return;
        }

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

        var fx = ExpenseCurrency == AccountingCurrency.IQD
            ? 1m
            : (ExpenseFxRate > 0 ? ExpenseFxRate : 1m);

        if (_editingExpenseRow is null)
        {
            Expenses.Add(new CustomsInvoiceExpenseRow
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
        }
        else
        {
            _editingExpenseRow.ExpenseTypeId = SelectedExpenseType.Id;
            _editingExpenseRow.ExpenseTypeName = SelectedExpenseType.Name;
            _editingExpenseRow.Description = ExpenseDescription?.Trim() ?? string.Empty;
            _editingExpenseRow.Amount = ExpenseAmount;
            _editingExpenseRow.Currency = ExpenseCurrency;
            _editingExpenseRow.FxRate = fx;
            _editingExpenseRow.ExpenseDate = ExpenseDate;
            _editingExpenseRow.Recipient = ExpenseRecipient?.Trim() ?? string.Empty;
        }

        RecalculateInternalProfit();
        CloseExpenseDialog();
    }

    [RelayCommand]
    private void RemoveExpenseRow(CustomsInvoiceExpenseRow? row)
    {
        if (row is null || !CanDeleteExpense) return;
        if (!BeautifulMessageDialog.ShowConfirm("حذف هذا المصروف من الفاتورة؟"))
            return;

        if (ReferenceEquals(_editingExpenseRow, row))
            CloseExpenseDialog();

        Expenses.Remove(row);
        RecalculateInternalProfit();
    }

    [RelayCommand]
    private async Task SaveAsync() => await SaveInternalAsync(approveAfterSave: false);

    [RelayCommand]
    private async Task SaveAndApproveAsync() => await SaveInternalAsync(approveAfterSave: true);

    private async Task SaveInternalAsync(bool approveAfterSave)
    {
        if (IsSaving) return;

        FormError = string.Empty;
        if (!CanSave)
        {
            FormError = "ليس لديك صلاحية الحفظ أو الفاتورة ملغاة";
            return;
        }

        if (SelectedCustomer is null)
        {
            FormError = "اختر العميل";
            return;
        }

        var vehicleDrafts = Vehicles
            .Where(v => !string.IsNullOrWhiteSpace(v.IraqiPlate)
                        || !string.IsNullOrWhiteSpace(v.SyrianPlate)
                        || !string.IsNullOrWhiteSpace(v.SerialNumber)
                        || v.VehiclePrice > 0)
            .Select(v => v.ToDraft())
            .ToList();

        var request = new CustomsInvoiceSaveRequest
        {
            CustomerId = SelectedCustomer.Id,
            InvoiceDate = InvoiceDate,
            ServiceType = ServiceType,
            TransportKind = ServiceType == CustomsServiceType.Transport ? TransportKind : null,
            TransportRoute = ShowTransportRoute ? TransportRoute : string.Empty,
            Currency = Currency,
            FxRate = Currency == AccountingCurrency.USD ? FxRate : 1m,
            Description = Description,
            DiscountAmount = DiscountAmount,
            InternalNotes = InternalNotes,
            CustomerNotes = CustomerNotes,
            UserName = _currentUserService.Username,
            Vehicles = vehicleDrafts,
            Expenses = Expenses.Select(e => e.ToDraft()).ToList()
        };

        IsSaving = true;
        try
        {
            CustomsInvoiceDetail saved;
            if (_editId.HasValue)
                saved = await _invoiceService.UpdateAsync(_editId.Value, request);
            else
                saved = await _invoiceService.CreateAsync(request);

            _editId = saved.Id;
            InvoiceNumber = saved.InvoiceNumber;
            Status = saved.Status;
            StatusLabel = saved.StatusLabel;
            PaidAmount = saved.PaidAmount;
            CanApprove = saved.CanApprove && CanEdit;
            CanCancelInvoice = saved.CanCancel && CanEdit;

            if (approveAfterSave)
            {
                await _invoiceService.ApproveAsync(saved.Id, _currentUserService.Username);
                BeautifulMessageDialog.ShowSuccess("تم الحفظ والاعتماد");
            }
            else
            {
                BeautifulMessageDialog.ShowSuccess("تم حفظ الفاتورة");
            }

            await LoadInvoiceAsync(saved.Id);
        }
        catch (Exception ex)
        {
            FormError = ex.Message;
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsSaving = false;
            NotifyFormStateChanged();
        }
    }

    [RelayCommand]
    private async Task ApproveAsync()
    {
        if (!_editId.HasValue || !CanApprove || IsSaving) return;
        IsSaving = true;
        try
        {
            await _invoiceService.ApproveAsync(_editId.Value, _currentUserService.Username);
            BeautifulMessageDialog.ShowSuccess("تم اعتماد الفاتورة");
            await LoadInvoiceAsync(_editId.Value);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsSaving = false;
            NotifyFormStateChanged();
        }
    }

    [RelayCommand]
    private async Task CancelInvoiceAsync()
    {
        if (!_editId.HasValue || !CanCancelInvoice) return;
        if (!BeautifulMessageDialog.ShowConfirm("تأكيد إلغاء هذه الفاتورة؟"))
            return;

        try
        {
            await _invoiceService.CancelAsync(_editId.Value, _currentUserService.Username);
            BeautifulMessageDialog.ShowSuccess("تم إلغاء الفاتورة");
            await LoadInvoiceAsync(_editId.Value);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task AddAttachmentAsync()
    {
        if (!CanManageAttachments)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية إضافة مرفقات");
            return;
        }

        if (!_editId.HasValue)
        {
            BeautifulMessageDialog.ShowWarning("احفظ الفاتورة أولاً قبل إرفاق الملفات");
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "صور ومستندات|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.pdf;*.doc;*.docx;*.xls;*.xlsx|كل الملفات|*.*",
            Multiselect = true,
            Title = "اختر المرفقات"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            foreach (var path in dialog.FileNames)
            {
                await _attachmentService.AddFromFileAsync(
                    CustomsAttachmentOwnerType.Invoice,
                    _editId.Value,
                    path,
                    null,
                    _currentUserService.Username);
            }

            var items = await _attachmentService.GetByOwnerAsync(
                CustomsAttachmentOwnerType.Invoice, _editId.Value);
            Attachments.Clear();
            foreach (var item in items)
                Attachments.Add(item);
            BeautifulMessageDialog.ShowSuccess($"تم إرفاق {dialog.FileNames.Length} ملف");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void OpenAttachment(CustomsAttachmentItem? item)
    {
        if (item is null || !File.Exists(item.FilePath)) return;
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
    private async Task DeleteAttachmentAsync(CustomsAttachmentItem? item)
    {
        if (item is null || !CanDelete) return;
        if (!BeautifulMessageDialog.ShowConfirm($"حذف المرفق «{item.FileName}»؟"))
            return;

        try
        {
            await _attachmentService.SoftDeleteAsync(item.Id, _currentUserService.Username);
            Attachments.Remove(item);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        if (!_editId.HasValue || !CanPrint)
        {
            BeautifulMessageDialog.ShowWarning(
                !_editId.HasValue ? "احفظ الفاتورة أولاً قبل الطباعة" : "ليس لديك صلاحية الطباعة");
            return;
        }

        try
        {
            var detail = await _invoiceService.GetByIdAsync(_editId.Value);
            if (detail is null)
            {
                BeautifulMessageDialog.ShowError("الفاتورة غير موجودة");
                return;
            }

            // Customer copy via ICustomsPrintService — no expenses / InternalNotes / profit.
            _printService.PrintCustomerInvoice(detail, SelectedCustomer?.Phone);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        if (!_editId.HasValue || !CanExport)
        {
            BeautifulMessageDialog.ShowWarning(
                !_editId.HasValue ? "احفظ الفاتورة أولاً قبل التصدير" : "ليس لديك صلاحية التصدير");
            return;
        }

        try
        {
            var detail = await _invoiceService.GetByIdAsync(_editId.Value);
            if (detail is null)
            {
                BeautifulMessageDialog.ShowError("الفاتورة غير موجودة");
                return;
            }

            var temp = _printService.ExportCustomerInvoicePdf(detail, SelectedCustomer?.Phone);
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
    private void Close() => _mainWindow.CloseTabForViewModel(this);
}

public sealed record CustomsServiceTypeOption(CustomsServiceType Value, string Label);
public sealed record CustomsTransportKindOption(CustomsTransportKind Value, string Label);
public sealed record CustomsCurrencyOption(AccountingCurrency Value, string Label);
public sealed record CustomsVehicleStatusOption(CustomsVehicleStatus Value, string Label);

public partial class CustomsInvoiceVehicleRow : ObservableObject
{
    public Action? PriceChanged { get; set; }

    [ObservableProperty] private int? _id;
    [ObservableProperty] private string _serialNumber = string.Empty;
    [ObservableProperty] private string _iraqiPlate = string.Empty;
    [ObservableProperty] private string _iraqiDriver = string.Empty;
    [ObservableProperty] private string _syrianPlate = string.Empty;
    [ObservableProperty] private string _syrianDriver = string.Empty;
    [ObservableProperty] private string _cargoType = string.Empty;
    [ObservableProperty] private DateTime? _loadingDate;
    [ObservableProperty] private DateTime? _exitDate;
    [ObservableProperty] private DateTime? _warehouseArrivalDate;
    [ObservableProperty] private DateTime? _unloadingDate;
    [ObservableProperty] private decimal _vehiclePrice;
    [ObservableProperty] private decimal _delayPenaltyAmount;
    [ObservableProperty] private CustomsVehicleStatus _status = CustomsVehicleStatus.InProgress;
    [ObservableProperty] private string _notes = string.Empty;

    public string StatusLabel => CustomsVehicleStatusDisplay.ToArabic(Status);

    partial void OnVehiclePriceChanged(decimal value) => PriceChanged?.Invoke();

    partial void OnDelayPenaltyAmountChanged(decimal value) => PriceChanged?.Invoke();

    partial void OnStatusChanged(CustomsVehicleStatus value) =>
        OnPropertyChanged(nameof(StatusLabel));

    public static CustomsInvoiceVehicleRow FromDraft(CustomsInvoiceVehicleDraft d) => new()
    {
        Id = d.Id,
        SerialNumber = d.SerialNumber,
        IraqiPlate = d.IraqiPlate,
        IraqiDriver = d.IraqiDriver,
        SyrianPlate = d.SyrianPlate,
        SyrianDriver = d.SyrianDriver,
        CargoType = d.CargoType,
        LoadingDate = d.LoadingDate,
        ExitDate = d.ExitDate,
        WarehouseArrivalDate = d.WarehouseArrivalDate,
        UnloadingDate = d.UnloadingDate,
        VehiclePrice = d.VehiclePrice,
        DelayPenaltyAmount = d.DelayPenaltyAmount,
        Status = d.Status,
        Notes = d.Notes
    };

    public CustomsInvoiceVehicleDraft ToDraft() => new()
    {
        Id = Id,
        SerialNumber = SerialNumber,
        IraqiPlate = IraqiPlate,
        IraqiDriver = IraqiDriver,
        SyrianPlate = SyrianPlate,
        SyrianDriver = SyrianDriver,
        CargoType = CargoType,
        LoadingDate = LoadingDate,
        ExitDate = ExitDate,
        WarehouseArrivalDate = WarehouseArrivalDate,
        UnloadingDate = UnloadingDate,
        VehiclePrice = VehiclePrice,
        DelayPenaltyAmount = DelayPenaltyAmount,
        Status = Status,
        Notes = Notes
    };
}

public partial class CustomsInvoiceExpenseRow : ObservableObject
{
    [ObservableProperty] private int? _id;
    [ObservableProperty] private int _expenseTypeId;
    [ObservableProperty] private string _expenseTypeName = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private AccountingCurrency _currency = AccountingCurrency.IQD;
    [ObservableProperty] private decimal _fxRate = 1m;
    [ObservableProperty] private DateTime _expenseDate = DateTime.Today;
    [ObservableProperty] private string _recipient = string.Empty;

    public string CurrencyLabel => Currency == AccountingCurrency.USD ? "$" : "د.ع";

    partial void OnCurrencyChanged(AccountingCurrency value) =>
        OnPropertyChanged(nameof(CurrencyLabel));

    public static CustomsInvoiceExpenseRow FromDraft(CustomsInvoiceExpenseDraft d) => new()
    {
        Id = d.Id,
        ExpenseTypeId = d.ExpenseTypeId,
        ExpenseTypeName = d.ExpenseTypeName,
        Description = d.Description,
        Amount = d.Amount,
        Currency = d.Currency,
        FxRate = d.FxRate > 0 ? d.FxRate : 1m,
        ExpenseDate = d.ExpenseDate,
        Recipient = d.Recipient
    };

    public CustomsInvoiceExpenseDraft ToDraft() => new()
    {
        Id = Id,
        ExpenseTypeId = ExpenseTypeId,
        ExpenseTypeName = ExpenseTypeName,
        Description = Description,
        Amount = Amount,
        Currency = Currency,
        FxRate = FxRate,
        ExpenseDate = ExpenseDate,
        Recipient = Recipient
    };
}
