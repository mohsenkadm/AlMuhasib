using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldCustomersViewModel : ViewModelBase
{
    private readonly IGoldCustomerService _customerService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserPreferencesService _userPreferences;
    private readonly INavigationService _navigationService;
    private System.Timers.Timer? _debounceTimer;
    private int? _editingId;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private GoldCustomerListItem? _selectedCustomer;

    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string _editPhone = string.Empty;
    [ObservableProperty] private string _editAddress = string.Empty;
    [ObservableProperty] private string _editNotes = string.Empty;
    [ObservableProperty] private bool _editIsActive = true;
    [ObservableProperty] private string _dialogError = string.Empty;

    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private GoldCustomerListItem? _customerToDelete;
    [ObservableProperty] private bool _isCardView;

    public ObservableCollection<GoldCustomerListItem> Customers { get; } = [];

    public GoldCustomersViewModel(
        IGoldCustomerService customerService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IUserPreferencesService userPreferences,
        INavigationService navigationService)
    {
        _customerService = customerService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _userPreferences = userPreferences;
        _navigationService = navigationService;
        IsCardView = ListViewModeHelper.LoadIsCardView(_userPreferences, ListViewModeKeys.GoldCustomers);
        PageTitle = "الزبائن";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.Customers);
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
                var (allItems, _) = await _customerService.GetPagedAsync(1, int.MaxValue, search, activeOnly: null);
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
                CurrentPage, PageSize, search, activeOnly: null);

            TotalCount = totalCount;
            TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
            PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);

            Customers.Clear();
            foreach (var item in items)
                Customers.Add(item);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر تحميل الزبائن:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
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
        _editingId = null;
        IsEditMode = false;
        DialogTitle = "إضافة زبون";
        EditName = string.Empty;
        EditPhone = string.Empty;
        EditAddress = string.Empty;
        EditNotes = string.Empty;
        EditIsActive = true;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task OpenEditDialog(GoldCustomerListItem? item)
    {
        if (item is null) return;
        try
        {
            var entity = await _customerService.GetByIdAsync(item.Id);
            if (entity is null)
            {
                BeautifulMessageDialog.ShowWarning("الزبون غير موجود");
                return;
            }

            _editingId = entity.Id;
            IsEditMode = true;
            DialogTitle = "تعديل بيانات الزبون";
            EditName = entity.Name;
            EditPhone = entity.Phone;
            EditAddress = entity.Address;
            EditNotes = entity.Notes;
            EditIsActive = entity.IsActive;
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
        if (string.IsNullOrWhiteSpace(EditName))
        {
            DialogError = "اسم الزبون مطلوب";
            return;
        }

        try
        {
            if (IsEditMode && _editingId.HasValue)
            {
                var existing = await _customerService.GetByIdAsync(_editingId.Value);
                if (existing is null)
                {
                    DialogError = "الزبون غير موجود";
                    return;
                }

                existing.Name = EditName.Trim();
                existing.Phone = EditPhone?.Trim() ?? string.Empty;
                existing.Address = EditAddress?.Trim() ?? string.Empty;
                existing.Notes = EditNotes?.Trim() ?? string.Empty;
                existing.IsActive = EditIsActive;
                existing.UpdatedBy = _currentUserService.Username;
                await _customerService.UpdateAsync(existing);
            }
            else
            {
                await _customerService.CreateAsync(new GoldCustomer
                {
                    Name = EditName.Trim(),
                    Phone = EditPhone?.Trim() ?? string.Empty,
                    Address = EditAddress?.Trim() ?? string.Empty,
                    Notes = EditNotes?.Trim() ?? string.Empty,
                    IsActive = EditIsActive,
                    CreatedBy = _currentUserService.Username
                });
            }

            IsDialogOpen = false;
            BeautifulMessageDialog.ShowSuccess("تم حفظ الزبون");
            await LoadCustomersAsync();
        }
        catch (Exception ex)
        {
            DialogError = ex.Message;
        }
    }

    [RelayCommand]
    private void ConfirmDelete(GoldCustomerListItem? item)
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
        if (CustomerToDelete is null) return;
        try
        {
            await _customerService.DeleteAsync(CustomerToDelete.Id, _currentUserService.Username);
            IsDeleteDialogOpen = false;
            CustomerToDelete = null;
            BeautifulMessageDialog.ShowSuccess("تم حذف الزبون");
            await LoadCustomersAsync();
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
            var (allItems, _) = await _customerService.GetPagedAsync(1, int.MaxValue, null, activeOnly: null);
            var exportData = allItems.Select(c => new
            {
                الاسم = c.Name,
                الهاتف = c.Phone,
                العنوان = c.Address,
                رصيد_آجل_د_ع = c.CreditBalanceIqd,
                رصيد_آجل_دولار = c.CreditBalanceUsd,
                فواتير_مفتوحة = c.OpenInvoiceCount,
                نشط = c.IsActive ? "نعم" : "لا"
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"زبائن_الذهب_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "الزبائن");
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
            var (allItems, _) = await _customerService.GetPagedAsync(1, int.MaxValue, null, activeOnly: null);
            var columns = new[] { "الاسم", "الهاتف", "العنوان", "رصيد آجل د.ع", "رصيد آجل $", "فواتير مفتوحة", "نشط" };
            IList<object[]> rows = allItems.Select(c => new object[]
            {
                c.Name,
                c.Phone,
                c.Address,
                c.CreditBalanceIqd.ToString("N0"),
                c.CreditBalanceUsd.ToString("N2"),
                c.OpenInvoiceCount,
                c.IsActive ? "نعم" : "لا"
            }).ToList();
            _exportService.PrintTable("قائمة زبائن الذهب", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenCustomerStatement(GoldCustomerListItem? customer)
    {
        if (customer is null) return;
        GoldNavigationContext.PendingCustomerId = customer.Id;
        _navigationService.NavigateTo<GoldCustomerStatementViewModel>();
    }

    partial void OnIsCardViewChanged(bool value) =>
        ListViewModeHelper.SaveIsCardView(_userPreferences, ListViewModeKeys.GoldCustomers, value);
}
