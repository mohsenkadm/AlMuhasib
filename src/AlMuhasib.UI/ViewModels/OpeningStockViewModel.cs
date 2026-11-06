using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using AlMuhasib.Infrastructure.Data;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels;

public partial class OpeningStockViewModel : ViewModelBase
{
    private const int DefaultPageSize = 100;
    private const int UiYieldEvery = 250;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IOpeningStockExcelService _excelService;
    private readonly IProductService _productService;
    private readonly IBranchContext _branchContext;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly List<OpeningStockRow> _allRows = [];
    private readonly Dictionary<int, Product> _productsById = new();
    private Dictionary<string, Product> _productsByName = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, Product> _productsByBarcode = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;
    private bool _suppressDirty;
    private bool _suppressWarehouseReload;
    private bool _isDirty;
    private int? _lastLoadedWarehouseId;
    private Dictionary<int, decimal> _warehouseUnitCosts = new();

    public ObservableCollection<OpeningStockRow> Rows { get; } = [];
    public ObservableCollection<Warehouse> Warehouses { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];

    [ObservableProperty] private Warehouse? _selectedWarehouse;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isSaved;
    [ObservableProperty] private string _importStatusMessage = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = DefaultPageSize;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private string _paginationText = string.Empty;

    // إحصائيات الاستيراد
    [ObservableProperty] private bool _showImportStats;
    [ObservableProperty] private double _importProgress;
    [ObservableProperty] private string _importPhase = string.Empty;
    [ObservableProperty] private int _statTotalRows;
    [ObservableProperty] private int _statUpdated;
    [ObservableProperty] private int _statCreated;
    [ObservableProperty] private int _statSkipped;
    [ObservableProperty] private int _statInvalid;
    [ObservableProperty] private string _statElapsedText = string.Empty;
    [ObservableProperty] private string _busyStatusText = "جارٍ التحميل...";

    public override bool HasUnsavedChanges => _isDirty;
    public override bool SupportsSaveBeforeLeave => true;

    public OpeningStockViewModel(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IOpeningStockExcelService excelService,
        IProductService productService,
        IBranchContext branchContext,
        IDbContextFactory<AppDbContext> dbFactory)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _excelService = excelService;
        _productService = productService;
        _branchContext = branchContext;
        _dbFactory = dbFactory;
        PageTitle = "الأرصدة الافتتاحية للمنتجات";
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        BusyStatusText = "جارٍ تجهيز المخازن والمنتجات...";
        try
        {
            LoadPermissions(_currentUserService, "OpeningStock");

            var warehouses = await _unitOfWork.Warehouses.GetAllAsync();
            Warehouses.Clear();
            foreach (var w in warehouses)
                Warehouses.Add(w);

            await RebuildProductLookupsAsync();
            _initialized = true;

            if (Warehouses.Count > 0)
                SelectedWarehouse = Warehouses[0];
        }
        finally { IsBusy = false; }
    }

    public override async Task<bool> SavePendingChangesAsync()
    {
        await SaveAsync();
        return !_isDirty;
    }

    private async Task RebuildProductLookupsAsync()
    {
        var products = await _productService.GetVisibleInCurrentBranchAsync();
        _productsById.Clear();
        Products.Clear();
        foreach (var p in products.OrderBy(x => x.Name))
        {
            Products.Add(p);
            _productsById[p.Id] = p;
        }

        RebuildNameBarcodeMaps();
    }

    private void RebuildNameBarcodeMaps()
    {
        _productsByName = Products
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .GroupBy(p => p.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        _productsByBarcode = Products
            .Where(p => !string.IsNullOrWhiteSpace(p.Barcode))
            .SelectMany(p => ProductBarcodeHelper.Split(p.Barcode!)
                .Select(token => (Token: token, Product: p)))
            .GroupBy(x => x.Token, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Product, StringComparer.OrdinalIgnoreCase);
    }

    partial void OnSelectedWarehouseChanged(Warehouse? value)
    {
        if (_suppressWarehouseReload || value is null || !_initialized)
            return;

        _ = HandleWarehouseChangedAsync(value);
    }

    private async Task HandleWarehouseChangedAsync(Warehouse warehouse)
    {
        if (_isDirty
            && _lastLoadedWarehouseId is int previousId
            && previousId != warehouse.Id
            && !BeautifulMessageDialog.ShowConfirm(
                "لديك تغييرات غير محفوظة في الأرصدة الافتتاحية.\nالمتابعة ستتجاهل التعديلات الحالية. هل تريد المتابعة؟",
                "تغييرات غير محفوظة"))
        {
            _suppressWarehouseReload = true;
            SelectedWarehouse = Warehouses.FirstOrDefault(w => w.Id == previousId);
            _suppressWarehouseReload = false;
            return;
        }

        await LoadExistingStockAsync();
    }

    /// <summary>
    /// يحمّل فقط المنتجات التي لها رصيد في المخزن — لا ينشئ 3000 صف فارغ.
    /// </summary>
    private async Task LoadExistingStockAsync()
    {
        if (SelectedWarehouse is null) return;
        IsBusy = true;
        BusyStatusText = "جارٍ تحميل الأرصدة الافتتاحية...";
        try
        {
            var stocks = (await _unitOfWork.WarehouseStocks.FindAsync(
                    s => s.WarehouseId == SelectedWarehouse.Id
                         && (s.Quantity != 0 || s.OpeningQuantity != 0)))
                .ToList();

            _warehouseUnitCosts = stocks.ToDictionary(s => s.ProductId, s => s.UnitCost);

            ClearAllRows();
            _suppressDirty = true;

            var missingIds = stocks
                .Select(s => s.ProductId)
                .Where(id => !_productsById.ContainsKey(id))
                .Distinct()
                .ToList();
            if (missingIds.Count > 0)
            {
                foreach (var id in missingIds)
                {
                    var p = await _productService.GetByIdAsync(id);
                    if (p is null) continue;
                    _productsById[p.Id] = p;
                    Products.Add(p);
                }
                RebuildNameBarcodeMaps();
            }

            var i = 0;
            foreach (var stock in stocks.OrderBy(s =>
                         _productsById.TryGetValue(s.ProductId, out var p) ? p.Name : string.Empty))
            {
                if (!_productsById.TryGetValue(stock.ProductId, out var product))
                    continue;

                AddTrackedRow(new OpeningStockRow
                {
                    SelectedProduct = product,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Barcode = product.Barcode,
                    Quantity = stock.OpeningQuantity > 0 ? stock.OpeningQuantity : stock.Quantity,
                    UnitCost = stock.UnitCost
                });

                if (++i % UiYieldEvery == 0)
                    await YieldUiAsync();
            }

            _suppressDirty = false;
            CurrentPage = 1;
            RefreshPagedRows();
            _lastLoadedWarehouseId = SelectedWarehouse.Id;
            ClearDirty();
            ImportStatusMessage = TotalCount > 0
                ? $"عرض {TotalCount:N0} منتج له رصيد في المخزن"
                : "لا أرصدة بعد — استورد Excel أو أضف صفاً";
        }
        finally { IsBusy = false; }
    }

    private static async Task YieldUiAsync()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null) return;
        await dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
    }

    private void RefreshPagedRows()
    {
        TotalCount = _allRows.Count;
        TotalPages = PaginationHelper.ComputeTotalPages(TotalCount, PageSize);
        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;
        if (CurrentPage < 1)
            CurrentPage = 1;

        for (var i = 0; i < _allRows.Count; i++)
            _allRows[i].RowNumber = i + 1;

        Rows.Clear();
        var startIndex = (CurrentPage - 1) * PageSize;
        foreach (var row in _allRows.Skip(startIndex).Take(PageSize))
            Rows.Add(row);

        PaginationText = PaginationHelper.BuildPaginationText(TotalCount, CurrentPage, PageSize);
    }

    private void ClearAllRows()
    {
        foreach (var row in _allRows)
            row.PropertyChanged -= OnRowPropertyChanged;
        _allRows.Clear();
        Rows.Clear();
    }

    private void AddTrackedRow(OpeningStockRow row)
    {
        row.PropertyChanged += OnRowPropertyChanged;
        _allRows.Add(row);
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressDirty) return;
        if (e.PropertyName is nameof(OpeningStockRow.Quantity)
            or nameof(OpeningStockRow.SelectedProduct)
            or nameof(OpeningStockRow.ProductId)
            or nameof(OpeningStockRow.ProductName)
            or nameof(OpeningStockRow.Barcode))
            MarkDirty();
    }

    private void MarkDirty()
    {
        _isDirty = true;
        IsSaved = false;
    }

    private void ClearDirty()
    {
        _isDirty = false;
        IsSaved = true;
    }

    [RelayCommand]
    private void FirstPage()
    {
        CurrentPage = 1;
        RefreshPagedRows();
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentPage <= 1) return;
        CurrentPage--;
        RefreshPagedRows();
    }

    [RelayCommand]
    private void NextPage()
    {
        if (CurrentPage >= TotalPages) return;
        CurrentPage++;
        RefreshPagedRows();
    }

    [RelayCommand]
    private void LastPage()
    {
        CurrentPage = TotalPages;
        RefreshPagedRows();
    }

    [RelayCommand]
    private void AddRow()
    {
        AddTrackedRow(new OpeningStockRow());
        MarkDirty();
        CurrentPage = PaginationHelper.ComputeTotalPages(_allRows.Count, PageSize);
        RefreshPagedRows();
    }

    [RelayCommand]
    private void RemoveRow(OpeningStockRow? row)
    {
        if (row is null) return;
        row.PropertyChanged -= OnRowPropertyChanged;
        _allRows.Remove(row);
        MarkDirty();
        RefreshPagedRows();
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
            var bytes = _excelService.GenerateTemplate(includeProductSalePrice: false);
            await File.WriteAllBytesAsync(dialog.FileName, bytes);
            BeautifulMessageDialog.ShowSuccess(
                "تم تنزيل القالب.\nالأعمدة: اسم المنتج · الباركود (اختياري) · الكمية");
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
        ShowImportStats = true;
        ImportProgress = 0;
        ImportPhase = "قراءة الملف...";
        var sw = Stopwatch.StartNew();
        try
        {
            var filePath = dialog.FileName;
            var importRows = await Task.Run(() => _excelService.ParseImportFile(filePath, false));
            if (importRows.Count == 0)
            {
                BeautifulMessageDialog.ShowWarning("الملف لا يحتوي على بيانات");
                return;
            }

            StatTotalRows = importRows.Count;
            StatInvalid = importRows.Count(r => !r.IsValid);
            var validRows = importRows.Where(r => r.IsValid).ToList();
            if (validRows.Count == 0)
            {
                BeautifulMessageDialog.ShowWarning("لا توجد أسطر صالحة للاستيراد — راجع بيانات الملف");
                return;
            }

            if (StatInvalid > 0
                && !BeautifulMessageDialog.ShowConfirm(
                    $"يوجد {StatInvalid:N0} سطر غير صالح.\nاستيراد الصالحة فقط ({validRows.Count:N0})؟"))
                return;

            ImportPhase = "مطابقة وإنشاء المنتجات...";
            ImportProgress = 8;

            var result = await ApplyImportAsync(validRows, progress =>
            {
                ImportProgress = Math.Clamp(progress, 0, 100);
            });

            StatUpdated = result.Updated;
            StatCreated = result.Created;
            StatSkipped = result.Skipped;
            sw.Stop();
            StatElapsedText = sw.Elapsed.TotalSeconds < 10
                ? $"{sw.Elapsed.TotalSeconds:0.0} ث"
                : $"{sw.Elapsed.TotalSeconds:0} ث";
            ImportProgress = 100;
            ImportPhase = "اكتمل";

            ImportStatusMessage =
                $"تم: تحديث {result.Updated:N0} · إنشاء {result.Created:N0}"
                + (result.Skipped > 0 ? $" · تُجوّز {result.Skipped:N0}" : string.Empty)
                + $" — خلال {StatElapsedText}";

            BeautifulMessageDialog.ShowSuccess(ImportStatusMessage);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"فشل الاستيراد: {ex.Message}";
            ImportPhase = "فشل";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<(int Updated, int Created, int Skipped)> ApplyImportAsync(
        IReadOnlyList<OpeningStockImportRow> validRows,
        Action<double> reportProgress)
    {
        var updated = 0;
        var created = 0;
        var skipped = 0;
        var warehouse = SelectedWarehouse!;
        var username = _currentUserService.Username;
        var branchId = warehouse.BranchId > 0
            ? warehouse.BranchId
            : _branchContext.CurrentBranchId ?? 0;

        var categoryId = await EnsureDefaultCategoryIdAsync();
        var rowByProductId = _allRows.Where(r => r.ProductId > 0).ToDictionary(r => r.ProductId);
        var pendingNewProducts = new List<(OpeningStockImportRow Import, Product Product)>();
        var pendingByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var qtyByProductId = new Dictionary<int, (decimal Qty, decimal? UnitCost)>();
        var matchedExistingIds = new HashSet<int>();

        // مرحلة 1: مطابقة / تجهيز منتجات جديدة
        for (var i = 0; i < validRows.Count; i++)
        {
            var importRow = validRows[i];
            Product? product = null;

            if (!string.IsNullOrWhiteSpace(importRow.Barcode))
            {
                foreach (var token in ProductBarcodeHelper.Split(importRow.Barcode))
                {
                    if (_productsByBarcode.TryGetValue(token, out var byBarcode))
                    {
                        product = byBarcode;
                        break;
                    }
                }
            }

            if (product is null
                && !string.IsNullOrWhiteSpace(importRow.ProductName)
                && _productsByName.TryGetValue(importRow.ProductName.Trim(), out var byName))
                product = byName;

            if (product is not null)
            {
                qtyByProductId[product.Id] = (importRow.Quantity, importRow.UnitCost > 0 ? importRow.UnitCost : null);
                matchedExistingIds.Add(product.Id);
            }
            else if (!string.IsNullOrWhiteSpace(importRow.ProductName))
            {
                var nameKey = importRow.ProductName.Trim();
                if (pendingByName.TryGetValue(nameKey, out var pendingIndex))
                {
                    // نفس الاسم أكثر من مرة في الملف — اعتمد آخر كمية
                    var existing = pendingNewProducts[pendingIndex];
                    pendingNewProducts[pendingIndex] = (importRow, existing.Product);
                }
                else
                {
                    var barcode = ProductBarcodeHelper.Normalize(importRow.Barcode);
                    var newProduct = new Product
                    {
                        Name = nameKey,
                        Barcode = barcode,
                        CategoryId = categoryId,
                        ShowInCashier = true,
                        CreatedBy = username,
                        CreatedAt = DateTime.UtcNow
                    };
                    pendingByName[nameKey] = pendingNewProducts.Count;
                    pendingNewProducts.Add((importRow, newProduct));
                }
            }
            else
            {
                skipped++;
            }

            if (i % UiYieldEvery == 0)
            {
                reportProgress(8 + 25.0 * (i + 1) / validRows.Count);
                await YieldUiAsync();
            }
        }

        updated = matchedExistingIds.Count;

        // مرحلة 2: إنشاء المنتجات الجديدة دفعة واحدة
        ImportPhase = pendingNewProducts.Count > 0
            ? $"إنشاء {pendingNewProducts.Count:N0} منتج جديد..."
            : "تحديث الكميات...";
        reportProgress(35);

        if (pendingNewProducts.Count > 0)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            db.BypassBranchFilter = true;

            foreach (var (_, product) in pendingNewProducts)
                await db.Products.AddAsync(product);
            await db.SaveChangesAsync();

            if (branchId > 0)
            {
                var productIds = pendingNewProducts.Select(x => x.Product.Id).ToList();
                var existingLinks = await db.ProductBranches
                    .Where(pb => productIds.Contains(pb.ProductId) && pb.BranchId == branchId)
                    .Select(pb => pb.ProductId)
                    .ToListAsync();
                var linked = existingLinks.ToHashSet();
                foreach (var id in productIds.Where(id => !linked.Contains(id)))
                {
                    db.ProductBranches.Add(new ProductBranch
                    {
                        ProductId = id,
                        BranchId = branchId,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                await db.SaveChangesAsync();
            }

            foreach (var (importRow, product) in pendingNewProducts)
            {
                _productsById[product.Id] = product;
                Products.Add(product);
                qtyByProductId[product.Id] = (importRow.Quantity, importRow.UnitCost > 0 ? importRow.UnitCost : null);
            }

            created = pendingNewProducts.Count;
            RebuildNameBarcodeMaps();
        }

        reportProgress(55);
        ImportPhase = "تحديث صفوف الجدول...";

        // مرحلة 3: تحديث/إضافة صفوف الواجهة بدون تجميد
        _suppressDirty = true;
        var applied = 0;
        foreach (var (productId, (qty, unitCostOpt)) in qtyByProductId)
        {
            if (!_productsById.TryGetValue(productId, out var product))
                continue;

            var unitCost = unitCostOpt
                ?? (_warehouseUnitCosts.TryGetValue(productId, out var existingCost) ? existingCost : 0m);

            if (rowByProductId.TryGetValue(productId, out var existingRow))
            {
                existingRow.Quantity = qty;
                if (unitCostOpt is > 0)
                    existingRow.UnitCost = unitCost;
                existingRow.Barcode = product.Barcode;
            }
            else
            {
                var newRow = new OpeningStockRow
                {
                    SelectedProduct = product,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Barcode = product.Barcode,
                    Quantity = qty,
                    UnitCost = unitCost
                };
                AddTrackedRow(newRow);
                rowByProductId[productId] = newRow;
            }

            if (++applied % UiYieldEvery == 0)
            {
                reportProgress(55 + 20.0 * applied / qtyByProductId.Count);
                await YieldUiAsync();
            }
        }
        _suppressDirty = false;

        reportProgress(78);
        ImportPhase = "حفظ الأرصدة في المخزن...";
        await PersistStocksBulkAsync(
            warehouse.Id,
            branchId,
            qtyByProductId.ToDictionary(
                kv => kv.Key,
                kv =>
                {
                    var unit = kv.Value.UnitCost
                        ?? (_warehouseUnitCosts.TryGetValue(kv.Key, out var c) ? c : 0m);
                    return (kv.Value.Qty, unit);
                }));

        reportProgress(95);
        CurrentPage = 1;
        RefreshPagedRows();
        ClearDirty();
        return (updated, created, skipped);
    }

    private async Task<int> EnsureDefaultCategoryIdAsync()
    {
        var categories = (await _unitOfWork.Categories.GetAllAsync()).ToList();
        var existing = categories.FirstOrDefault(c =>
            string.Equals(c.Name, "عام", StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            return existing.Id;

        var cat = new Category { Name = "عام", CreatedAt = DateTime.UtcNow, CreatedBy = _currentUserService.Username };
        await _unitOfWork.Categories.AddAsync(cat);
        await _unitOfWork.SaveChangesAsync();
        return cat.Id;
    }

    private async Task PersistStocksBulkAsync(
        int warehouseId,
        int branchId,
        IReadOnlyDictionary<int, (decimal Qty, decimal UnitCost)> byProduct)
    {
        if (byProduct.Count == 0) return;

        var username = _currentUserService.Username;
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.BypassBranchFilter = true;

        var productIds = byProduct.Keys.ToList();
        var existing = await db.WarehouseStocks.IgnoreQueryFilters()
            .Where(s => !s.IsDeleted
                        && s.WarehouseId == warehouseId
                        && productIds.Contains(s.ProductId))
            .ToListAsync();
        var byId = existing.ToDictionary(s => s.ProductId);
        var resolvedBranch = branchId > 0
            ? branchId
            : existing.FirstOrDefault()?.BranchId
              ?? _branchContext.CurrentBranchId
              ?? 0;

        foreach (var (productId, (qty, unitCost)) in byProduct)
        {
            if (byId.TryGetValue(productId, out var stock))
            {
                stock.Quantity = qty;
                stock.OpeningQuantity = qty;
                if (unitCost > 0)
                    stock.UnitCost = unitCost;
                if (resolvedBranch > 0)
                    stock.BranchId = resolvedBranch;
                stock.UpdatedBy = username;
                stock.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var created = new WarehouseStock
                {
                    WarehouseId = warehouseId,
                    ProductId = productId,
                    BranchId = resolvedBranch > 0 ? resolvedBranch : AppDbContext.UnsetBranchId,
                    Quantity = qty,
                    OpeningQuantity = qty,
                    UnitCost = unitCost,
                    CreatedBy = username,
                    CreatedAt = DateTime.UtcNow
                };
                db.WarehouseStocks.Add(created);
            }

            _warehouseUnitCosts[productId] = unitCost > 0
                ? unitCost
                : (_warehouseUnitCosts.TryGetValue(productId, out var prev) ? prev : 0m);
        }

        await db.SaveChangesAsync();
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

        var validRows = _allRows.Where(r => r.ProductId > 0 && r.Quantity > 0).ToList();
        if (validRows.Count == 0)
        {
            ErrorMessage = "لا توجد بيانات للحفظ — أدخل كمية أكبر من صفر";
            return;
        }

        if (validRows.GroupBy(r => r.ProductId).Any(g => g.Count() > 1))
        {
            ErrorMessage = "لا يمكن تكرار نفس المنتج في أكثر من صف";
            return;
        }

        IsBusy = true;
        BusyStatusText = $"جارٍ حفظ {validRows.Count:N0} رصيد...";
        try
        {
            var branchId = SelectedWarehouse.BranchId > 0
                ? SelectedWarehouse.BranchId
                : _branchContext.CurrentBranchId ?? 0;

            await PersistStocksBulkAsync(
                SelectedWarehouse.Id,
                branchId,
                validRows.ToDictionary(
                    r => r.ProductId,
                    r => (r.Quantity, r.UnitCost > 0
                        ? r.UnitCost
                        : (_warehouseUnitCosts.TryGetValue(r.ProductId, out var c) ? c : 0m))));

            ClearDirty();
            BeautifulMessageDialog.ShowSuccess($"تم حفظ {validRows.Count:N0} رصيد افتتاحي بنجاح");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"حدث خطأ: {ex.Message}";
        }
        finally { IsBusy = false; }
    }
}

public partial class OpeningStockRow : ObservableObject
{
    [ObservableProperty] private int _rowNumber;
    [ObservableProperty] private int _productId;
    [ObservableProperty] private string _productName = string.Empty;
    [ObservableProperty] private string? _barcode;
    [ObservableProperty] private decimal _quantity;
    [ObservableProperty] private decimal _unitCost;
    [ObservableProperty] private Product? _selectedProduct;

    partial void OnSelectedProductChanged(Product? value)
    {
        if (value is null)
        {
            ProductId = 0;
            ProductName = string.Empty;
            Barcode = null;
            return;
        }

        ProductId = value.Id;
        ProductName = value.Name;
        Barcode = value.Barcode;
    }

    /// <summary>Alias لقالب خلية المنتج في الفواتير.</summary>
    public string ItemName
    {
        get => ProductName;
        set => ProductName = value;
    }
}
