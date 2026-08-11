using System.Collections.ObjectModel;
using System.IO;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels;

public partial class OpeningCustomerBalanceViewModel : ViewModelBase
{
    private readonly IOpeningPartyBalanceService _balanceService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IOpeningCustomerBalanceExcelService _excelService;

    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<Customer> FilteredCustomers { get; } = [];
    public ObservableCollection<OpeningPartyBalanceImportRow> ImportRows { get; } = [];

    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private string _customerSearchText = string.Empty;
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private string _fileNumber = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private DateTime _balanceDate = DateTime.Today;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _importStatusMessage = string.Empty;
    [ObservableProperty] private bool _isQuickAddCustomerOpen;
    [ObservableProperty] private string _quickCustomerName = string.Empty;
    [ObservableProperty] private string _quickCustomerPhone = string.Empty;
    [ObservableProperty] private string _quickCustomerError = string.Empty;

    public int ImportValidCount => ImportRows.Count(r => r.IsValid);
    public int ImportInvalidCount => ImportRows.Count(r => !r.IsValid);
    public bool HasImportRows => ImportRows.Count > 0;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    public OpeningCustomerBalanceViewModel(
        IOpeningPartyBalanceService balanceService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IOpeningCustomerBalanceExcelService excelService)
    {
        _balanceService = balanceService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _excelService = excelService;
        PageTitle = "أرصدة العملاء الافتتاحية";
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "OpeningCustomerBalances");
            await LoadCustomersAsync();
        }
        finally { IsBusy = false; }
    }

    private async Task LoadCustomersAsync()
    {
        var customers = await _unitOfWork.Customers.GetAllAsync();
        Customers.Clear();
        FilteredCustomers.Clear();
        foreach (var c in customers.OrderBy(c => c.Name))
        {
            Customers.Add(c);
            FilteredCustomers.Add(c);
        }
    }

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        if (value is not null)
        {
            CustomerSearchText = value.Name;
            Phone = value.Phone ?? string.Empty;
            FileNumber = value.FileNumber ?? string.Empty;
        }
    }

    partial void OnCustomerSearchTextChanged(string value)
    {
        if (SelectedCustomer is not null && SelectedCustomer.Name == value)
            return;

        SelectedCustomer = null;
        FilteredCustomers.Clear();
        var term = value?.Trim() ?? string.Empty;
        var source = string.IsNullOrEmpty(term)
            ? Customers
            : Customers.Where(c => c.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                                   || (c.Phone?.Contains(term) ?? false)
                                   || (c.FileNumber?.Contains(term) ?? false));
        foreach (var c in source.Take(30))
            FilteredCustomers.Add(c);
    }

    [RelayCommand]
    private async Task SaveManualAsync()
    {
        ErrorMessage = string.Empty;

        if (SelectedCustomer is null && string.IsNullOrWhiteSpace(CustomerSearchText))
        {
            ErrorMessage = "يرجى اختيار عميل أو إدخال اسمه";
            return;
        }

        if (Amount <= 0)
        {
            ErrorMessage = "المبلغ يجب أن يكون أكبر من صفر";
            return;
        }

        IsBusy = true;
        try
        {
            var request = new OpeningPartyBalanceRequest
            {
                PartyId = SelectedCustomer?.Id,
                PartyName = SelectedCustomer?.Name ?? CustomerSearchText.Trim(),
                Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                FileNumber = string.IsNullOrWhiteSpace(FileNumber) ? null : FileNumber.Trim(),
                Amount = Amount,
                Date = BalanceDate.Date,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
            };

            await _balanceService.CreateCustomerOpeningBalanceAsync(request);
            BeautifulMessageDialog.ShowSuccess(
                $"تم حفظ الرصيد الافتتاحي الآجل للعميل «{request.PartyName}» بمبلغ {Amount:N0} د.ع");

            ResetManualForm();
            await LoadCustomersAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally { IsBusy = false; }
    }

    private void ResetManualForm()
    {
        SelectedCustomer = null;
        CustomerSearchText = string.Empty;
        FileNumber = string.Empty;
        Phone = string.Empty;
        Amount = 0;
        BalanceDate = DateTime.Today;
        Notes = string.Empty;
    }

    [RelayCommand]
    private void OpenQuickAddCustomer()
    {
        QuickCustomerName = CustomerSearchText.Trim();
        QuickCustomerPhone = string.Empty;
        QuickCustomerError = string.Empty;
        IsQuickAddCustomerOpen = true;
    }

    [RelayCommand]
    private void CancelQuickAddCustomer() => IsQuickAddCustomerOpen = false;

    [RelayCommand]
    private async Task SaveQuickCustomerAsync()
    {
        if (string.IsNullOrWhiteSpace(QuickCustomerName))
        {
            QuickCustomerError = "اسم العميل مطلوب";
            return;
        }

        try
        {
            var customer = new Customer
            {
                Name = QuickCustomerName.Trim(),
                Phone = string.IsNullOrWhiteSpace(QuickCustomerPhone) ? null : QuickCustomerPhone.Trim(),
                CreatedBy = _currentUserService.Username,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Customers.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();
            await LoadCustomersAsync();
            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == customer.Id);
            CustomerSearchText = customer.Name;
            IsQuickAddCustomerOpen = false;
            BeautifulMessageDialog.ShowSuccess($"تم إضافة العميل «{customer.Name}»");
        }
        catch (Exception ex)
        {
            QuickCustomerError = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DownloadTemplateAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Excel|*.xlsx",
            FileName = "قالب_أرصدة_العملاء_الافتتاحية.xlsx",
            Title = "حفظ قالب Excel"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var bytes = _excelService.GenerateTemplate();
            await File.WriteAllBytesAsync(dialog.FileName, bytes);
            BeautifulMessageDialog.ShowSuccess("تم تنزيل القالب بنجاح.\nاملأ ورقة «البيانات» ثم استورد الملف.");
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"خطأ في التنزيل: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task PickImportFileAsync()
    {
        ImportStatusMessage = string.Empty;
        var dialog = new OpenFileDialog
        {
            Filter = "Excel|*.xlsx;*.xls",
            Title = "اختر ملف Excel للاستيراد"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var rows = _excelService.ParseImportFile(dialog.FileName);
            ImportRows.Clear();
            foreach (var row in rows)
                ImportRows.Add(row);

            ImportStatusMessage = rows.Count == 0
                ? "الملف لا يحتوي على بيانات"
                : $"تم قراءة {rows.Count} سطر — صالح: {ImportValidCount} | يحتاج تصحيح: {ImportInvalidCount}";
            OnPropertyChanged(nameof(ImportValidCount));
            OnPropertyChanged(nameof(ImportInvalidCount));
            OnPropertyChanged(nameof(HasImportRows));
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"خطأ في قراءة الملف: {ex.Message}";
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        ImportStatusMessage = string.Empty;
        if (ImportRows.Count == 0)
        {
            ImportStatusMessage = "لا توجد بيانات للاستيراد. اختر ملف Excel أولاً.";
            return;
        }

        var validRows = ImportRows.Where(r => r.IsValid).ToList();
        if (validRows.Count == 0)
        {
            ImportStatusMessage = "لا توجد أسطر صالحة للاستيراد — راجع عمود الأخطاء";
            BeautifulMessageDialog.ShowWarning(ImportStatusMessage);
            return;
        }

        if (ImportInvalidCount > 0)
        {
            var proceed = BeautifulMessageDialog.ShowConfirm(
                $"يوجد {ImportInvalidCount} سطر يحتاج تصحيح.\nهل تريد استيراد الأسطر الصالحة فقط ({validRows.Count})؟");
            if (!proceed)
                return;
        }

        IsBusy = true;
        ImportStatusMessage = $"جاري الاستيراد... 0 / {validRows.Count}";
        try
        {
            await Task.Yield();

            var requests = validRows.Select(r => new OpeningPartyBalanceRequest
            {
                PartyName = r.PartyName,
                Phone = r.Phone,
                FileNumber = r.FileNumber,
                Amount = r.Amount,
                Date = r.Date,
                Notes = r.Notes
            }).ToList();

            ImportStatusMessage = $"جاري استيراد {requests.Count} سطر إلى النظام...";
            var result = await Task.Run(async () =>
                await _balanceService.CreateCustomerOpeningBalancesBatchAsync(requests).ConfigureAwait(false)).ConfigureAwait(true);

            if (result.SuccessCount > 0 && result.FailedCount == 0)
            {
                BeautifulMessageDialog.ShowSuccess($"تم استيراد {result.SuccessCount} رصيد افتتاحي بنجاح");
                ImportRows.Clear();
                ImportStatusMessage = $"اكتمل الاستيراد بنجاح ({result.SuccessCount} سطر)";
                await LoadCustomersAsync();
            }
            else if (result.SuccessCount > 0)
            {
                ImportStatusMessage = $"نجح {result.SuccessCount} | فشل {result.FailedCount}\n{string.Join("\n", result.Errors.Take(5))}";
                BeautifulMessageDialog.ShowWarning(ImportStatusMessage);
                await LoadCustomersAsync();
            }
            else
            {
                ImportStatusMessage = $"فشل الاستيراد:\n{string.Join("\n", result.Errors.Take(5))}";
                BeautifulMessageDialog.ShowError(ImportStatusMessage);
            }

            OnPropertyChanged(nameof(HasImportRows));
            OnPropertyChanged(nameof(ImportValidCount));
            OnPropertyChanged(nameof(ImportInvalidCount));
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"خطأ: {ex.Message}";
            BeautifulMessageDialog.ShowError(ImportStatusMessage);
        }
        finally { IsBusy = false; }
    }
}
