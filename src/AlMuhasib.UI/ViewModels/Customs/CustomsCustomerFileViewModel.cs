using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Customs;
using AlMuhasib.Core.Models.Customs;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels.Customs;

public partial class CustomsCustomerFileViewModel : ViewModelBase
{
    private readonly ICustomsCustomerService _customerService;
    private readonly ICustomsAttachmentService _attachmentService;
    private readonly ICurrentUserService _currentUserService;
    private readonly MainWindowViewModel _mainWindow;
    private int _customerId;

    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private bool _hasSelection;
    [ObservableProperty] private int _selectedTab;

    [ObservableProperty] private string _customerName = string.Empty;
    [ObservableProperty] private string _customerTypeLabel = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _companyName = string.Empty;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string _statusLabel = string.Empty;

    [ObservableProperty] private string _balanceIqdDisplay = "0";
    [ObservableProperty] private string _balanceUsdDisplay = "0";
    [ObservableProperty] private string _unpaidInvoiceCountDisplay = "0";
    [ObservableProperty] private string _vehicleCountDisplay = "0";

    [ObservableProperty] private string _debitIqdDisplay = "0";
    [ObservableProperty] private string _debitUsdDisplay = "0";
    [ObservableProperty] private string _creditIqdDisplay = "0";
    [ObservableProperty] private string _creditUsdDisplay = "0";

    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string _notesError = string.Empty;

    public ObservableCollection<CustomsCustomerInvoiceRow> Invoices { get; } = [];
    public ObservableCollection<CustomsCustomerVehicleRow> Vehicles { get; } = [];
    public ObservableCollection<CustomsCustomerReceiptRow> Receipts { get; } = [];
    public ObservableCollection<CustomsAttachmentItem> Attachments { get; } = [];

    public CustomsCustomerFileViewModel(
        ICustomsCustomerService customerService,
        ICustomsAttachmentService attachmentService,
        ICurrentUserService currentUserService,
        MainWindowViewModel mainWindow)
    {
        _customerService = customerService;
        _attachmentService = attachmentService;
        _currentUserService = currentUserService;
        _mainWindow = mainWindow;
        PageTitle = "ملف العميل";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CustomsPermissionRegistry.Customers);
        var pendingId = CustomsNavigationContext.TakePendingCustomerId();
        if (pendingId is null)
        {
            BeautifulMessageDialog.ShowWarning("لم يتم تحديد عميل لعرض الملف");
            return;
        }

        _customerId = pendingId.Value;
        await LoadFileAsync();
    }

    [RelayCommand]
    private async Task LoadFileAsync()
    {
        if (_customerId <= 0) return;
        IsBusy = true;
        try
        {
            var file = await _customerService.GetCustomerFileAsync(_customerId);
            if (file is null)
            {
                HasSelection = false;
                IsLoaded = true;
                BeautifulMessageDialog.ShowWarning("العميل غير موجود");
                return;
            }

            ApplyFile(file);
            HasSelection = true;
            IsLoaded = true;
        }
        catch (Exception ex)
        {
            IsLoaded = true;
            BeautifulMessageDialog.ShowError($"تعذر تحميل ملف العميل:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFile(CustomsCustomerFile file)
    {
        CustomerName = file.Name;
        CustomerTypeLabel = file.CustomerTypeLabel;
        Phone = string.IsNullOrWhiteSpace(file.Phone) ? "—" : file.Phone;
        Address = string.IsNullOrWhiteSpace(file.Address) ? "—" : file.Address;
        CompanyName = file.CompanyName;
        IsActive = file.IsActive;
        StatusLabel = file.IsActive ? "نشط" : "موقوف";
        Notes = file.Notes;
        NotesError = string.Empty;

        BalanceIqdDisplay = file.BalanceIqd.ToString("N0");
        BalanceUsdDisplay = file.BalanceUsd.ToString("N2");
        UnpaidInvoiceCountDisplay = file.UnpaidInvoiceCount.ToString("N0");
        VehicleCountDisplay = file.VehicleCount.ToString("N0");

        DebitIqdDisplay = file.DebitIqd.ToString("N0");
        DebitUsdDisplay = file.DebitUsd.ToString("N2");
        CreditIqdDisplay = file.CreditIqd.ToString("N0");
        CreditUsdDisplay = file.CreditUsd.ToString("N2");

        Invoices.Clear();
        foreach (var row in file.Invoices)
            Invoices.Add(row);

        Vehicles.Clear();
        foreach (var row in file.Vehicles)
            Vehicles.Add(row);

        Receipts.Clear();
        foreach (var row in file.Receipts)
            Receipts.Add(row);

        Attachments.Clear();
        foreach (var row in file.Attachments)
            Attachments.Add(row);
    }

    [RelayCommand]
    private async Task OpenInvoiceAsync(CustomsCustomerInvoiceRow? row)
    {
        if (row is null) return;
        CustomsInvoiceNavigationBridge.PendingEditInvoiceId = row.Id;
        await _mainWindow.OpenTabAsync(
            typeof(CustomsInvoiceFormViewModel),
            $"تعديل {row.InvoiceNumber}",
            PackIconKind.FileDocumentEdit,
            activateIfExists: false,
            permissionScreenName: CustomsPermissionRegistry.Invoices);
    }

    [RelayCommand]
    private async Task SaveNotesAsync()
    {
        if (_customerId <= 0 || !CanEdit) return;
        try
        {
            var entity = await _customerService.GetByIdAsync(_customerId);
            if (entity is null)
            {
                NotesError = "العميل غير موجود";
                return;
            }

            entity.Notes = Notes?.Trim() ?? string.Empty;
            entity.UpdatedBy = _currentUserService.Username;
            await _customerService.UpdateAsync(entity);
            NotesError = string.Empty;
            BeautifulMessageDialog.ShowSuccess("تم حفظ الملاحظات");
        }
        catch (Exception ex)
        {
            NotesError = ex.Message;
        }
    }

    [RelayCommand]
    private async Task AddAttachmentAsync()
    {
        if (_customerId <= 0 || !CanEdit) return;

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
                    CustomsAttachmentOwnerType.Customer,
                    _customerId,
                    path,
                    description: null,
                    _currentUserService.Username);
            }

            await ReloadAttachmentsAsync();
            BeautifulMessageDialog.ShowSuccess($"تم إرفاق {dialog.FileNames.Length} ملف");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"خطأ أثناء الإرفاق: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenAttachment(CustomsAttachmentItem? item)
    {
        if (item is null || !File.Exists(item.FilePath)) return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = item.FilePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"لا يمكن فتح الملف: {ex.Message}");
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
            await ReloadAttachmentsAsync();
            BeautifulMessageDialog.ShowSuccess("تم حذف المرفق");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    private async Task ReloadAttachmentsAsync()
    {
        var items = await _attachmentService.GetByOwnerAsync(
            CustomsAttachmentOwnerType.Customer, _customerId);
        Attachments.Clear();
        foreach (var item in items)
            Attachments.Add(item);
    }
}
