using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities.Customs;
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

public partial class CustomsCustomersViewModel : ViewModelBase
{
    private readonly ICustomsCustomerService _customerService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserPreferencesService _userPreferences;
    private readonly MainWindowViewModel _mainWindow;
    private System.Timers.Timer? _debounceTimer;
    private int? _editingId;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private CustomsCustomerListItem? _selectedCustomer;
    [ObservableProperty] private string? _sortBy;
    [ObservableProperty] private bool _sortDescending = true;

    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private CustomsCustomerType _editCustomerType = CustomsCustomerType.Individual;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string _editCompanyName = string.Empty;
    [ObservableProperty] private string _editPhone = string.Empty;
    [ObservableProperty] private string _editAddress = string.Empty;
    [ObservableProperty] private string _editNotes = string.Empty;
    [ObservableProperty] private bool _editIsActive = true;
    [ObservableProperty] private string _dialogError = string.Empty;
    [ObservableProperty] private bool _showCompanyName;

    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private CustomsCustomerListItem? _customerToDelete;
    [ObservableProperty] private bool _isCardView;

    public ObservableCollection<CustomsCustomerListItem> Customers { get; } = [];

    public IReadOnlyList<CustomsCustomerTypeOption> CustomerTypeOptions { get; } =
    [
        new(CustomsCustomerType.Individual, CustomsCustomerTypeDisplay.ToArabic(CustomsCustomerType.Individual)),
        new(CustomsCustomerType.Company, CustomsCustomerTypeDisplay.ToArabic(CustomsCustomerType.Company))
    ];

    public CustomsCustomersViewModel(
        ICustomsCustomerService customerService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IUserPreferencesService userPreferences,
        MainWindowViewModel mainWindow)
    {
        _customerService = customerService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _userPreferences = userPreferences;
        _mainWindow = mainWindow;
        IsCardView = ListViewModeHelper.LoadIsCardView(_userPreferences, ListViewModeKeys.CustomsCustomers);
        PageTitle = "العملاء";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CustomsPermissionRegistry.Customers);
        await LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

            if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            {
                var (allItems, _) = await _customerService.GetPagedAsync(
                    1, int.MaxValue, search, activeOnly: null, sortBy: SortBy, sortDescending: SortDescending);
                var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
                MasterDataColumnFilterHelper.ApplyClientPagination(
                    filtered, Customers, CurrentPage, PageSize,
                    out var filteredTotal, out var filteredPages, out var filteredText);
                TotalCount = filteredTotal;
                TotalPages = filteredPages;
                PaginationText = filteredText;
                return;
            }

            var (items, totalCount) = await _customerService.GetPagedAsync(
                CurrentPage, PageSize, search, activeOnly: null, sortBy: SortBy, sortDescending: SortDescending);

            TotalCount = totalCount;
            TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
            PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);

            Customers.Clear();
            foreach (var item in items)
                Customers.Add(item);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر تحميل العملاء:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>M-04: server-side sort from DataGrid header (allowlisted columns only).</summary>
    public Task ApplyServerSortAsync(string sortBy, bool sortDescending)
    {
        SortBy = sortBy;
        SortDescending = sortDescending;
        CurrentPage = 1;
        return LoadCustomersAsync();
    }

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        _ = LoadCustomersAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(400);
        _debounceTimer.Elapsed += (_, _) =>
        {
            _debounceTimer?.Stop();
            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                CurrentPage = 1;
                await LoadCustomersAsync();
            });
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    partial void OnEditCustomerTypeChanged(CustomsCustomerType value) =>
        ShowCompanyName = value == CustomsCustomerType.Company;

    [RelayCommand]
    private async Task FirstPage() { CurrentPage = 1; await LoadCustomersAsync(); }

    [RelayCommand]
    private async Task PreviousPage() { if (CurrentPage > 1) { CurrentPage--; await LoadCustomersAsync(); } }

    [RelayCommand]
    private async Task NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; await LoadCustomersAsync(); } }

    [RelayCommand]
    private async Task LastPage() { CurrentPage = TotalPages; await LoadCustomersAsync(); }

    [RelayCommand]
    private async Task Refresh()
    {
        CurrentPage = 1;
        await LoadCustomersAsync();
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        if (!CanAdd)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية إضافة عميل");
            return;
        }

        _editingId = null;
        IsEditMode = false;
        DialogTitle = "إضافة عميل";
        EditCustomerType = CustomsCustomerType.Individual;
        EditName = string.Empty;
        EditCompanyName = string.Empty;
        EditPhone = string.Empty;
        EditAddress = string.Empty;
        EditNotes = string.Empty;
        EditIsActive = true;
        DialogError = string.Empty;
        ShowCompanyName = false;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task OpenEditDialog(CustomsCustomerListItem? item)
    {
        if (item is null) return;
        if (!CanEdit)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية تعديل العميل");
            return;
        }

        try
        {
            var entity = await _customerService.GetByIdAsync(item.Id);
            if (entity is null)
            {
                BeautifulMessageDialog.ShowWarning("العميل غير موجود");
                return;
            }

            _editingId = entity.Id;
            IsEditMode = true;
            DialogTitle = "تعديل بيانات العميل";
            EditCustomerType = entity.CustomerType;
            EditName = entity.Name;
            EditCompanyName = entity.CompanyName;
            EditPhone = entity.Phone;
            EditAddress = entity.Address;
            EditNotes = entity.Notes;
            EditIsActive = entity.IsActive;
            ShowCompanyName = entity.CustomerType == CustomsCustomerType.Company;
            DialogError = string.Empty;
            IsDialogOpen = true;
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private async Task SaveCustomerAsync()
    {
        if (IsEditMode ? !CanEdit : !CanAdd)
        {
            DialogError = "ليس لديك صلاحية الحفظ";
            return;
        }

        if (string.IsNullOrWhiteSpace(EditName))
        {
            DialogError = "اسم العميل مطلوب";
            return;
        }

        try
        {
            if (IsEditMode && _editingId.HasValue)
            {
                var existing = await _customerService.GetByIdAsync(_editingId.Value);
                if (existing is null)
                {
                    DialogError = "العميل غير موجود";
                    return;
                }

                existing.CustomerType = EditCustomerType;
                existing.Name = EditName.Trim();
                existing.CompanyName = EditCompanyName?.Trim() ?? string.Empty;
                existing.Phone = EditPhone?.Trim() ?? string.Empty;
                existing.Address = EditAddress?.Trim() ?? string.Empty;
                existing.Notes = EditNotes?.Trim() ?? string.Empty;
                existing.IsActive = EditIsActive;
                existing.UpdatedBy = _currentUserService.Username;
                await _customerService.UpdateAsync(existing);
            }
            else
            {
                await _customerService.CreateAsync(new CustomsCustomer
                {
                    CustomerType = EditCustomerType,
                    Name = EditName.Trim(),
                    CompanyName = EditCompanyName?.Trim() ?? string.Empty,
                    Phone = EditPhone?.Trim() ?? string.Empty,
                    Address = EditAddress?.Trim() ?? string.Empty,
                    Notes = EditNotes?.Trim() ?? string.Empty,
                    IsActive = EditIsActive,
                    CreatedBy = _currentUserService.Username
                });
            }

            IsDialogOpen = false;
            BeautifulMessageDialog.ShowSuccess("تم حفظ العميل");
            await LoadCustomersAsync();
        }
        catch (Exception ex)
        {
            DialogError = ex.Message;
        }
    }

    [RelayCommand]
    private void ConfirmDelete(CustomsCustomerListItem? item)
    {
        if (item is null || !CanDelete) return;
        CustomerToDelete = item;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteDialogOpen = false;
        CustomerToDelete = null;
    }

    [RelayCommand]
    private async Task ExecuteDeleteAsync()
    {
        if (CustomerToDelete is null || !CanDelete) return;
        try
        {
            await _customerService.SoftDeleteAsync(CustomerToDelete.Id, _currentUserService.Username);
            IsDeleteDialogOpen = false;
            CustomerToDelete = null;
            BeautifulMessageDialog.ShowSuccess("تم حذف العميل");
            await LoadCustomersAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task OpenCustomerFileAsync(CustomsCustomerListItem? customer)
    {
        if (customer is null) return;
        CustomsNavigationContext.PendingCustomerId = customer.Id;
        await _mainWindow.OpenTabAsync(
            typeof(CustomsCustomerFileViewModel),
            $"ملف — {customer.Name}",
            PackIconKind.AccountDetails,
            activateIfExists: false,
            permissionScreenName: CustomsPermissionRegistry.Customers);
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            var (allItems, _) = await _customerService.GetPagedAsync(1, int.MaxValue, null, activeOnly: null);
            var columns = new[] { "الاسم", "النوع", "الشركة", "الهاتف", "العنوان", "رصيد د.ع", "رصيد $", "فواتير مفتوحة", "الحالة" };
            IList<object[]> rows = allItems.Select(c => new object[]
            {
                c.Name,
                c.CustomerTypeLabel,
                c.CompanyName,
                c.Phone,
                c.Address,
                c.BalanceIqd,
                c.BalanceUsd,
                c.OpenInvoiceCount,
                c.StatusLabel
            }).ToList();
            ListTableExportHelper.ExportExcel(
                _exportService, toast: null, CanExport,
                "عملاء_التخليص", "العملاء", columns, rows);
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
            var (allItems, _) = await _customerService.GetPagedAsync(1, int.MaxValue, null, activeOnly: null);
            var columns = new[] { "الاسم", "النوع", "الشركة", "الهاتف", "رصيد د.ع", "رصيد $", "فواتير مفتوحة", "الحالة" };
            IList<object[]> rows = allItems.Select(c => new object[]
            {
                c.Name,
                c.CustomerTypeLabel,
                c.CompanyName,
                c.Phone,
                c.BalanceIqd.ToString("N0"),
                c.BalanceUsd.ToString("N2"),
                c.OpenInvoiceCount,
                c.StatusLabel
            }).ToList();
            ListTableExportHelper.Print(
                _exportService, CanPrint,
                "قائمة عملاء التخليص الكمركي", columns, rows,
                warnOnDenied: true);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }

    partial void OnIsCardViewChanged(bool value) =>
        ListViewModeHelper.SaveIsCardView(_userPreferences, ListViewModeKeys.CustomsCustomers, value);
}

public sealed record CustomsCustomerTypeOption(CustomsCustomerType Value, string Label);
