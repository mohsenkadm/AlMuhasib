using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using AlMuhasib.UI.Controls;

using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.ViewModels;

public partial class WarehousesViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<Warehouse> Warehouses { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private Warehouse? _selectedWarehouse;

    // Dialog state
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string _editLocation = string.Empty;
    [ObservableProperty] private string _dialogError = string.Empty;

    // Delete confirmation
    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private Warehouse? _warehouseToDelete;

    private int? _editingWarehouseId;
    private System.Timers.Timer? _debounceTimer;

    public WarehousesViewModel(
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "\u0627\u0644\u0645\u062e\u0627\u0632\u0646";
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "Warehouses");
            await LoadWarehousesAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadWarehousesAsync()
    {
        var filter = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
        System.Linq.Expressions.Expression<Func<Warehouse, bool>>? searchPredicate = filter is null
            ? null
            : w => w.Name.Contains(filter) || (w.Location != null && w.Location.Contains(filter));

        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
        {
            var (allItems, _) = await _unitOfWork.Warehouses.GetPagedAsync(
                1, int.MaxValue, searchPredicate, q => q.OrderByDescending(w => w.CreatedAt));

            var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
            MasterDataColumnFilterHelper.ApplyClientPagination(
                filtered, Warehouses, CurrentPage, PageSize,
                out var filteredTotal, out var filteredPages, out var filteredText);
            TotalCount = filteredTotal;
            TotalPages = filteredPages;
            PaginationText = filteredText;
            return;
        }

        var (items, totalCount) = await _unitOfWork.Warehouses.GetPagedAsync(
            CurrentPage, PageSize, searchPredicate, q => q.OrderByDescending(w => w.CreatedAt));

        TotalCount = totalCount;
        TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
        PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);

        Warehouses.Clear();
        foreach (var w in items)
            Warehouses.Add(w);
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
        _debounceTimer.Elapsed += async (_, _) =>
        {
            _debounceTimer?.Stop();
            CurrentPage = 1;
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
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
        SearchText = string.Empty;
        await LoadWarehousesAsync();
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        _editingWarehouseId = null;
        IsEditMode = false;
        DialogTitle = "\u0625\u0636\u0627\u0641\u0629 \u0645\u062e\u0632\u0646 \u062c\u062f\u064a\u062f";
        EditName = string.Empty;
        EditLocation = string.Empty;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(Warehouse warehouse)
    {
        if (warehouse is null) return;
        _editingWarehouseId = warehouse.Id;
        IsEditMode = true;
        DialogTitle = "\u062a\u0639\u062f\u064a\u0644 \u0627\u0644\u0645\u062e\u0632\u0646";
        EditName = warehouse.Name;
        EditLocation = warehouse.Location ?? string.Empty;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveWarehouse()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            DialogError = "\u0627\u0633\u0645 \u0627\u0644\u0645\u062e\u0632\u0646 \u0645\u0637\u0644\u0648\u0628";
            return;
        }

        DialogError = string.Empty;

        try
        {
            if (IsEditMode && _editingWarehouseId.HasValue)
            {
                var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(_editingWarehouseId.Value);
                if (warehouse is null) return;

                warehouse.Name = EditName.Trim();
                warehouse.Location = string.IsNullOrWhiteSpace(EditLocation) ? null : EditLocation.Trim();
                warehouse.UpdatedAt = DateTime.UtcNow;
                warehouse.UpdatedBy = _currentUserService.Username;

                _unitOfWork.Warehouses.Update(warehouse);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                var warehouse = new Warehouse
                {
                    Name = EditName.Trim(),
                    Location = string.IsNullOrWhiteSpace(EditLocation) ? null : EditLocation.Trim(),
                    CreatedBy = _currentUserService.Username
                };

                await _unitOfWork.Warehouses.AddAsync(warehouse);
                await _unitOfWork.SaveChangesAsync();
            }

            IsDialogOpen = false;
            await LoadWarehousesAsync();
        }
        catch (Exception ex)
        {
            DialogError = $"\u062d\u062f\u062b \u062e\u0637\u0623: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private void ConfirmDelete(Warehouse warehouse)
    {
        if (warehouse is null) return;
        WarehouseToDelete = warehouse;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private async Task ExecuteDelete()
    {
        if (WarehouseToDelete is null) return;
        try
        {
            _unitOfWork.Warehouses.SoftDelete(WarehouseToDelete, _currentUserService.Username);
            await _unitOfWork.SaveChangesAsync();
            IsDeleteDialogOpen = false;
            WarehouseToDelete = null;
            await LoadWarehousesAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"\u062d\u062f\u062b \u062e\u0637\u0623 \u0623\u062b\u0646\u0627\u0621 \u0627\u0644\u062d\u0630\u0641: {ex.Message}");
        }
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteDialogOpen = false;
        WarehouseToDelete = null;
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            var (allItems, _) = await _unitOfWork.Warehouses.GetPagedAsync(1, int.MaxValue);
            var exportData = allItems.Select(w => new
            {
                \u0627\u0644\u0627\u0633\u0645 = w.Name,
                \u0627\u0644\u0645\u0648\u0642\u0639 = w.Location ?? "",
                \u062a\u0627\u0631\u064a\u062e_\u0627\u0644\u0625\u0646\u0634\u0627\u0621 = w.CreatedAt.ToString("yyyy/MM/dd")
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"\u0627\u0644\u0645\u062e\u0627\u0632\u0646_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "\u0627\u0644\u0645\u062e\u0627\u0632\u0646");
                BeautifulMessageDialog.ShowSuccess("\u062a\u0645 \u0627\u0644\u062a\u0635\u062f\u064a\u0631 \u0628\u0646\u062c\u0627\u062d");
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"\u062d\u062f\u062b \u062e\u0637\u0623 \u0623\u062b\u0646\u0627\u0621 \u0627\u0644\u062a\u0635\u062f\u064a\u0631: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PrintTable()
    {
        try
        {
            var (allItems, _) = await _unitOfWork.Warehouses.GetPagedAsync(1, int.MaxValue);
            var columns = new[] { "الاسم", "الموقع", "تاريخ الإنشاء" };
            IList<object[]> rows = allItems.Select(w => new object[]
            {
                w.Name,
                w.Location ?? "",
                w.CreatedAt.ToString("yyyy/MM/dd")
            }).ToList();
            _exportService.PrintTable("قائمة المخازن", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }
}
