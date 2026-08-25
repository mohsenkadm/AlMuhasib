using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
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
    private readonly IExportService _exportService;
    private System.Timers.Timer? _debounceTimer;
    private int? _editingInvoiceId;

    public ObservableCollection<OpeningPartyBalanceListItem> Items { get; } = [];
    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<Customer> FilteredCustomers { get; } = [];
    public ObservableCollection<OpeningPartyBalanceImportRow> ImportRows { get; } = [];

    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private DateTime? _filterFromDate;
    [ObservableProperty] private DateTime? _filterToDate;
    [ObservableProperty] private string _filterMinAmountText = string.Empty;
    [ObservableProperty] private string _filterMaxAmountText = string.Empty;
    [ObservableProperty] private bool _unpaidOnly;
    [ObservableProperty] private bool _isAdvancedFilterOpen;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private OpeningPartyBalanceListItem? _selectedItem;

    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private string _customerSearchText = string.Empty;
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private string _fileNumber = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private DateTime _balanceDate = DateTime.Today;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string _dialogError = string.Empty;

    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private OpeningPartyBalanceListItem? _itemToDelete;

    [ObservableProperty] private string _importStatusMessage = string.Empty;
    [ObservableProperty] private bool _isQuickAddCustomerOpen;
    [ObservableProperty] private string _quickCustomerName = string.Empty;
    [ObservableProperty] private string _quickCustomerPhone = string.Empty;
    [ObservableProperty] private string _quickCustomerError = string.Empty;

    public int ImportValidCount => ImportRows.Count(r => r.IsValid);
    public int ImportInvalidCount => ImportRows.Count(r => !r.IsValid);
    public bool HasImportRows => ImportRows.Count > 0;
    public bool CanSaveBalance => CanAdd || CanEdit;
    public string ActiveFiltersSummary
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(SearchText)) parts.Add("بحث");
            if (FilterFromDate.HasValue || FilterToDate.HasValue) parts.Add("تاريخ");
            if (!string.IsNullOrWhiteSpace(FilterMinAmountText) || !string.IsNullOrWhiteSpace(FilterMaxAmountText))
                parts.Add("مبلغ");
            if (UnpaidOnly) parts.Add("غير مسدد");
            if (ActiveColumnFilterCount > 0) parts.Add($"أعمدة ({ActiveColumnFilterCount})");
            return parts.Count == 0 ? "بدون فلاتر نشطة" : string.Join(" · ", parts);
        }
    }

    public OpeningCustomerBalanceViewModel(
        IOpeningPartyBalanceService balanceService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IOpeningCustomerBalanceExcelService excelService,
        IExportService exportService)
    {
        _balanceService = balanceService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _excelService = excelService;
        _exportService = exportService;
        PageTitle = "أرصدة العملاء الافتتاحية";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "OpeningCustomerBalances");
        OnPropertyChanged(nameof(CanSaveBalance));
        await LoadCustomersAsync();
        await LoadItemsAsync();
    }

    private OpeningPartyBalanceQuery BuildQuery(int page, int pageSize)
    {
        decimal? minAmount = null;
        decimal? maxAmount = null;
        if (decimal.TryParse(FilterMinAmountText, out var min)) minAmount = min;
        if (decimal.TryParse(FilterMaxAmountText, out var max)) maxAmount = max;

        return new OpeningPartyBalanceQuery
        {
            Search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
            FromDate = FilterFromDate?.Date,
            ToDate = FilterToDate?.Date,
            MinAmount = minAmount,
            MaxAmount = maxAmount,
            UnpaidOnly = UnpaidOnly,
            Page = page,
            PageSize = pageSize
        };
    }

    private async Task LoadItemsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            {
                var allResult = await _balanceService.GetCustomerOpeningBalancesAsync(BuildQuery(1, int.MaxValue));
                var filtered = ColumnFilterEngine.Apply(allResult.Items, ColumnFilters).ToList();
                MasterDataColumnFilterHelper.ApplyClientPagination(
                    filtered, Items, CurrentPage, PageSize,
                    out var filteredTotal, out var filteredPages, out var filteredText);
                TotalCount = filteredTotal;
                TotalPages = filteredPages;
                PaginationText = filteredText;
            }
            else
            {
                var result = await _balanceService.GetCustomerOpeningBalancesAsync(BuildQuery(CurrentPage, PageSize));
                TotalCount = result.TotalCount;
                TotalPages = PaginationHelper.ComputeTotalPages(result.TotalCount, PageSize);
                PaginationText = PaginationHelper.BuildPaginationText(result.TotalCount, CurrentPage, PageSize);

                Items.Clear();
                foreach (var item in result.Items)
                    Items.Add(item);
            }

            OnPropertyChanged(nameof(ActiveFiltersSummary));
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر تحميل الأرصدة الافتتاحية:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
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

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        OnPropertyChanged(nameof(ActiveFiltersSummary));
        _ = LoadItemsAsync();
    }

    partial void OnSearchTextChanged(string value) => DebounceReload();
    partial void OnFilterFromDateChanged(DateTime? value) => QueueReload();
    partial void OnFilterToDateChanged(DateTime? value) => QueueReload();
    partial void OnUnpaidOnlyChanged(bool value) => QueueReload();

    private void DebounceReload()
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(400);
        _debounceTimer.Elapsed += (_, _) =>
        {
            _debounceTimer?.Stop();
            Application.Current.Dispatcher.InvokeAsync(() => QueueReload());
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    private void QueueReload()
    {
        CurrentPage = 1;
        OnPropertyChanged(nameof(ActiveFiltersSummary));
        _ = LoadItemsAsync();
    }

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        if (value is null || IsEditMode) return;
        CustomerSearchText = value.Name;
        Phone = value.Phone ?? string.Empty;
        FileNumber = value.FileNumber ?? string.Empty;
    }

    partial void OnCustomerSearchTextChanged(string value)
    {
        if (IsEditMode) return;
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
    private async Task FirstPage() { CurrentPage = 1; await LoadItemsAsync(); }

    [RelayCommand]
    private async Task PreviousPage() { if (CurrentPage > 1) { CurrentPage--; await LoadItemsAsync(); } }

    [RelayCommand]
    private async Task NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; await LoadItemsAsync(); } }

    [RelayCommand]
    private async Task LastPage() { CurrentPage = TotalPages; await LoadItemsAsync(); }

    [RelayCommand]
    private async Task Refresh()
    {
        CurrentPage = 1;
        await LoadItemsAsync();
    }

    [RelayCommand]
    private void ApplyAmountFilters() => QueueReload();

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        FilterFromDate = null;
        FilterToDate = null;
        FilterMinAmountText = string.Empty;
        FilterMaxAmountText = string.Empty;
        UnpaidOnly = false;
        ClearColumnFiltersCommand.Execute(null);
        QueueReload();
    }

    [RelayCommand]
    private void ToggleAdvancedFilter() => IsAdvancedFilterOpen = !IsAdvancedFilterOpen;

    [RelayCommand]
    private async Task OpenAddDialog()
    {
        if (!CanAdd)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية إضافة رصيد افتتاحي");
            return;
        }

        _editingInvoiceId = null;
        IsEditMode = false;
        DialogTitle = "إضافة رصيد افتتاحي لعميل";
        ResetDialogForm();
        DialogError = string.Empty;
        await LoadCustomersAsync();
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(OpeningPartyBalanceListItem? item)
    {
        if (item is null) return;
        if (!CanEdit)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية تعديل الرصيد الافتتاحي");
            return;
        }

        if (!item.CanModify)
        {
            BeautifulMessageDialog.ShowWarning("لا يمكن تعديل رصيد تم تسديد جزء منه.");
            return;
        }

        _editingInvoiceId = item.InvoiceId;
        IsEditMode = true;
        DialogTitle = $"تعديل رصيد — {item.PartyName}";
        CustomerSearchText = item.PartyName;
        SelectedCustomer = Customers.FirstOrDefault(c => c.Id == item.PartyId);
        Phone = item.Phone ?? string.Empty;
        FileNumber = item.FileNumber ?? string.Empty;
        Amount = item.Amount;
        BalanceDate = item.Date;
        Notes = item.UserNotes;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private async Task SaveBalanceAsync()
    {
        DialogError = string.Empty;

        if (IsEditMode)
        {
            if (!CanEdit)
            {
                DialogError = "ليس لديك صلاحية التعديل";
                return;
            }

            if (_editingInvoiceId is null)
            {
                DialogError = "تعذر تحديد الرصيد للتعديل";
                return;
            }

            if (Amount <= 0)
            {
                DialogError = "المبلغ يجب أن يكون أكبر من صفر";
                return;
            }

            try
            {
                IsBusy = true;
                await _balanceService.UpdateCustomerOpeningBalanceAsync(new OpeningPartyBalanceUpdateRequest
                {
                    InvoiceId = _editingInvoiceId.Value,
                    Amount = Amount,
                    Date = BalanceDate.Date,
                    Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
                });

                IsDialogOpen = false;
                BeautifulMessageDialog.ShowSuccess("تم تعديل الرصيد الافتتاحي");
                await LoadItemsAsync();
            }
            catch (Exception ex)
            {
                DialogError = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }

            return;
        }

        if (!CanAdd)
        {
            DialogError = "ليس لديك صلاحية الإضافة";
            return;
        }

        if (SelectedCustomer is null && string.IsNullOrWhiteSpace(CustomerSearchText))
        {
            DialogError = "يرجى اختيار عميل أو إدخال اسمه";
            return;
        }

        if (Amount <= 0)
        {
            DialogError = "المبلغ يجب أن يكون أكبر من صفر";
            return;
        }

        try
        {
            IsBusy = true;
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
            IsDialogOpen = false;
            BeautifulMessageDialog.ShowSuccess(
                $"تم حفظ الرصيد الافتتاحي للعميل «{request.PartyName}» بمبلغ {Amount:N0} د.ع");
            await LoadCustomersAsync();
            await LoadItemsAsync();
        }
        catch (Exception ex)
        {
            DialogError = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetDialogForm()
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
    private void ConfirmDelete(OpeningPartyBalanceListItem? item)
    {
        if (item is null || !CanDelete) return;
        if (!item.CanModify)
        {
            BeautifulMessageDialog.ShowWarning("لا يمكن حذف رصيد تم تسديد جزء منه.");
            return;
        }

        ItemToDelete = item;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteDialogOpen = false;
        ItemToDelete = null;
    }

    [RelayCommand]
    private async Task ExecuteDeleteAsync()
    {
        if (ItemToDelete is null || !CanDelete) return;
        try
        {
            IsBusy = true;
            await _balanceService.DeleteCustomerOpeningBalanceAsync(ItemToDelete.InvoiceId);
            IsDeleteDialogOpen = false;
            ItemToDelete = null;
            BeautifulMessageDialog.ShowSuccess("تم حذف الرصيد الافتتاحي");
            await LoadItemsAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
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
    private async Task ExportToExcel()
    {
        try
        {
            var allResult = await _balanceService.GetCustomerOpeningBalancesAsync(BuildQuery(1, int.MaxValue));
            var rows = MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters)
                ? ColumnFilterEngine.Apply(allResult.Items, ColumnFilters).ToList()
                : allResult.Items.ToList();

            var exportData = rows.Select(r => new
            {
                رقم_الفاتورة = r.InvoiceNumber,
                العميل = r.PartyName,
                الهاتف = r.Phone,
                رقم_الملف = r.FileNumber,
                المبلغ = r.Amount,
                المسدد = r.PaidAmount,
                المتبقي = r.RemainingAmount,
                التاريخ = r.Date.ToString("yyyy/MM/dd"),
                ملاحظات = r.UserNotes
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"ارصدة_افتتاحية_عملاء_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "أرصدة العملاء");
                BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
            }
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
            var allResult = await _balanceService.GetCustomerOpeningBalancesAsync(BuildQuery(1, int.MaxValue));
            var rowsData = MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters)
                ? ColumnFilterEngine.Apply(allResult.Items, ColumnFilters).ToList()
                : allResult.Items.ToList();

            var columns = new[] { "رقم", "العميل", "الهاتف", "المبلغ", "المتبقي", "التاريخ", "ملاحظات" };
            IList<object[]> rows = rowsData.Select(r => new object[]
            {
                r.InvoiceNumber,
                r.PartyName,
                r.Phone ?? string.Empty,
                r.Amount.ToString("N0"),
                r.RemainingAmount.ToString("N0"),
                r.Date.ToString("yyyy/MM/dd"),
                r.UserNotes
            }).ToList();
            _exportService.PrintTable("أرصدة العملاء الافتتاحية", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }

        await Task.CompletedTask;
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
                await LoadItemsAsync();
            }
            else if (result.SuccessCount > 0)
            {
                ImportStatusMessage = $"نجح {result.SuccessCount} | فشل {result.FailedCount}\n{string.Join("\n", result.Errors.Take(5))}";
                BeautifulMessageDialog.ShowWarning(ImportStatusMessage);
                await LoadCustomersAsync();
                await LoadItemsAsync();
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
