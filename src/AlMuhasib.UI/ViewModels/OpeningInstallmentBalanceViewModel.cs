using System.Collections.ObjectModel;
using System.IO;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels;

public partial class OpeningInstallmentBalanceViewModel : ViewModelBase
{
    private readonly IInstallmentService _installmentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IOpeningInstallmentExcelService _excelService;

    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<Customer> FilteredCustomers { get; } = [];
    public ObservableCollection<OpeningInstallmentPreviewRow> SchedulePreview { get; } = [];
    public ObservableCollection<OpeningInstallmentImportRow> ImportRows { get; } = [];

    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private string _customerSearchText = string.Empty;
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private string _fileNumber = string.Empty;
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private int _numberOfInstallments = 6;
    [ObservableProperty] private int _paidInstallmentsCount;
    [ObservableProperty] private DateTime _startDate = DateTime.Today.AddMonths(-3);
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _importStatusMessage = string.Empty;
    [ObservableProperty] private bool _isQuickAddCustomerOpen;
    [ObservableProperty] private string _quickCustomerName = string.Empty;
    [ObservableProperty] private string _quickCustomerPhone = string.Empty;
    [ObservableProperty] private string _quickCustomerError = string.Empty;

    public decimal RemainingAmount => Math.Max(0, TotalAmount - PaidPreviewTotal);
    public decimal PaidPreviewTotal => SchedulePreview.Where(r => r.IsPaid).Sum(r => r.Amount);
    public decimal UnpaidPreviewTotal => SchedulePreview.Where(r => !r.IsPaid).Sum(r => r.Amount);
    public int ImportValidCount => ImportRows.Count(r => r.IsValid);
    public int ImportInvalidCount => ImportRows.Count(r => !r.IsValid);
    public bool HasImportRows => ImportRows.Count > 0;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    public OpeningInstallmentBalanceViewModel(
        IInstallmentService installmentService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IOpeningInstallmentExcelService excelService)
    {
        _installmentService = installmentService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _excelService = excelService;
        PageTitle = "أرصدة الأقساط الافتتاحية";
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "OpeningInstallments");
            await LoadCustomersAsync();
            GenerateSchedulePreview();
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
            CustomerSearchText = value.Name;
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

    partial void OnTotalAmountChanged(decimal value) => GenerateSchedulePreview();
    partial void OnNumberOfInstallmentsChanged(int value) => GenerateSchedulePreview();
    partial void OnPaidInstallmentsCountChanged(int value) => GenerateSchedulePreview();
    partial void OnStartDateChanged(DateTime value) => GenerateSchedulePreview();

    private void GenerateSchedulePreview()
    {
        SchedulePreview.Clear();
        if (NumberOfInstallments <= 0 || TotalAmount <= 0)
        {
            NotifySummaryChanged();
            return;
        }

        var paidCount = Math.Clamp(PaidInstallmentsCount, 0, NumberOfInstallments);
        if (PaidInstallmentsCount != paidCount)
            PaidInstallmentsCount = paidCount;

        var perInstallment = Math.Floor(TotalAmount / NumberOfInstallments);
        for (var i = 0; i < NumberOfInstallments; i++)
        {
            var amount = i < NumberOfInstallments - 1
                ? perInstallment
                : TotalAmount - (perInstallment * (NumberOfInstallments - 1));

            SchedulePreview.Add(new OpeningInstallmentPreviewRow
            {
                Number = i + 1,
                DueDate = StartDate.Date.AddMonths(i),
                Amount = amount,
                IsPaid = i < paidCount
            });
        }

        NotifySummaryChanged();
    }

    private void NotifySummaryChanged()
    {
        OnPropertyChanged(nameof(PaidPreviewTotal));
        OnPropertyChanged(nameof(UnpaidPreviewTotal));
        OnPropertyChanged(nameof(RemainingAmount));
    }

    [RelayCommand]
    private async Task SaveManualAsync()
    {
        ErrorMessage = string.Empty;

        if (SelectedCustomer is null && string.IsNullOrWhiteSpace(CustomerSearchText))
        {
            ErrorMessage = "يرجى اختيار زبون أو إدخال اسمه";
            return;
        }

        if (TotalAmount <= 0)
        {
            ErrorMessage = "المبلغ الكلي يجب أن يكون أكبر من صفر";
            return;
        }

        if (NumberOfInstallments <= 0)
        {
            ErrorMessage = "عدد الأقساط يجب أن يكون أكبر من صفر";
            return;
        }

        if (PaidInstallmentsCount < 0 || PaidInstallmentsCount > NumberOfInstallments)
        {
            ErrorMessage = "عدد الأقساط المسددة غير صحيح";
            return;
        }

        if (SchedulePreview.Count == 0)
        {
            ErrorMessage = "لا يمكن إنشاء جدول الأقساط — تحقق من المبالغ";
            return;
        }

        IsBusy = true;
        try
        {
            var request = BuildRequest(
                SelectedCustomer?.Id,
                SelectedCustomer?.Name ?? CustomerSearchText.Trim());

            await _installmentService.CreateOpeningBalancePlanAsync(request);
            BeautifulMessageDialog.ShowSuccess(
                $"تم حفظ رصيد الأقساط الافتتاحي للزبون «{request.CustomerName ?? SelectedCustomer?.Name}» بنجاح.\n" +
                $"المسدد سابقاً: {PaidPreviewTotal:N0} د.ع | المتبقي: {UnpaidPreviewTotal:N0} د.ع");

            ResetManualForm();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally { IsBusy = false; }
    }

    private OpeningInstallmentBalanceRequest BuildRequest(int? customerId, string? customerName) => new()
    {
        CustomerId = customerId,
        CustomerName = customerId is null ? customerName : null,
        FileNumber = string.IsNullOrWhiteSpace(FileNumber) ? null : FileNumber.Trim(),
        TotalAmount = TotalAmount,
        NumberOfInstallments = NumberOfInstallments,
        PaidInstallmentsCount = PaidInstallmentsCount,
        StartDate = StartDate.Date,
        Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
    };

    private void ResetManualForm()
    {
        SelectedCustomer = null;
        CustomerSearchText = string.Empty;
        FileNumber = string.Empty;
        TotalAmount = 0;
        NumberOfInstallments = 6;
        PaidInstallmentsCount = 0;
        StartDate = DateTime.Today.AddMonths(-3);
        Notes = string.Empty;
        GenerateSchedulePreview();
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
            QuickCustomerError = "اسم الزبون مطلوب";
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
            BeautifulMessageDialog.ShowSuccess($"تم إضافة الزبون «{customer.Name}»");
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
            FileName = "قالب_أرصدة_الاقساط_الافتتاحية.xlsx",
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
            {
                EnsurePaidZeroIsAllowed(row);
                ImportRows.Add(row);
            }

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
            // إتاحة تحديث الواجهة لعرض مؤشر التحميل
            await Task.Yield();

            var requests = validRows.Select(r => new OpeningInstallmentBalanceRequest
            {
                CustomerName = r.CustomerName,
                FileNumber = r.FileNumber,
                TotalAmount = r.TotalAmount,
                NumberOfInstallments = r.NumberOfInstallments,
                PaidInstallmentsCount = r.PaidInstallmentsCount,
                StartDate = r.StartDate,
                Notes = r.Notes
            }).ToList();

            ImportStatusMessage = $"جاري استيراد {requests.Count} سطر إلى النظام...";
            var result = await Task.Run(async () =>
                await _installmentService.CreateOpeningBalancePlansBatchAsync(requests).ConfigureAwait(false)).ConfigureAwait(true);

            if (result.SuccessCount > 0 && result.FailedCount == 0)
            {
                BeautifulMessageDialog.ShowSuccess($"تم استيراد {result.SuccessCount} رصيد افتتاحي بنجاح");
                ImportRows.Clear();
                ImportStatusMessage = $"اكتمل الاستيراد بنجاح ({result.SuccessCount} سطر)";
            }
            else if (result.SuccessCount > 0)
            {
                ImportStatusMessage = $"نجح {result.SuccessCount} | فشل {result.FailedCount}\n{string.Join("\n", result.Errors.Take(5))}";
                BeautifulMessageDialog.ShowWarning(ImportStatusMessage);
            }
            else
            {
                ImportStatusMessage = string.Join("\n", result.Errors.Take(8));
                BeautifulMessageDialog.ShowError(string.IsNullOrWhiteSpace(ImportStatusMessage)
                    ? "فشل الاستيراد"
                    : ImportStatusMessage);
            }

            OnPropertyChanged(nameof(ImportValidCount));
            OnPropertyChanged(nameof(ImportInvalidCount));
            OnPropertyChanged(nameof(HasImportRows));
        }
        catch (Exception ex)
        {
            ImportStatusMessage = ex.Message;
            BeautifulMessageDialog.ShowError($"فشل الاستيراد: {ex.Message}");
        }
        finally { IsBusy = false; }
    }

    /// <summary>
    /// المسدد = 0 عقد جديد ويجب أن يُستورد — أزل أخطاء المسدد الزائفة إن وُجدت.
    /// </summary>
    private static void EnsurePaidZeroIsAllowed(OpeningInstallmentImportRow row)
    {
        if (row.PaidInstallmentsCount < 0)
            row.PaidInstallmentsCount = 0;

        row.Errors.RemoveAll(e =>
            e.Contains("عدد_الاقساط_المسددة", StringComparison.Ordinal)
            || e.Contains("عدد الأقساط المسددة غير صالح", StringComparison.Ordinal));

        if (row.NumberOfInstallments > 0 && row.PaidInstallmentsCount > row.NumberOfInstallments)
        {
            var msg = "عدد الأقساط المسددة أكبر من إجمالي الأقساط";
            if (!row.Errors.Contains(msg))
                row.Errors.Add(msg);
        }
    }
}
