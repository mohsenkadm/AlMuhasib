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

public partial class CustomsReceiptFormViewModel : ViewModelBase
{
    private readonly ICustomsReceiptService _receiptService;
    private readonly ICustomsCustomerService _customerService;
    private readonly ICustomsAttachmentService _attachmentService;
    private readonly ICustomsPrintService _printService;
    private readonly ICurrentUserService _currentUserService;
    private readonly MainWindowViewModel _mainWindow;
    private int? _editId;

    [ObservableProperty] private string _receiptNumber = "يُنشأ تلقائياً";
    [ObservableProperty] private DateTime _receiptDate = DateTime.Today;
    [ObservableProperty] private CustomsCustomerListItem? _selectedCustomer;
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private AccountingCurrency _currency = AccountingCurrency.IQD;
    [ObservableProperty] private decimal _fxRate = 1m;
    [ObservableProperty] private bool _showFxRate;
    [ObservableProperty] private CustomsPaymentMethod _paymentMethod = CustomsPaymentMethod.Cash;
    [ObservableProperty] private string _receiverName = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private decimal _allocatedSum;
    [ObservableProperty] private decimal _allocationDifference;
    [ObservableProperty] private bool _isAllocationBalanced;
    [ObservableProperty] private string _formError = string.Empty;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private bool _canCancelReceipt;

    public ObservableCollection<CustomsCustomerListItem> Customers { get; } = [];
    public ObservableCollection<CustomsReceiptAllocationRow> AllocationRows { get; } = [];
    public ObservableCollection<CustomsAttachmentItem> Attachments { get; } = [];

    public IReadOnlyList<CustomsCurrencyOption> CurrencyOptions { get; } =
    [
        new(AccountingCurrency.IQD, "دينار عراقي"),
        new(AccountingCurrency.USD, "دولار أمريكي")
    ];

    public IReadOnlyList<CustomsPaymentMethodOption> PaymentMethodOptions { get; } =
    [
        new(CustomsPaymentMethod.Cash, CustomsPaymentMethodDisplay.ToArabic(CustomsPaymentMethod.Cash)),
        new(CustomsPaymentMethod.Bank, CustomsPaymentMethodDisplay.ToArabic(CustomsPaymentMethod.Bank)),
        new(CustomsPaymentMethod.Other, CustomsPaymentMethodDisplay.ToArabic(CustomsPaymentMethod.Other))
    ];

    public bool IsEditMode => _editId.HasValue;
    public bool CanSave => (IsEditMode ? CanEdit : CanAdd);
    public bool HasSavedReceipt => _editId.HasValue;
    public bool CanManageAttachments => CanEdit;

    public CustomsReceiptFormViewModel(
        ICustomsReceiptService receiptService,
        ICustomsCustomerService customerService,
        ICustomsAttachmentService attachmentService,
        ICustomsPrintService printService,
        ICurrentUserService currentUserService,
        MainWindowViewModel mainWindow)
    {
        _receiptService = receiptService;
        _customerService = customerService;
        _attachmentService = attachmentService;
        _printService = printService;
        _currentUserService = currentUserService;
        _mainWindow = mainWindow;
        PageTitle = "وصل قبض";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CustomsPermissionRegistry.Receipts);
        await LoadCustomersAsync();

        var pendingId = CustomsReceiptNavigationBridge.TakePendingEditReceiptId();
        if (pendingId is int editId)
            await LoadReceiptAsync(editId);
        else
            await ResetForNewAsync();
    }

    private async Task LoadCustomersAsync()
    {
        var (customers, _) = await _customerService.GetPagedAsync(1, 500, activeOnly: true);
        Customers.Clear();
        foreach (var c in customers)
            Customers.Add(c);
    }

    private async Task ResetForNewAsync()
    {
        _editId = null;
        PageTitle = "وصل قبض جديد";
        ReceiptNumber = await _receiptService.PreviewNextReceiptNumberAsync();
        ReceiptDate = DateTime.Today;
        SelectedCustomer = null;
        Amount = 0;
        Currency = AccountingCurrency.IQD;
        FxRate = 1m;
        ShowFxRate = false;
        PaymentMethod = CustomsPaymentMethod.Cash;
        ReceiverName = string.Empty;
        Notes = string.Empty;
        FormError = string.Empty;
        CanCancelReceipt = false;
        AllocationRows.Clear();
        Attachments.Clear();
        RecalculateAllocationSum();
        NotifyFormState();
    }

    private async Task LoadReceiptAsync(int id)
    {
        try
        {
            var detail = await _receiptService.GetByIdAsync(id);
            if (detail is null)
            {
                BeautifulMessageDialog.ShowError("الوصل غير موجود");
                await ResetForNewAsync();
                return;
            }

            _editId = detail.Id;
            PageTitle = $"تعديل {detail.ReceiptNumber}";
            ReceiptNumber = detail.ReceiptNumber;
            ReceiptDate = detail.ReceiptDate;
            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == detail.CustomerId)
                ?? new CustomsCustomerListItem { Id = detail.CustomerId, Name = detail.CustomerName };
            if (SelectedCustomer is not null && Customers.All(c => c.Id != SelectedCustomer.Id))
                Customers.Insert(0, SelectedCustomer);

            Amount = detail.Amount;
            Currency = detail.Currency;
            FxRate = detail.FxRate;
            ShowFxRate = detail.Currency == AccountingCurrency.USD;
            PaymentMethod = detail.PaymentMethod;
            ReceiverName = detail.ReceiverName;
            Notes = detail.Notes;
            CanCancelReceipt = detail.CanCancel && CanDelete;
            FormError = string.Empty;

            await ReloadOpenInvoicesAsync(detail.Allocations);

            Attachments.Clear();
            foreach (var a in detail.Attachments)
                Attachments.Add(a);

            NotifyFormState();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    private async Task ReloadOpenInvoicesAsync(IReadOnlyList<CustomsReceiptAllocationDraft>? existing = null)
    {
        AllocationRows.Clear();
        if (SelectedCustomer is null) return;

        var open = await _receiptService.GetOpenInvoicesAsync(
            SelectedCustomer.Id, Currency, _editId);

        var existingByInvoice = existing?
            .Where(a => a.Amount > 0)
            .ToDictionary(a => a.InvoiceId, a => a)
            ?? new Dictionary<int, CustomsReceiptAllocationDraft>();

        foreach (var inv in open)
        {
            existingByInvoice.TryGetValue(inv.InvoiceId, out var prior);
            var row = new CustomsReceiptAllocationRow
            {
                AllocationId = prior?.Id,
                InvoiceId = inv.InvoiceId,
                InvoiceNumber = inv.InvoiceNumber,
                InvoiceDate = inv.InvoiceDate,
                TotalAmount = inv.TotalAmount,
                RemainingAmount = inv.RemainingAmount,
                AllocateAmount = prior?.Amount ?? 0m
            };
            row.AmountChanged = RecalculateAllocationSum;
            AllocationRows.Add(row);
        }

        // Keep any allocations whose invoice is no longer "open" (fully paid by this receipt only)
        if (existing is not null)
        {
            foreach (var prior in existing.Where(a => a.Amount > 0 && AllocationRows.All(r => r.InvoiceId != a.InvoiceId)))
            {
                var row = new CustomsReceiptAllocationRow
                {
                    AllocationId = prior.Id,
                    InvoiceId = prior.InvoiceId,
                    InvoiceNumber = prior.InvoiceNumber,
                    RemainingAmount = prior.Amount,
                    AllocateAmount = prior.Amount
                };
                row.AmountChanged = RecalculateAllocationSum;
                AllocationRows.Add(row);
            }
        }

        RecalculateAllocationSum();
    }

    partial void OnSelectedCustomerChanged(CustomsCustomerListItem? value) =>
        _ = ReloadOpenInvoicesAsync();

    partial void OnCurrencyChanged(AccountingCurrency value)
    {
        ShowFxRate = value == AccountingCurrency.USD;
        if (value == AccountingCurrency.IQD)
            FxRate = 1m;
        _ = ReloadOpenInvoicesAsync();
    }

    partial void OnAmountChanged(decimal value) => RecalculateAllocationSum();

    private void RecalculateAllocationSum()
    {
        AllocatedSum = AllocationRows.Sum(r => r.AllocateAmount);
        AllocationDifference = Amount - AllocatedSum;
        IsAllocationBalanced = Amount > 0 && AllocationDifference == 0 && AllocatedSum > 0;
    }

    private void NotifyFormState()
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(HasSavedReceipt));
        OnPropertyChanged(nameof(CanManageAttachments));
    }

    [RelayCommand]
    private void AutoAllocate()
    {
        if (Amount <= 0)
        {
            BeautifulMessageDialog.ShowWarning("أدخل مبلغ الوصل أولاً");
            return;
        }

        var remaining = Amount;
        foreach (var row in AllocationRows)
        {
            var take = Math.Min(row.RemainingAmount, remaining);
            row.AllocateAmount = take;
            remaining -= take;
        }

        RecalculateAllocationSum();
    }

    [RelayCommand]
    private void ClearAllocations()
    {
        foreach (var row in AllocationRows)
            row.AllocateAmount = 0;
        RecalculateAllocationSum();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        FormError = string.Empty;
        if (!CanSave)
        {
            FormError = "ليس لديك صلاحية الحفظ";
            return;
        }

        if (SelectedCustomer is null)
        {
            FormError = "اختر العميل";
            return;
        }

        if (!IsAllocationBalanced)
        {
            FormError = $"مجموع التوزيع ({AllocatedSum:N0}) يجب أن يساوي مبلغ الوصل ({Amount:N0})";
            return;
        }

        var request = new CustomsReceiptSaveRequest
        {
            CustomerId = SelectedCustomer.Id,
            ReceiptDate = ReceiptDate,
            Amount = Amount,
            Currency = Currency,
            FxRate = Currency == AccountingCurrency.USD ? FxRate : 1m,
            PaymentMethod = PaymentMethod,
            ReceiverName = ReceiverName,
            Notes = Notes,
            UserName = _currentUserService.Username,
            Allocations = AllocationRows
                .Where(r => r.AllocateAmount > 0)
                .Select(r => new CustomsReceiptAllocationDraft
                {
                    Id = r.AllocationId,
                    InvoiceId = r.InvoiceId,
                    InvoiceNumber = r.InvoiceNumber,
                    Amount = r.AllocateAmount
                })
                .ToList()
        };

        IsSaving = true;
        try
        {
            CustomsReceiptDetail saved;
            if (_editId.HasValue)
                saved = await _receiptService.UpdateAsync(_editId.Value, request);
            else
                saved = await _receiptService.CreateAsync(request);

            BeautifulMessageDialog.ShowSuccess("تم حفظ الوصل وتحديث المدفوع والرصيد");
            await LoadReceiptAsync(saved.Id);
        }
        catch (Exception ex)
        {
            FormError = ex.Message;
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsSaving = false;
            NotifyFormState();
        }
    }

    [RelayCommand]
    private async Task CancelReceiptAsync()
    {
        if (!_editId.HasValue || !CanCancelReceipt) return;
        if (!BeautifulMessageDialog.ShowConfirm("إلغاء هذا الوصل وعكس التوزيعات على الفواتير؟"))
            return;

        try
        {
            await _receiptService.CancelAsync(_editId.Value, _currentUserService.Username);
            BeautifulMessageDialog.ShowSuccess("تم إلغاء الوصل");
            _mainWindow.CloseTabForViewModel(this);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        if (!_editId.HasValue || !CanPrint) return;
        try
        {
            var detail = await _receiptService.GetByIdAsync(_editId.Value);
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
    private async Task ExportPdfAsync()
    {
        if (!_editId.HasValue || !CanExport) return;
        try
        {
            var detail = await _receiptService.GetByIdAsync(_editId.Value);
            if (detail is null) return;
            var temp = _printService.ExportReceiptPdf(detail);
            var dialog = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"{detail.ReceiptNumber}.pdf",
                DefaultExt = ".pdf"
            };
            if (dialog.ShowDialog() == true)
            {
                File.Copy(temp, dialog.FileName, overwrite: true);
                BeautifulMessageDialog.ShowSuccess("تم حفظ ملف PDF");
            }
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
            BeautifulMessageDialog.ShowWarning("احفظ الوصل أولاً قبل إرفاق الملفات");
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
                    CustomsAttachmentOwnerType.Receipt,
                    _editId.Value,
                    path,
                    null,
                    _currentUserService.Username);
            }

            var items = await _attachmentService.GetByOwnerAsync(
                CustomsAttachmentOwnerType.Receipt, _editId.Value);
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

public sealed record CustomsPaymentMethodOption(CustomsPaymentMethod Value, string Label);

public partial class CustomsReceiptAllocationRow : ObservableObject
{
    public Action? AmountChanged { get; set; }

    [ObservableProperty] private int? _allocationId;
    [ObservableProperty] private int _invoiceId;
    [ObservableProperty] private string _invoiceNumber = string.Empty;
    [ObservableProperty] private DateTime _invoiceDate;
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private decimal _remainingAmount;
    [ObservableProperty] private decimal _allocateAmount;

    partial void OnAllocateAmountChanged(decimal value) => AmountChanged?.Invoke();
}
