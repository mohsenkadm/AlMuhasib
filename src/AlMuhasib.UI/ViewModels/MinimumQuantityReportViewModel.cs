using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class MinimumQuantityReportViewModel : ReportViewModelBase
{
    public record StatusFilterItem(string Label, MinimumQuantityFilter Value);

    [ObservableProperty] private string _totalItems = "0";
    [ObservableProperty] private string _belowMinimumCount = "0";
    [ObservableProperty] private string _atMinimumCount = "0";
    [ObservableProperty] private string _aboveMinimumCount = "0";
    [ObservableProperty] private string _totalShortage = "0";

    [ObservableProperty] private int? _selectedWarehouseId;
    [ObservableProperty] private int? _selectedCategoryId;
    [ObservableProperty] private MinimumQuantityFilter _selectedFilter = MinimumQuantityFilter.All;
    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private bool _isDetailsOpen;
    [ObservableProperty] private string _detailsProductName = string.Empty;
    [ObservableProperty] private string _detailsBarcode = string.Empty;
    [ObservableProperty] private string _detailsCategory = string.Empty;
    [ObservableProperty] private string _detailsDescription = string.Empty;
    [ObservableProperty] private string _detailsSelectedWarehouse = string.Empty;
    [ObservableProperty] private string _detailsCurrentQuantity = "0";
    [ObservableProperty] private string _detailsMinQuantity = "0";
    [ObservableProperty] private string _detailsDifference = "0";
    [ObservableProperty] private string _detailsStatus = string.Empty;

    public ObservableCollection<Warehouse> Warehouses { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];
    public List<StatusFilterItem> StatusFilters { get; } =
    [
        new("الكل", MinimumQuantityFilter.All),
        new("تحت الحد", MinimumQuantityFilter.BelowMinimum),
        new("مساوٍ للحد", MinimumQuantityFilter.AtMinimum),
        new("فوق الحد", MinimumQuantityFilter.AboveMinimum),
    ];

    private List<MinimumQuantityRow> _allRows = [];
    public ObservableCollection<MinimumQuantityRow> Rows { get; } = [];
    public ObservableCollection<MinimumQuantityRow> DetailWarehouseRows { get; } = [];

    public MinimumQuantityReportViewModel(
        IReportService reportService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "كميات الحد الأدنى";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, ScreenPermissionRegistry.MinimumQuantityReport);

        Warehouses.Clear();
        foreach (var w in await _unitOfWork.Warehouses.GetAllAsync())
            Warehouses.Add(w);

        Categories.Clear();
        foreach (var c in await _unitOfWork.Categories.GetAllAsync())
            Categories.Add(c);

        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;

            var result = await _reportService.GetMinimumQuantityReportAsync(
                SelectedWarehouseId,
                SelectedCategoryId,
                SelectedFilter,
                SearchText);

            TotalItems = result.TotalItems.ToString("N0");
            BelowMinimumCount = result.BelowMinimumCount.ToString("N0");
            AtMinimumCount = result.AtMinimumCount.ToString("N0");
            AboveMinimumCount = result.AboveMinimumCount.ToString("N0");
            TotalShortage = result.TotalShortage.ToString("N0");

            _allRows = result.Rows;
            CurrentPage = 1;
            UpdatePaginationWithFilters(_allRows, Rows);
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

    protected override void OnPageChanged() => UpdatePaginationWithFilters(_allRows, Rows);

    [RelayCommand]
    private void ShowDetails(MinimumQuantityRow? row)
    {
        if (row is null) return;

        DetailsProductName = row.ProductName;
        DetailsBarcode = string.IsNullOrWhiteSpace(row.Barcode) ? "—" : row.Barcode!;
        DetailsCategory = row.CategoryName;
        DetailsDescription = string.IsNullOrWhiteSpace(row.Description) ? "—" : row.Description!;
        DetailsSelectedWarehouse = row.WarehouseName;
        DetailsCurrentQuantity = row.CurrentQuantity.ToString("N0");
        DetailsMinQuantity = row.MinQuantity.ToString("N0");
        DetailsDifference = row.Difference.ToString("N0");
        DetailsStatus = row.StatusDisplay;

        DetailWarehouseRows.Clear();
        foreach (var item in _allRows
                     .Where(r => r.ProductId == row.ProductId)
                     .OrderBy(r => r.WarehouseName))
            DetailWarehouseRows.Add(item);

        IsDetailsOpen = true;
        _ = LoadProductWarehouseDetailsAsync(row.ProductId, row);
    }

    private async Task LoadProductWarehouseDetailsAsync(int productId, MinimumQuantityRow selected)
    {
        try
        {
            var full = await _reportService.GetMinimumQuantityReportAsync(
                warehouseId: null,
                categoryId: null,
                filter: MinimumQuantityFilter.All,
                search: null);

            var productRows = full.Rows
                .Where(r => r.ProductId == productId)
                .OrderBy(r => r.WarehouseName)
                .ToList();

            if (productRows.Count == 0)
                productRows = [selected];

            DetailWarehouseRows.Clear();
            foreach (var item in productRows)
                DetailWarehouseRows.Add(item);
        }
        catch
        {
            // Keep rows already shown from the current filter set.
        }
    }

    [RelayCommand]
    private void CloseDetails() => IsDetailsOpen = false;

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel|*.xlsx",
            FileName = "كميات_الحد_الادنى.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        var cols = new[]
        {
            "المنتج", "الباركود", "التصنيف", "المخزن",
            "الكمية الحالية", "الحد الأدنى", "الفرق", "الحالة"
        };
        var rows = _allRows.Select(r => new object[]
        {
            r.ProductName,
            r.Barcode ?? "",
            r.CategoryName,
            r.WarehouseName,
            r.CurrentQuantity.ToString("N0"),
            r.MinQuantity.ToString("N0"),
            r.Difference.ToString("N0"),
            r.StatusDisplay
        }).ToList();

        _exportService.ExportToExcel(dlg.FileName, "الحد الأدنى", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[]
        {
            "المنتج", "الباركود", "التصنيف", "المخزن",
            "الكمية الحالية", "الحد الأدنى", "الفرق", "الحالة"
        };
        var rows = _allRows.Select(r => new object[]
        {
            r.ProductName,
            r.Barcode ?? "",
            r.CategoryName,
            r.WarehouseName,
            r.CurrentQuantity.ToString("N0"),
            r.MinQuantity.ToString("N0"),
            r.Difference.ToString("N0"),
            r.StatusDisplay
        }).ToList();

        _exportService.PrintTable("كميات الحد الأدنى", cols, rows);
    }
}
