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
    private readonly ICurrentUserService _currentUserService;
    private readonly MainWindowViewModel _mainWindow;
    private int? _editId;

    [ObservableProperty] private string _invoiceNumber = "يُنشأ تلقائياً";
    [ObservableProperty] private DateTime _invoiceDate = DateTime.Today;
    [ObservableProperty] private CustomsCustomerListItem? _selectedCustomer;
    [ObservableProperty] private CustomsServiceType _serviceType = CustomsServiceType.Clearance;
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

    [ObservableProperty] private string _newExpenseTypeName = string.Empty;
    [ObservableProperty] private CustomsExpenseTypeItem? _selectedExpenseType;
    [ObservableProperty] private string _expenseDescription = string.Empty;
    [ObservableProperty] private decimal _expenseAmount;
    [ObservableProperty] private AccountingCurrency _expenseCurrency = AccountingCurrency.IQD;
    [ObservableProperty] private DateTime _expenseDate = DateTime.Today;
    [ObservableProperty] private string _expenseRecipient = string.Empty;

    public ObservableCollection<CustomsCustomerListItem> Customers { get; } = [];
    public ObservableCollection<CustomsInvoiceVehicleRow> Vehicles { get; } = [];
    public ObservableCollection<CustomsInvoiceExpenseRow> Expenses { get; } = [];
    public ObservableCollection<CustomsAttachmentItem> Attachments { get; } = [];
    public ObservableCollection<CustomsExpenseTypeItem> ExpenseTypes { get; } = [];

    public IReadOnlyList<CustomsServiceTypeOption> ServiceTypeOptions { get; } =
    [
        new(CustomsServiceType.Clearance, CustomsServiceTypeDisplay.ToArabic(CustomsServiceType.Clearance)),
        new(CustomsServiceType.Transport, CustomsServiceTypeDisplay.ToArabic(CustomsServiceType.Transport)),
        new(CustomsServiceType.ClearanceAndTransport, CustomsServiceTypeDisplay.ToArabic(CustomsServiceType.ClearanceAndTransport))
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

    public CustomsInvoiceFormViewModel(
        ICustomsInvoiceService invoiceService,
        ICustomsCustomerService customerService,
        ICustomsExpenseTypeService expenseTypeService,
        ICustomsAttachmentService attachmentService,
        ICurrentUserService currentUserService,
        MainWindowViewModel mainWindow)
    {
        _invoiceService = invoiceService;
        _customerService = customerService;
        _expenseTypeService = expenseTypeService;
        _attachmentService = attachmentService;
        _currentUserService = currentUserService;
        _mainWindow = mainWindow;
        PageTitle = "فاتورة تخليص";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CustomsPermissionRegistry.Invoices);
        await LoadLookupsAsync();

        var pendingId = CustomsInvoiceNavigationBridge.TakePendingEditInvoiceId();
        if (pendingId is int editId)
            await LoadInvoiceAsync(editId);
        else
            await ResetForNewAsync();
    }

    private async Task LoadLookupsAsync()
    {
        var (customers, _) = await _customerService.GetPagedAsync(1, 500, activeOnly: true);
        Customers.Clear();
        foreach (var c in customers)
            Customers.Add(c);

        var types = await _expenseTypeService.GetActiveAsync();
        ExpenseTypes.Clear();
        foreach (var t in types)
            ExpenseTypes.Add(t);
    }

    private async Task ResetForNewAsync()
    {
        _editId = null;
        PageTitle = "فاتورة جديدة";
        InvoiceNumber = await _invoiceService.PreviewNextInvoiceNumberAsync();
        InvoiceDate = DateTime.Today;
        SelectedCustomer = null;
        ServiceType = CustomsServiceType.Clearance;
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
        Expenses.Clear();
        Attachments.Clear();
        AddVehicleRow();
        RecalculateTotals();
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(HasSavedInvoice));
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
                Vehicles.Add(CustomsInvoiceVehicleRow.FromDraft(v));
            if (Vehicles.Count == 0)
                AddVehicleRow();

            Expenses.Clear();
            foreach (var e in detail.Expenses)
                Expenses.Add(CustomsInvoiceExpenseRow.FromDraft(e));

            Attachments.Clear();
            foreach (var a in detail.Attachments)
                Attachments.Add(a);

            RecalculateTotals();
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(HasSavedInvoice));
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

    partial void OnDiscountAmountChanged(decimal value) => RecalculateTotals();

    private void RecalculateTotals()
    {
        VehiclesSum = Vehicles.Sum(v => v.VehiclePrice);
        TotalAmount = Math.Max(0, VehiclesSum - DiscountAmount);
        RemainingAmount = Math.Max(0, TotalAmount - PaidAmount);
    }

    [RelayCommand]
    private void AddVehicleRow()
    {
        Vehicles.Add(new CustomsInvoiceVehicleRow { Status = CustomsVehicleStatus.InProgress });
        RecalculateTotals();
    }

    [RelayCommand]
    private void RemoveVehicleRow(CustomsInvoiceVehicleRow? row)
    {
        if (row is null) return;
        Vehicles.Remove(row);
        if (Vehicles.Count == 0)
            AddVehicleRow();
        RecalculateTotals();
    }

    [RelayCommand]
    private void VehiclePriceChanged() => RecalculateTotals();

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
    private void AddExpenseRow()
    {
        if (SelectedExpenseType is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر نوع المصروف");
            return;
        }

        Expenses.Add(new CustomsInvoiceExpenseRow
        {
            ExpenseTypeId = SelectedExpenseType.Id,
            ExpenseTypeName = SelectedExpenseType.Name,
            Description = ExpenseDescription?.Trim() ?? string.Empty,
            Amount = ExpenseAmount,
            Currency = ExpenseCurrency,
            ExpenseDate = ExpenseDate,
            Recipient = ExpenseRecipient?.Trim() ?? string.Empty
        });

        ExpenseDescription = string.Empty;
        ExpenseAmount = 0;
        ExpenseRecipient = string.Empty;
    }

    [RelayCommand]
    private void RemoveExpenseRow(CustomsInvoiceExpenseRow? row)
    {
        if (row is null) return;
        Expenses.Remove(row);
    }

    [RelayCommand]
    private async Task SaveAsync() => await SaveInternalAsync(approveAfterSave: false);

    [RelayCommand]
    private async Task SaveAndApproveAsync() => await SaveInternalAsync(approveAfterSave: true);

    private async Task SaveInternalAsync(bool approveAfterSave)
    {
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
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(HasSavedInvoice));
        }
    }

    [RelayCommand]
    private async Task ApproveAsync()
    {
        if (!_editId.HasValue || !CanApprove) return;
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
    private void Close() => _mainWindow.CloseTabForViewModel(this);
}

public sealed record CustomsServiceTypeOption(CustomsServiceType Value, string Label);
public sealed record CustomsCurrencyOption(AccountingCurrency Value, string Label);
public sealed record CustomsVehicleStatusOption(CustomsVehicleStatus Value, string Label);

public partial class CustomsInvoiceVehicleRow : ObservableObject
{
    [ObservableProperty] private int? _id;
    [ObservableProperty] private string _serialNumber = string.Empty;
    [ObservableProperty] private string _iraqiPlate = string.Empty;
    [ObservableProperty] private string _iraqiDriver = string.Empty;
    [ObservableProperty] private string _syrianPlate = string.Empty;
    [ObservableProperty] private string _syrianDriver = string.Empty;
    [ObservableProperty] private string _cargoType = string.Empty;
    [ObservableProperty] private DateTime? _loadingDate;
    [ObservableProperty] private DateTime? _exitDate;
    [ObservableProperty] private decimal _vehiclePrice;
    [ObservableProperty] private CustomsVehicleStatus _status = CustomsVehicleStatus.InProgress;
    [ObservableProperty] private string _notes = string.Empty;

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
        VehiclePrice = d.VehiclePrice,
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
        VehiclePrice = VehiclePrice,
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
    [ObservableProperty] private DateTime _expenseDate = DateTime.Today;
    [ObservableProperty] private string _recipient = string.Empty;

    public string CurrencyLabel => Currency == AccountingCurrency.USD ? "$" : "د.ع";

    public static CustomsInvoiceExpenseRow FromDraft(CustomsInvoiceExpenseDraft d) => new()
    {
        Id = d.Id,
        ExpenseTypeId = d.ExpenseTypeId,
        ExpenseTypeName = d.ExpenseTypeName,
        Description = d.Description,
        Amount = d.Amount,
        Currency = d.Currency,
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
        ExpenseDate = ExpenseDate,
        Recipient = Recipient
    };
}
