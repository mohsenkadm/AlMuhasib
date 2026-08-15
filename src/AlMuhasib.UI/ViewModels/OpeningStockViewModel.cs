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

public partial class OpeningStockViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IOpeningStockExcelService _excelService;
    private readonly IUserPreferencesService _userPreferences;
    private readonly IProductPriceService _productPriceService;
    private readonly IPricingTypeService _pricingTypeService;
    private bool _initialized;
    private bool _suppressPricingTypeReload;
    private Dictionary<int, decimal> _warehouseUnitCosts = new();
    private Dictionary<(int ProductId, int PricingTypeId), ProductPrice> _pricesByProductAndType = new();

    public ObservableCollection<OpeningStockRow> Rows { get; } = [];
    public ObservableCollection<Warehouse> Warehouses { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<PricingType> PricingTypes { get; } = [];

    [ObservableProperty]
    private Warehouse? _selectedWarehouse;

    [ObservableProperty]
    private PricingType? _selectedPricingType;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isSaved;

    [ObservableProperty]
    private string _importStatusMessage = string.Empty;

    public bool ProductPricingEnabled => _userPreferences.Current.FeatureFlags.ProductPricingEnabled;

    public OpeningStockViewModel(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IOpeningStockExcelService excelService,
        IUserPreferencesService userPreferences,
        IProductPriceService productPriceService,
        IPricingTypeService pricingTypeService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _excelService = excelService;
        _userPreferences = userPreferences;
        _productPriceService = productPriceService;
        _pricingTypeService = pricingTypeService;
        PageTitle = "الأرصدة الافتتاحية للمنتجات";
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "OpeningStock");

            var warehouses = await _unitOfWork.Warehouses.GetAllAsync();
            Warehouses.Clear();
            foreach (var w in warehouses)
                Warehouses.Add(w);

            var products = await _unitOfWork.Products.GetAllAsync();
            Products.Clear();
            foreach (var p in products)
                Products.Add(p);

            await EnsurePricingTypesLoadedAsync();

            _initialized = true;

            if (Warehouses.Count > 0)
                SelectedWarehouse = Warehouses[0];
        }
        finally { IsBusy = false; }
    }

    private async Task EnsurePricingTypesLoadedAsync()
    {
        PricingTypes.Clear();
        if (!ProductPricingEnabled)
        {
            _suppressPricingTypeReload = true;
            SelectedPricingType = null;
            _suppressPricingTypeReload = false;
            return;
        }

        await _pricingTypeService.EnsureDefaultExistsAsync();
        var types = await _pricingTypeService.GetActiveAsync();
        foreach (var type in types)
            PricingTypes.Add(type);

        _suppressPricingTypeReload = true;
        SelectedPricingType = types.FirstOrDefault(t => t.IsDefault)
            ?? types.FirstOrDefault(t => t.Name == "سعر مفرد")
            ?? types.FirstOrDefault();
        _suppressPricingTypeReload = false;

        await ReloadProductPricesCacheAsync();
    }

    private async Task ReloadProductPricesCacheAsync()
    {
        _pricesByProductAndType.Clear();
        if (!ProductPricingEnabled || Products.Count == 0)
            return;

        var prices = await _productPriceService.GetByProductIdsAsync(Products.Select(p => p.Id));
        foreach (var price in prices)
            _pricesByProductAndType[(price.ProductId, price.PricingTypeId)] = price;
    }

    partial void OnSelectedWarehouseChanged(Warehouse? value)
    {
        if (value is not null && _initialized)
            _ = LoadExistingStockAsync();
    }

    partial void OnSelectedPricingTypeChanged(PricingType? value)
    {
        if (_suppressPricingTypeReload || !_initialized || !ProductPricingEnabled)
            return;

        ApplyUnitCostsForSelectedPricingType();
    }

    private async Task LoadExistingStockAsync()
    {
        if (SelectedWarehouse is null || Products.Count == 0) return;
        IsBusy = true;
        try
        {
            var stocks = await _unitOfWork.WarehouseStocks.FindAsync(
                s => s.WarehouseId == SelectedWarehouse.Id);

            _warehouseUnitCosts = stocks.ToDictionary(s => s.ProductId, s => s.UnitCost);
            var stockDetails = stocks.ToDictionary(s => s.ProductId);

            if (ProductPricingEnabled)
                await ReloadProductPricesCacheAsync();

            Rows.Clear();
            foreach (var product in Products)
            {
                stockDetails.TryGetValue(product.Id, out var stock);
                Rows.Add(new OpeningStockRow
                {
                    SelectedProduct = product,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = stock?.Quantity ?? 0,
                    UnitCost = ResolveDisplayUnitCost(product.Id, stock?.UnitCost ?? 0)
                });
            }

            IsSaved = false;
        }
        finally { IsBusy = false; }
    }

    private decimal ResolveDisplayUnitCost(int productId, decimal warehouseUnitCost)
    {
        if (ProductPricingEnabled
            && SelectedPricingType is not null
            && _pricesByProductAndType.TryGetValue((productId, SelectedPricingType.Id), out var price)
            && price.PurchasePrice > 0)
        {
            return price.PurchasePrice;
        }

        return warehouseUnitCost;
    }

    private void ApplyUnitCostsForSelectedPricingType()
    {
        foreach (var row in Rows.Where(r => r.ProductId > 0))
        {
            _warehouseUnitCosts.TryGetValue(row.ProductId, out var warehouseCost);
            row.UnitCost = ResolveDisplayUnitCost(row.ProductId, warehouseCost);
        }

        IsSaved = false;
    }

    [RelayCommand]
    private void AddRow()
    {
        Rows.Add(new OpeningStockRow());
    }

    [RelayCommand]
    private void RemoveRow(OpeningStockRow? row)
    {
        if (row is not null)
            Rows.Remove(row);
    }

    [RelayCommand]
    private async Task DownloadTemplateAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Excel|*.xlsx",
            FileName = "قالب_أرصدة_المنتجات_الافتتاحية.xlsx",
            Title = "حفظ قالب Excel"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var bytes = _excelService.GenerateTemplate(ProductPricingEnabled);
            await File.WriteAllBytesAsync(dialog.FileName, bytes);
            BeautifulMessageDialog.ShowSuccess("تم تنزيل القالب بنجاح.\nاملأ ورقة «البيانات» ثم استورد الملف.");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطأ في التنزيل: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ImportFromExcelAsync()
    {
        ErrorMessage = string.Empty;
        ImportStatusMessage = string.Empty;

        if (SelectedWarehouse is null)
        {
            ErrorMessage = "يرجى اختيار المخزن قبل الاستيراد";
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "Excel|*.xlsx;*.xls",
            Title = "استيراد أرصدة منتجات افتتاحية"
        };
        if (dialog.ShowDialog() != true) return;

        IsBusy = true;
        try
        {
            var includePricing = ProductPricingEnabled;
            var importRows = _excelService.ParseImportFile(dialog.FileName, includePricing);
            if (importRows.Count == 0)
            {
                BeautifulMessageDialog.ShowWarning("الملف لا يحتوي على بيانات");
                return;
            }

            var validRows = importRows.Where(r => r.IsValid).ToList();
            var invalidCount = importRows.Count - validRows.Count;
            if (validRows.Count == 0)
            {
                BeautifulMessageDialog.ShowWarning("لا توجد أسطر صالحة للاستيراد — راجع بيانات الملف");
                return;
            }

            if (invalidCount > 0)
            {
                var proceed = BeautifulMessageDialog.ShowConfirm(
                    $"يوجد {invalidCount} سطر غير صالح.\nهل تريد استيراد الأسطر الصالحة فقط ({validRows.Count})؟");
                if (!proceed)
                    return;
            }

            var productsByName = Products.ToDictionary(p => p.Name.Trim(), p => p, StringComparer.OrdinalIgnoreCase);
            var productsByBarcode = Products
                .Where(p => !string.IsNullOrWhiteSpace(p.Barcode))
                .GroupBy(p => p.Barcode!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var applied = 0;
            var unmatched = 0;
            var pricesToSave = new List<ProductPrice>();
            int? pricingTypeId = null;

            if (includePricing)
            {
                await _pricingTypeService.EnsureDefaultExistsAsync();
                if (SelectedPricingType is null && PricingTypes.Count == 0)
                    await EnsurePricingTypesLoadedAsync();

                pricingTypeId = SelectedPricingType?.Id
                    ?? PricingTypes.FirstOrDefault(t => t.IsDefault)?.Id
                    ?? PricingTypes.FirstOrDefault(t => t.Name == "سعر مفرد")?.Id
                    ?? (await _pricingTypeService.GetActiveAsync()).FirstOrDefault()?.Id;
            }

            var existingPricesByProduct = new Dictionary<int, ProductPrice>();
            if (pricingTypeId is int pricingTypeIdForLookup)
            {
                var existingPrices = await _productPriceService.GetByProductIdsAsync(Products.Select(p => p.Id));
                foreach (var price in existingPrices.Where(p => p.PricingTypeId == pricingTypeIdForLookup))
                    existingPricesByProduct[price.ProductId] = price;
            }

            var rowByProductId = Rows.Where(r => r.ProductId > 0).ToDictionary(r => r.ProductId);

            foreach (var importRow in validRows)
            {
                Product? product = null;
                if (!string.IsNullOrWhiteSpace(importRow.Barcode)
                    && productsByBarcode.TryGetValue(importRow.Barcode, out var byBarcode))
                    product = byBarcode;
                else if (!string.IsNullOrWhiteSpace(importRow.ProductName)
                         && productsByName.TryGetValue(importRow.ProductName.Trim(), out var byName))
                    product = byName;

                if (product is null)
                {
                    unmatched++;
                    continue;
                }

                if (rowByProductId.TryGetValue(product.Id, out var existingRow))
                {
                    existingRow.Quantity = importRow.Quantity;
                    existingRow.UnitCost = importRow.UnitCost;
                }
                else
                {
                    var newRow = new OpeningStockRow
                    {
                        SelectedProduct = product,
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = importRow.Quantity,
                        UnitCost = importRow.UnitCost
                    };
                    Rows.Add(newRow);
                    rowByProductId[product.Id] = newRow;
                }

                if (includePricing && pricingTypeId is int typeId && importRow.UnitCost > 0)
                {
                    existingPricesByProduct.TryGetValue(product.Id, out var existingPrice);
                    var salePrice = importRow.ProductSalePrice
                        ?? existingPrice?.SalePrice
                        ?? 0m;
                    pricesToSave.Add(new ProductPrice
                    {
                        ProductId = product.Id,
                        PricingTypeId = typeId,
                        SalePrice = salePrice,
                        PurchasePrice = importRow.UnitCost
                    });
                }
                else if (includePricing
                         && importRow.ProductSalePrice.HasValue
                         && pricingTypeId is int saleTypeId)
                {
                    existingPricesByProduct.TryGetValue(product.Id, out var existingPrice);
                    pricesToSave.Add(new ProductPrice
                    {
                        ProductId = product.Id,
                        PricingTypeId = saleTypeId,
                        SalePrice = importRow.ProductSalePrice.Value,
                        PurchasePrice = existingPrice?.PurchasePrice ?? 0
                    });
                }

                applied++;
            }

            if (pricesToSave.Count > 0)
                await _productPriceService.UpsertManyAsync(pricesToSave);

            await SaveImportedRowsAsync();

            if (includePricing)
                await ReloadProductPricesCacheAsync();

            ImportStatusMessage = unmatched > 0
                ? $"تم استيراد {applied} منتج — لم يُطابق {unmatched}"
                : $"تم استيراد {applied} منتج بنجاح";
            BeautifulMessageDialog.ShowSuccess(ImportStatusMessage);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"فشل الاستيراد: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    private async Task SaveImportedRowsAsync()
    {
        if (SelectedWarehouse is null) return;

        var validRows = Rows.Where(r => r.ProductId > 0 && r.Quantity > 0 && r.UnitCost > 0).ToList();
        var username = _currentUserService.Username;

        foreach (var row in validRows)
        {
            var existing = (await _unitOfWork.WarehouseStocks.FindAsync(
                s => s.WarehouseId == SelectedWarehouse.Id && s.ProductId == row.ProductId))
                .FirstOrDefault();

            if (existing is not null)
            {
                existing.Quantity = row.Quantity;
                existing.OpeningQuantity = row.Quantity;
                existing.UnitCost = row.UnitCost;
                existing.UpdatedBy = username;
                existing.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.WarehouseStocks.Update(existing);
            }
            else
            {
                await _unitOfWork.WarehouseStocks.AddAsync(new WarehouseStock
                {
                    WarehouseId = SelectedWarehouse.Id,
                    ProductId = row.ProductId,
                    Quantity = row.Quantity,
                    OpeningQuantity = row.Quantity,
                    UnitCost = row.UnitCost,
                    CreatedBy = username,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _warehouseUnitCosts[row.ProductId] = row.UnitCost;
        }

        await _unitOfWork.SaveChangesAsync();
        IsSaved = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        if (SelectedWarehouse is null)
        {
            ErrorMessage = "يرجى اختيار المخزن";
            return;
        }

        if (ProductPricingEnabled && SelectedPricingType is null)
        {
            ErrorMessage = "يرجى اختيار نوع التسعير";
            return;
        }

        var validRows = Rows.Where(r => r.ProductId > 0 && r.Quantity > 0).ToList();
        if (validRows.Count == 0)
        {
            ErrorMessage = "لا توجد بيانات للحفظ";
            return;
        }

        if (validRows.GroupBy(r => r.ProductId).Any(g => g.Count() > 1))
        {
            ErrorMessage = "لا يمكن تكرار نفس المنتج في أكثر من صف";
            return;
        }

        if (validRows.Any(r => r.Quantity > 0 && r.UnitCost <= 0 && r.TotalCost <= 0))
        {
            ErrorMessage = "يرجى إدخال سعر الوحدة أو الإجمالي الكلي لكل منتج له رصيد افتتاحي";
            return;
        }

        foreach (var row in validRows.Where(r => r.UnitCost <= 0 && r.TotalCost > 0 && r.Quantity > 0))
            row.UnitCost = row.TotalCost / row.Quantity;

        if (validRows.Any(r => r.UnitCost <= 0))
        {
            ErrorMessage = "يرجى إدخال سعر الوحدة أو الإجمالي الكلي لكل منتج له رصيد افتتاحي";
            return;
        }

        IsBusy = true;
        try
        {
            var username = _currentUserService.Username;

            foreach (var row in validRows)
            {
                var existing = (await _unitOfWork.WarehouseStocks.FindAsync(
                    s => s.WarehouseId == SelectedWarehouse.Id && s.ProductId == row.ProductId))
                    .FirstOrDefault();

                if (existing is not null)
                {
                    existing.Quantity = row.Quantity;
                    existing.OpeningQuantity = row.Quantity;
                    existing.UnitCost = row.UnitCost;
                    existing.UpdatedBy = username;
                    existing.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.WarehouseStocks.Update(existing);
                }
                else
                {
                    await _unitOfWork.WarehouseStocks.AddAsync(new WarehouseStock
                    {
                        WarehouseId = SelectedWarehouse.Id,
                        ProductId = row.ProductId,
                        Quantity = row.Quantity,
                        OpeningQuantity = row.Quantity,
                        UnitCost = row.UnitCost,
                        CreatedBy = username,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                _warehouseUnitCosts[row.ProductId] = row.UnitCost;
            }

            await _unitOfWork.SaveChangesAsync();

            if (ProductPricingEnabled && SelectedPricingType is not null)
                await SavePurchasePricesForSelectedTypeAsync(validRows);

            IsSaved = true;
            BeautifulMessageDialog.ShowSuccess("تم حفظ الأرصدة الافتتاحية بنجاح");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"حدث خطأ: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    private async Task SavePurchasePricesForSelectedTypeAsync(IReadOnlyList<OpeningStockRow> validRows)
    {
        if (SelectedPricingType is null) return;

        await ReloadProductPricesCacheAsync();

        var pricesToSave = new List<ProductPrice>();
        foreach (var row in validRows)
        {
            _pricesByProductAndType.TryGetValue((row.ProductId, SelectedPricingType.Id), out var existing);
            pricesToSave.Add(new ProductPrice
            {
                ProductId = row.ProductId,
                PricingTypeId = SelectedPricingType.Id,
                PurchasePrice = row.UnitCost,
                SalePrice = existing?.SalePrice ?? 0m
            });
        }

        if (pricesToSave.Count == 0)
            return;

        await _productPriceService.UpsertManyAsync(pricesToSave);
        await ReloadProductPricesCacheAsync();
    }
}

public partial class OpeningStockRow : ObservableObject
{
    [ObservableProperty] private int _productId;
    [ObservableProperty] private string _productName = string.Empty;
    [ObservableProperty] private decimal _quantity;
    [ObservableProperty] private decimal _unitCost;
    [ObservableProperty] private decimal _totalCost;
    [ObservableProperty] private Product? _selectedProduct;

    private bool _isManualTotal;
    private bool _isRecalculating;

    partial void OnSelectedProductChanged(Product? value)
    {
        if (value is null)
        {
            ProductId = 0;
            ProductName = string.Empty;
            return;
        }

        ProductId = value.Id;
        ProductName = value.Name;
    }

    /// <summary>Alias for invoice product cell template compatibility.</summary>
    public string ItemName
    {
        get => ProductName;
        set => ProductName = value;
    }

    partial void OnQuantityChanged(decimal value)
    {
        if (_isRecalculating) return;
        if (_isManualTotal)
            RecalcUnitCost();
        else
            RecalcTotal();
    }

    partial void OnUnitCostChanged(decimal value)
    {
        if (_isRecalculating) return;
        _isManualTotal = false;
        RecalcTotal();
    }

    partial void OnTotalCostChanged(decimal oldValue, decimal newValue)
    {
        if (_isRecalculating) return;
        _isManualTotal = true;
        RecalcUnitCost();
    }

    private void RecalcTotal()
    {
        if (_isManualTotal) return;
        _isRecalculating = true;
        TotalCost = Quantity * UnitCost;
        _isRecalculating = false;
    }

    private void RecalcUnitCost()
    {
        if (Quantity <= 0) return;
        _isRecalculating = true;
        UnitCost = TotalCost / Quantity;
        _isRecalculating = false;
    }
}
