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

public partial class GoldSuppliersViewModel : ViewModelBase
{
    private readonly IGoldSupplierService _supplierService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private System.Timers.Timer? _debounceTimer;
    private int? _editingId;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private GoldSupplierListItem? _selectedSupplier;

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
    [ObservableProperty] private GoldSupplierListItem? _supplierToDelete;

    public ObservableCollection<GoldSupplierListItem> Suppliers { get; } = [];

    public GoldSuppliersViewModel(
        IGoldSupplierService supplierService,
        IExportService exportService,
        ICurrentUserService currentUserService)
    {
        _supplierService = supplierService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "الموردون";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.Suppliers);
        await LoadSuppliersAsync();
    }

    private async Task LoadSuppliersAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

            if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            {
                var (allItems, _) = await _supplierService.GetPagedAsync(1, int.MaxValue, search, activeOnly: null);
                var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
                MasterDataColumnFilterHelper.ApplyClientPagination(
                    filtered, Suppliers, CurrentPage, PageSize,
                    out var filteredTotal, out var filteredPages, out var filteredText);
                TotalCount = filteredTotal;
                TotalPages = filteredPages;
                PaginationText = filteredText;
                return;
            }

            var (items, totalCount) = await _supplierService.GetPagedAsync(
                CurrentPage, PageSize, search, activeOnly: null);

            TotalCount = totalCount;
            TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
            PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);

            Suppliers.Clear();
            foreach (var item in items)
                Suppliers.Add(item);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر تحميل الموردين:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        _ = LoadSuppliersAsync();
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
                await LoadSuppliersAsync();
            });
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    [RelayCommand]
    private async Task FirstPage() { CurrentPage = 1; await LoadSuppliersAsync(); }

    [RelayCommand]
    private async Task PreviousPage() { if (CurrentPage > 1) { CurrentPage--; await LoadSuppliersAsync(); } }

    [RelayCommand]
    private async Task NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; await LoadSuppliersAsync(); } }

    [RelayCommand]
    private async Task LastPage() { CurrentPage = TotalPages; await LoadSuppliersAsync(); }

    [RelayCommand]
    private async Task Refresh()
    {
        CurrentPage = 1;
        await LoadSuppliersAsync();
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        if (!CanAdd) return;
        _editingId = null;
        IsEditMode = false;
        DialogTitle = "إضافة مورد";
        EditName = string.Empty;
        EditPhone = string.Empty;
        EditAddress = string.Empty;
        EditNotes = string.Empty;
        EditIsActive = true;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task OpenEditDialog(GoldSupplierListItem? item)
    {
        if (item is null || !CanEdit) return;
        try
        {
            var entity = await _supplierService.GetByIdAsync(item.Id);
            if (entity is null)
            {
                BeautifulMessageDialog.ShowWarning("المورد غير موجود");
                return;
            }

            _editingId = entity.Id;
            IsEditMode = true;
            DialogTitle = "تعديل بيانات المورد";
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
    private async Task SaveSupplierAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            DialogError = "اسم المورد مطلوب";
            return;
        }

        try
        {
            if (IsEditMode && _editingId.HasValue)
            {
                var existing = await _supplierService.GetByIdAsync(_editingId.Value);
                if (existing is null)
                {
                    DialogError = "المورد غير موجود";
                    return;
                }

                existing.Name = EditName.Trim();
                existing.Phone = EditPhone?.Trim() ?? string.Empty;
                existing.Address = EditAddress?.Trim() ?? string.Empty;
                existing.Notes = EditNotes?.Trim() ?? string.Empty;
                existing.IsActive = EditIsActive;
                existing.UpdatedBy = _currentUserService.Username;
                await _supplierService.UpdateAsync(existing);
            }
            else
            {
                await _supplierService.CreateAsync(new GoldSupplier
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
            BeautifulMessageDialog.ShowSuccess("تم حفظ المورد");
            await LoadSuppliersAsync();
        }
        catch (Exception ex)
        {
            DialogError = ex.Message;
        }
    }

    [RelayCommand]
    private void ConfirmDelete(GoldSupplierListItem? item)
    {
        if (item is null || !CanDelete) return;
        SupplierToDelete = item;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteDialogOpen = false;
        SupplierToDelete = null;
    }

    [RelayCommand]
    private async Task ExecuteDeleteAsync()
    {
        if (SupplierToDelete is null) return;
        try
        {
            await _supplierService.DeleteAsync(SupplierToDelete.Id, _currentUserService.Username);
            IsDeleteDialogOpen = false;
            SupplierToDelete = null;
            BeautifulMessageDialog.ShowSuccess("تم حذف المورد");
            await LoadSuppliersAsync();
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
            var (allItems, _) = await _supplierService.GetPagedAsync(1, int.MaxValue, null, activeOnly: null);
            var exportData = allItems.Select(s => new
            {
                الاسم = s.Name,
                الهاتف = s.Phone,
                العنوان = s.Address,
                رصيد_آجل_د_ع = s.CreditBalanceIqd,
                رصيد_آجل_دولار = s.CreditBalanceUsd,
                فواتير_مفتوحة = s.OpenInvoiceCount,
                نشط = s.IsActive ? "نعم" : "لا"
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"موردو_الذهب_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "الموردون");
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
            var (allItems, _) = await _supplierService.GetPagedAsync(1, int.MaxValue, null, activeOnly: null);
            var columns = new[] { "الاسم", "الهاتف", "العنوان", "رصيد آجل د.ع", "رصيد آجل $", "فواتير مفتوحة", "نشط" };
            IList<object[]> rows = allItems.Select(s => new object[]
            {
                s.Name,
                s.Phone,
                s.Address,
                s.CreditBalanceIqd.ToString("N0"),
                s.CreditBalanceUsd.ToString("N2"),
                s.OpenInvoiceCount,
                s.IsActive ? "نعم" : "لا"
            }).ToList();
            _exportService.PrintTable("قائمة موردي الذهب", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }
}
