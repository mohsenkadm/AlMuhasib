using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldCustomersViewModel : ViewModelBase
{
    private readonly IGoldCustomerService _customerService;
    private readonly ICurrentUserService _currentUserService;
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

    public ObservableCollection<GoldCustomerListItem> Customers { get; } = [];

    public GoldCustomersViewModel(
        IGoldCustomerService customerService,
        ICurrentUserService currentUserService)
    {
        _customerService = customerService;
        _currentUserService = currentUserService;
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
            var (items, totalCount) = await _customerService.GetPagedAsync(
                CurrentPage,
                PageSize,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
                activeOnly: null);

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
}
