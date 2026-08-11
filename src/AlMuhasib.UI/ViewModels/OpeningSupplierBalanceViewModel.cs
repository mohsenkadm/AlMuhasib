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

public partial class OpeningSupplierBalanceViewModel : ViewModelBase
{
    private readonly IOpeningPartyBalanceService _balanceService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IOpeningSupplierBalanceExcelService _excelService;

    public ObservableCollection<Supplier> Suppliers { get; } = [];
    public ObservableCollection<Supplier> FilteredSuppliers { get; } = [];
    public ObservableCollection<OpeningPartyBalanceImportRow> ImportRows { get; } = [];

    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private string _supplierSearchText = string.Empty;
    [ObservableProperty] private Supplier? _selectedSupplier;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private DateTime _balanceDate = DateTime.Today;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _importStatusMessage = string.Empty;
    [ObservableProperty] private bool _isQuickAddSupplierOpen;
    [ObservableProperty] private string _quickSupplierName = string.Empty;
    [ObservableProperty] private string _quickSupplierPhone = string.Empty;
    [ObservableProperty] private string _quickSupplierError = string.Empty;

    public int ImportValidCount => ImportRows.Count(r => r.IsValid);
    public int ImportInvalidCount => ImportRows.Count(r => !r.IsValid);
    public bool HasImportRows => ImportRows.Count > 0;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    public OpeningSupplierBalanceViewModel(
        IOpeningPartyBalanceService balanceService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IOpeningSupplierBalanceExcelService excelService)
    {
        _balanceService = balanceService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _excelService = excelService;
        PageTitle = "أرصدة الموردين الافتتاحية";
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "OpeningSupplierBalances");
            await LoadSuppliersAsync();
        }
        finally { IsBusy = false; }
    }

    private async Task LoadSuppliersAsync()
    {
        var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
        Suppliers.Clear();
        FilteredSuppliers.Clear();
        foreach (var s in suppliers.OrderBy(s => s.Name))
        {
            Suppliers.Add(s);
            FilteredSuppliers.Add(s);
        }
    }

    partial void OnSelectedSupplierChanged(Supplier? value)
    {
        if (value is not null)
        {
            SupplierSearchText = value.Name;
            Phone = value.Phone ?? string.Empty;
        }
    }

    partial void OnSupplierSearchTextChanged(string value)
    {
        if (SelectedSupplier is not null && SelectedSupplier.Name == value)
            return;

        SelectedSupplier = null;
        FilteredSuppliers.Clear();
        var term = value?.Trim() ?? string.Empty;
        var source = string.IsNullOrEmpty(term)
            ? Suppliers
            : Suppliers.Where(s => s.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                                   || (s.Phone?.Contains(term) ?? false));
        foreach (var s in source.Take(30))
            FilteredSuppliers.Add(s);
    }

    [RelayCommand]
    private async Task SaveManualAsync()
    {
        ErrorMessage = string.Empty;

        if (SelectedSupplier is null && string.IsNullOrWhiteSpace(SupplierSearchText))
        {
            ErrorMessage = "يرجى اختيار مورد أو إدخال اسمه";
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
                PartyId = SelectedSupplier?.Id,
                PartyName = SelectedSupplier?.Name ?? SupplierSearchText.Trim(),
                Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                Amount = Amount,
                Date = BalanceDate.Date,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
            };

            await _balanceService.CreateSupplierOpeningBalanceAsync(request);
            BeautifulMessageDialog.ShowSuccess(
                $"تم حفظ الرصيد الافتتاحي الآجل للمورد «{request.PartyName}» بمبلغ {Amount:N0} د.ع");

            ResetManualForm();
            await LoadSuppliersAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally { IsBusy = false; }
    }

    private void ResetManualForm()
    {
        SelectedSupplier = null;
        SupplierSearchText = string.Empty;
        Phone = string.Empty;
        Amount = 0;
        BalanceDate = DateTime.Today;
        Notes = string.Empty;
    }

    [RelayCommand]
    private void OpenQuickAddSupplier()
    {
        QuickSupplierName = SupplierSearchText.Trim();
        QuickSupplierPhone = string.Empty;
        QuickSupplierError = string.Empty;
        IsQuickAddSupplierOpen = true;
    }

    [RelayCommand]
    private void CancelQuickAddSupplier() => IsQuickAddSupplierOpen = false;

    [RelayCommand]
    private async Task SaveQuickSupplierAsync()
    {
        if (string.IsNullOrWhiteSpace(QuickSupplierName))
        {
            QuickSupplierError = "اسم المورد مطلوب";
            return;
        }

        try
        {
            var supplier = new Supplier
            {
                Name = QuickSupplierName.Trim(),
                Phone = string.IsNullOrWhiteSpace(QuickSupplierPhone) ? null : QuickSupplierPhone.Trim(),
                CreatedBy = _currentUserService.Username,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Suppliers.AddAsync(supplier);
            await _unitOfWork.SaveChangesAsync();
            await LoadSuppliersAsync();
            SelectedSupplier = Suppliers.FirstOrDefault(s => s.Id == supplier.Id);
            SupplierSearchText = supplier.Name;
            IsQuickAddSupplierOpen = false;
            BeautifulMessageDialog.ShowSuccess($"تم إضافة المورد «{supplier.Name}»");
        }
        catch (Exception ex)
        {
            QuickSupplierError = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DownloadTemplateAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Excel|*.xlsx",
            FileName = "قالب_أرصدة_الموردين_الافتتاحية.xlsx",
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
                Amount = r.Amount,
                Date = r.Date,
                Notes = r.Notes
            }).ToList();

            ImportStatusMessage = $"جاري استيراد {requests.Count} سطر إلى النظام...";
            var result = await Task.Run(async () =>
                await _balanceService.CreateSupplierOpeningBalancesBatchAsync(requests).ConfigureAwait(false)).ConfigureAwait(true);

            if (result.SuccessCount > 0 && result.FailedCount == 0)
            {
                BeautifulMessageDialog.ShowSuccess($"تم استيراد {result.SuccessCount} رصيد افتتاحي بنجاح");
                ImportRows.Clear();
                ImportStatusMessage = $"اكتمل الاستيراد بنجاح ({result.SuccessCount} سطر)";
                await LoadSuppliersAsync();
            }
            else if (result.SuccessCount > 0)
            {
                ImportStatusMessage = $"نجح {result.SuccessCount} | فشل {result.FailedCount}\n{string.Join("\n", result.Errors.Take(5))}";
                BeautifulMessageDialog.ShowWarning(ImportStatusMessage);
                await LoadSuppliersAsync();
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
