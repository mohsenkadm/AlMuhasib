using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels;

public partial class ProductPricingViewModel : ViewModelBase
{
    private readonly IProductPriceService _productPriceService;
    private readonly IPricingTypeService _pricingTypeService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<ProductPriceEditRow> Rows { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<PricingType> PricingTypes { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private Category? _filterCategory;
    [ObservableProperty] private PricingType? _filterPricingType;
    [ObservableProperty] private string _minSalePriceText = string.Empty;
    [ObservableProperty] private string _maxSalePriceText = string.Empty;
    [ObservableProperty] private string _minPurchasePriceText = string.Empty;
    [ObservableProperty] private string _maxPurchasePriceText = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 50;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _hasUnsavedChanges;
    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private ProductPriceEditRow? _rowToDelete;

    private System.Timers.Timer? _debounceTimer;
    private readonly HashSet<int> _dirtyIds = [];
    private readonly HashSet<ProductPriceEditRow> _newRows = [];

    public ProductPricingViewModel(
        IProductPriceService productPriceService,
        IPricingTypeService pricingTypeService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService)
    {
        _productPriceService = productPriceService;
        _pricingTypeService = pricingTypeService;
        _unitOfWork = unitOfWork;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "تسعير منتجات";
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "ProductPricing");
            await _pricingTypeService.EnsureDefaultExistsAsync();
            await LoadLookupsAsync();
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadLookupsAsync()
    {
        Products.Clear();
        foreach (var p in (await _unitOfWork.Products.GetAllAsync()).OrderBy(p => p.Name))
            Products.Add(p);

        PricingTypes.Clear();
        foreach (var t in await _pricingTypeService.GetActiveAsync())
            PricingTypes.Add(t);

        Categories.Clear();
        Categories.Add(new Category { Id = 0, Name = "كل التصنيفات" });
        foreach (var c in (await _unitOfWork.Categories.GetAllAsync()).OrderBy(c => c.Name))
            Categories.Add(c);

        FilterCategory = Categories.FirstOrDefault();
    }

    private async Task LoadAsync()
    {
        decimal? minSale = ParseDecimal(MinSalePriceText);
        decimal? maxSale = ParseDecimal(MaxSalePriceText);
        decimal? minPurchase = ParseDecimal(MinPurchasePriceText);
        decimal? maxPurchase = ParseDecimal(MaxPurchasePriceText);
        int? categoryId = FilterCategory is { Id: > 0 } ? FilterCategory.Id : null;
        int? pricingTypeId = FilterPricingType?.Id;

        var (items, totalCount) = await _productPriceService.GetPagedAsync(
            CurrentPage, PageSize, SearchText, null, pricingTypeId, categoryId,
            minSale, maxSale, minPurchase, maxPurchase);

        TotalCount = totalCount;
        TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
        PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);

        Rows.Clear();
        _dirtyIds.Clear();
        _newRows.Clear();
        HasUnsavedChanges = false;

        foreach (var item in items)
        {
            var row = ProductPriceEditRow.FromEntity(item, Products, PricingTypes);
            row.Changed += OnRowChanged;
            Rows.Add(row);
        }
    }

    private void OnRowChanged(ProductPriceEditRow row)
    {
        if (row.Id > 0)
            _dirtyIds.Add(row.Id);
        else
            _newRows.Add(row);
        HasUnsavedChanges = true;
    }

    private static decimal? ParseDecimal(string? text) =>
        decimal.TryParse(text?.Trim(), out var value) ? value : null;

    partial void OnSearchTextChanged(string value) => DebounceReload();
    partial void OnFilterCategoryChanged(Category? value) => DebounceReload();
    partial void OnFilterPricingTypeChanged(PricingType? value) => DebounceReload();

    private void DebounceReload()
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(350);
        _debounceTimer.Elapsed += async (_, _) =>
        {
            _debounceTimer?.Stop();
            CurrentPage = 1;
            await Application.Current.Dispatcher.InvokeAsync(async () => await LoadAsync());
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    [RelayCommand]
    private async Task ApplyFilters() { CurrentPage = 1; await LoadAsync(); }

    [RelayCommand]
    private async Task ClearFilters()
    {
        SearchText = string.Empty;
        FilterCategory = Categories.FirstOrDefault();
        FilterPricingType = null;
        MinSalePriceText = string.Empty;
        MaxSalePriceText = string.Empty;
        MinPurchasePriceText = string.Empty;
        MaxPurchasePriceText = string.Empty;
        CurrentPage = 1;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task FirstPage() { CurrentPage = 1; await LoadAsync(); }

    [RelayCommand]
    private async Task PreviousPage() { if (CurrentPage > 1) { CurrentPage--; await LoadAsync(); } }

    [RelayCommand]
    private async Task NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; await LoadAsync(); } }

    [RelayCommand]
    private async Task LastPage() { CurrentPage = TotalPages; await LoadAsync(); }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void AddRow()
    {
        var defaultType = PricingTypes.FirstOrDefault(t => t.IsDefault) ?? PricingTypes.FirstOrDefault();
        var row = new ProductPriceEditRow
        {
            AvailableProducts = Products,
            AvailablePricingTypes = PricingTypes,
            SelectedPricingType = defaultType
        };
        row.Changed += OnRowChanged;
        Rows.Insert(0, row);
        _newRows.Add(row);
        HasUnsavedChanges = true;
    }

    [RelayCommand]
    private void ConfirmDelete(ProductPriceEditRow? row)
    {
        if (row is null) return;
        RowToDelete = row;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private async Task ExecuteDelete()
    {
        if (RowToDelete is null) return;
        try
        {
            if (RowToDelete.Id > 0)
                await _productPriceService.DeleteAsync(RowToDelete.Id);

            RowToDelete.Changed -= OnRowChanged;
            Rows.Remove(RowToDelete);
            _newRows.Remove(RowToDelete);
            _dirtyIds.Remove(RowToDelete.Id);
            IsDeleteDialogOpen = false;
            RowToDelete = null;
            HasUnsavedChanges = _dirtyIds.Count > 0 || _newRows.Count > 0;
            StatusMessage = "تم الحذف";
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
            IsDeleteDialogOpen = false;
        }
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteDialogOpen = false;
        RowToDelete = null;
    }

    [RelayCommand]
    private async Task SaveAll()
    {
        try
        {
            var toSave = Rows
                .Where(r => r.SelectedProduct is not null && r.SelectedPricingType is not null)
                .Select(r => r.ToEntity())
                .ToList();

            var duplicates = toSave
                .GroupBy(p => (p.ProductId, p.PricingTypeId))
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Count > 0)
            {
                BeautifulMessageDialog.ShowError("لا يمكن تكرار سعر منتج ونوع التسعير لنفس المنتج");
                return;
            }

            foreach (var entity in toSave)
                await _productPriceService.UpsertAsync(entity);

            StatusMessage = "تم حفظ التسعير بنجاح";
            BeautifulMessageDialog.ShowSuccess("تم حفظ تسعير المنتجات");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ExportTemplate()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"قالب_تسعير_منتجات_{DateTime.Now:yyyyMMdd}.xlsx",
                DefaultExt = ".xlsx"
            };
            if (dialog.ShowDialog() != true) return;

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("تسعير منتجات");
            sheet.RightToLeft = true;
            sheet.Cell(1, 1).Value = "اسم المنتج";
            sheet.Cell(1, 2).Value = "الباركود";
            sheet.Cell(1, 3).Value = "نوع التسعير";
            sheet.Cell(1, 4).Value = "سعر البيع";
            sheet.Cell(1, 5).Value = "سعر الشراء";
            sheet.Cell(2, 1).Value = "مثال منتج";
            sheet.Cell(2, 2).Value = "123456";
            sheet.Cell(2, 3).Value = "سعر مفرد";
            sheet.Cell(2, 4).Value = 10000;
            sheet.Cell(2, 5).Value = 8000;
            sheet.Columns().AdjustToContents();
            workbook.SaveAs(dialog.FileName);
            BeautifulMessageDialog.ShowSuccess("تم استخراج قالب Excel");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            var (items, _) = await _productPriceService.GetPagedAsync(1, int.MaxValue, SearchText,
                null, FilterPricingType?.Id, FilterCategory is { Id: > 0 } ? FilterCategory.Id : null,
                ParseDecimal(MinSalePriceText), ParseDecimal(MaxSalePriceText),
                ParseDecimal(MinPurchasePriceText), ParseDecimal(MaxPurchasePriceText));

            var exportData = items.Select(p => new
            {
                المنتج = p.Product?.Name ?? "",
                الباركود = p.Product?.Barcode ?? "",
                نوع_التسعير = p.PricingType?.Name ?? "",
                سعر_البيع = p.SalePrice,
                سعر_الشراء = p.PurchasePrice
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"تسعير_منتجات_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };
            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "تسعير منتجات");
                BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ImportFromExcel()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                Title = "استيراد تسعير منتجات"
            };
            if (dialog.ShowDialog() != true) return;

            var productsByName = Products.ToDictionary(p => p.Name.Trim(), p => p, StringComparer.OrdinalIgnoreCase);
            var productsByBarcode = Products
                .Where(p => !string.IsNullOrWhiteSpace(p.Barcode))
                .GroupBy(p => p.Barcode!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var typesByName = PricingTypes.ToDictionary(t => t.Name.Trim(), t => t, StringComparer.OrdinalIgnoreCase);

            var toSave = new List<ProductPrice>();
            using var workbook = new XLWorkbook(dialog.FileName);
            var sheet = workbook.Worksheets.First();
            var rows = sheet.RangeUsed()?.RowsUsed().Skip(1) ?? Enumerable.Empty<IXLRangeRow>();

            foreach (var excelRow in rows)
            {
                var productName = excelRow.Cell(1).GetString().Trim();
                var barcode = excelRow.Cell(2).GetString().Trim();
                var typeName = excelRow.Cell(3).GetString().Trim();
                if (string.IsNullOrWhiteSpace(typeName))
                    continue;

                Product? product = null;
                if (!string.IsNullOrWhiteSpace(barcode) && productsByBarcode.TryGetValue(barcode, out var byBarcode))
                    product = byBarcode;
                else if (!string.IsNullOrWhiteSpace(productName) && productsByName.TryGetValue(productName, out var byName))
                    product = byName;

                if (product is null || !typesByName.TryGetValue(typeName, out var pricingType))
                    continue;

                var sale = excelRow.Cell(4).TryGetValue(out double saleVal) ? (decimal)saleVal : 0m;
                var purchase = excelRow.Cell(5).TryGetValue(out double purchaseVal) ? (decimal)purchaseVal : 0m;

                toSave.Add(new ProductPrice
                {
                    ProductId = product.Id,
                    PricingTypeId = pricingType.Id,
                    SalePrice = sale,
                    PurchasePrice = purchase
                });
            }

            if (toSave.Count == 0)
            {
                BeautifulMessageDialog.ShowWarning("لم يتم العثور على صفوف صالحة للاستيراد");
                return;
            }

            await _productPriceService.UpsertManyAsync(toSave);
            BeautifulMessageDialog.ShowSuccess($"تم استيراد وحفظ {toSave.Count} سعر");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"فشل الاستيراد: {ex.Message}");
        }
    }
}
