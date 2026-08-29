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

public partial class GoldWarehousesViewModel : ViewModelBase
{
    private readonly IGoldWarehouseService _warehouseService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserPreferencesService _userPreferences;
    private System.Timers.Timer? _debounceTimer;
    private int? _editingId;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private GoldWarehouseListItem? _selectedWarehouse;

    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string _editNotes = string.Empty;
    [ObservableProperty] private bool _editIsDefault;
    [ObservableProperty] private bool _editIsActive = true;
    [ObservableProperty] private string _dialogError = string.Empty;

    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private GoldWarehouseListItem? _warehouseToDelete;
    [ObservableProperty] private bool _isCardView;

    public ObservableCollection<GoldWarehouseListItem> Warehouses { get; } = [];

    public GoldWarehousesViewModel(
        IGoldWarehouseService warehouseService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IUserPreferencesService userPreferences)
    {
        _warehouseService = warehouseService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _userPreferences = userPreferences;
        IsCardView = ListViewModeHelper.LoadIsCardView(_userPreferences, ListViewModeKeys.GoldWarehouses);
        PageTitle = "المخازن";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.Warehouses);
        await LoadWarehousesAsync();
    }

    private async Task LoadWarehousesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

            if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            {
                var (allItems, _) = await _warehouseService.GetPagedAsync(1, int.MaxValue, search, activeOnly: null);
                var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
                MasterDataColumnFilterHelper.ApplyClientPagination(
                    filtered, Warehouses, CurrentPage, PageSize,
                    out var filteredTotal, out var filteredPages, out var filteredText);
                TotalCount = filteredTotal;
                TotalPages = filteredPages;
                PaginationText = filteredText;
                return;
            }

            var (items, totalCount) = await _warehouseService.GetPagedAsync(
                CurrentPage, PageSize, search, activeOnly: null);

            TotalCount = totalCount;
            TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
            PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);

            Warehouses.Clear();
            foreach (var item in items)
                Warehouses.Add(item);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر تحميل المخازن:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        _ = LoadWarehousesAsync();
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
                await LoadWarehousesAsync();
            });
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    [RelayCommand]
    private async Task FirstPage() { CurrentPage = 1; await LoadWarehousesAsync(); }

    [RelayCommand]
    private async Task PreviousPage() { if (CurrentPage > 1) { CurrentPage--; await LoadWarehousesAsync(); } }

    [RelayCommand]
    private async Task NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; await LoadWarehousesAsync(); } }

    [RelayCommand]
    private async Task LastPage() { CurrentPage = TotalPages; await LoadWarehousesAsync(); }

    [RelayCommand]
    private async Task Refresh()
    {
        CurrentPage = 1;
        await LoadWarehousesAsync();
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        if (!CanAdd) return;
        _editingId = null;
        IsEditMode = false;
        DialogTitle = "إضافة مخزن";
        EditName = string.Empty;
        EditNotes = string.Empty;
        EditIsDefault = false;
        EditIsActive = true;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task OpenEditDialog(GoldWarehouseListItem? item)
    {
        if (item is null || !CanEdit) return;
        try
        {
            var entity = await _warehouseService.GetByIdAsync(item.Id);
            if (entity is null)
            {
                BeautifulMessageDialog.ShowWarning("المخزن غير موجود");
                return;
            }

            _editingId = entity.Id;
            IsEditMode = true;
            DialogTitle = "تعديل المخزن";
            EditName = entity.Name;
            EditNotes = entity.Notes;
            EditIsDefault = entity.IsDefault;
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
    private async Task SaveWarehouseAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            DialogError = "اسم المخزن مطلوب";
            return;
        }

        try
        {
            if (IsEditMode && _editingId.HasValue)
            {
                var existing = await _warehouseService.GetByIdAsync(_editingId.Value);
                if (existing is null)
                {
                    DialogError = "المخزن غير موجود";
                    return;
                }

                existing.Name = EditName.Trim();
                existing.Notes = EditNotes?.Trim() ?? string.Empty;
                existing.IsDefault = EditIsDefault;
                existing.IsActive = EditIsActive;
                existing.UpdatedBy = _currentUserService.Username;
                await _warehouseService.UpdateAsync(existing);
            }
            else
            {
                await _warehouseService.CreateAsync(new GoldWarehouse
                {
                    Name = EditName.Trim(),
                    Notes = EditNotes?.Trim() ?? string.Empty,
                    IsDefault = EditIsDefault,
                    IsActive = EditIsActive,
                    CreatedBy = _currentUserService.Username
                });
            }

            IsDialogOpen = false;
            BeautifulMessageDialog.ShowSuccess("تم حفظ المخزن");
            await LoadWarehousesAsync();
        }
        catch (Exception ex)
        {
            DialogError = ex.Message;
        }
    }

    [RelayCommand]
    private async Task SetDefaultAsync(GoldWarehouseListItem? item)
    {
        if (item is null || !CanEdit) return;
        if (item.IsDefault) return;

        try
        {
            var entity = await _warehouseService.GetByIdAsync(item.Id);
            if (entity is null)
            {
                BeautifulMessageDialog.ShowWarning("المخزن غير موجود");
                return;
            }

            entity.IsDefault = true;
            entity.UpdatedBy = _currentUserService.Username;
            await _warehouseService.UpdateAsync(entity);
            BeautifulMessageDialog.ShowSuccess($"تم تعيين «{entity.Name}» كمخزن افتراضي");
            await LoadWarehousesAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void ConfirmDelete(GoldWarehouseListItem? item)
    {
        if (item is null || !CanDelete) return;
        WarehouseToDelete = item;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteDialogOpen = false;
        WarehouseToDelete = null;
    }

    [RelayCommand]
    private async Task ExecuteDeleteAsync()
    {
        if (WarehouseToDelete is null) return;
        try
        {
            await _warehouseService.DeleteAsync(WarehouseToDelete.Id, _currentUserService.Username);
            IsDeleteDialogOpen = false;
            WarehouseToDelete = null;
            BeautifulMessageDialog.ShowSuccess("تم حذف المخزن");
            await LoadWarehousesAsync();
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
            var (allItems, _) = await _warehouseService.GetPagedAsync(1, int.MaxValue, null, activeOnly: null);
            var exportData = allItems.Select(w => new
            {
                الاسم = w.Name,
                افتراضي = w.IsDefault ? "نعم" : "لا",
                نشط = w.IsActive ? "نعم" : "لا",
                إجمالي_الوزن = w.TotalGrams,
                عدد_الأرصدة = w.BalanceRowCount,
                ملاحظات = w.Notes
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"مخازن_الذهب_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "المخازن");
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
            var (allItems, _) = await _warehouseService.GetPagedAsync(1, int.MaxValue, null, activeOnly: null);
            var columns = new[] { "الاسم", "افتراضي", "نشط", "إجمالي الوزن", "عدد الأرصدة", "ملاحظات" };
            IList<object[]> rows = allItems.Select(w => new object[]
            {
                w.Name,
                w.IsDefault ? "نعم" : "لا",
                w.IsActive ? "نعم" : "لا",
                w.TotalGrams.ToString("N2"),
                w.BalanceRowCount,
                w.Notes
            }).ToList();
            _exportService.PrintTable("قائمة مخازن الذهب", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }

    partial void OnIsCardViewChanged(bool value) =>
        ListViewModeHelper.SaveIsCardView(_userPreferences, ListViewModeKeys.GoldWarehouses, value);
}
