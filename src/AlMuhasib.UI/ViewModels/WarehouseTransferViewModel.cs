using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace AlMuhasib.UI.ViewModels;

public partial class WarehouseTransferViewModel : ViewModelBase, IProductQuickSearchHost
{
    private readonly IWarehouseTransferService _transferService;
    private readonly IUserPreferencesService _preferences;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductService _productService;
    private Dictionary<int, decimal> _fromStockByProduct = new();

    public ObservableCollection<WarehouseTransferWarehouseOption> Warehouses { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<TransferLineItem> Lines { get; } = [];

    public ProductQuickSearchCatalog QuickSearchCatalog { get; }

    [ObservableProperty] private WarehouseTransferWarehouseOption? _fromWarehouse;
    [ObservableProperty] private WarehouseTransferWarehouseOption? _toWarehouse;
    [ObservableProperty] private DateTime _transferDate = DateTime.Today;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private string _barcodeInput = string.Empty;

    [ObservableProperty] private string _linesCountText = "0";
    [ObservableProperty] private string _totalQuantityText = "0";
    [ObservableProperty] private string _distinctProductsText = "0";
    [ObservableProperty] private string _draftStatusText = "فارغ";

    public WarehouseTransferViewModel(
        IWarehouseTransferService transferService,
        IUserPreferencesService preferences,
        ICurrentUserService currentUserService,
        IDbContextFactory<AppDbContext> dbFactory,
        IUnitOfWork unitOfWork,
        IProductPriceService productPriceService,
        IProductService productService)
    {
        _transferService = transferService;
        _preferences = preferences;
        _dbFactory = dbFactory;
        _unitOfWork = unitOfWork;
        _productService = productService;
        QuickSearchCatalog = new ProductQuickSearchCatalog(_unitOfWork, productPriceService);
        PageTitle = "نقل بين مخازن";
        LoadPermissions(currentUserService, "Warehouses");
        Lines.CollectionChanged += OnLinesCollectionChanged;
    }

    public override async Task InitializeAsync()
    {
        if (!_preferences.Current.FeatureFlags.WarehouseTransfers)
        {
            BeautifulMessageDialog.ShowWarning("فعّل «نقل بين مخازن» من إعدادات الميزات");
            return;
        }

        Warehouses.Clear();
        Products.Clear();
        Lines.Clear();

        await using var db = await _dbFactory.CreateDbContextAsync();
        db.BypassBranchFilter = true;
        var warehouses = await db.Warehouses.AsNoTracking()
            .Include(w => w.Branch)
            .Where(w => !w.IsDeleted)
            .OrderBy(w => w.Branch!.Name)
            .ThenBy(w => w.Name)
            .ToListAsync();

        foreach (var w in warehouses)
        {
            Warehouses.Add(new WarehouseTransferWarehouseOption
            {
                Warehouse = w,
                DisplayName = $"{w.Name} — {w.Branch?.Name ?? $"فرع {w.BranchId}"}"
            });
        }

        var products = (await _productService.GetVisibleInCurrentBranchAsync())
            .OrderBy(p => p.Name)
            .ToList();
        foreach (var p in products)
            Products.Add(p);

        await QuickSearchCatalog.LoadAsync(products, InvoicePickerMode.Sale, pricingEnabled: false);

        TransferDate = DateTime.Today;
        EnsureStarterRow();
        RefreshDraftStats();
    }

    partial void OnFromWarehouseChanged(WarehouseTransferWarehouseOption? value)
    {
        _ = ReloadFromStockAsync();
    }

    private async Task ReloadFromStockAsync()
    {
        _fromStockByProduct = new Dictionary<int, decimal>();
        if (FromWarehouse is null)
        {
            foreach (var line in Lines)
                ApplyAvailable(line);
            return;
        }

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            db.BypassBranchFilter = true;
            var stocks = await db.WarehouseStocks.AsNoTracking()
                .Where(s => s.WarehouseId == FromWarehouse.Id && !s.IsDeleted)
                .Select(s => new { s.ProductId, s.Quantity })
                .ToListAsync();

            foreach (var s in stocks)
                _fromStockByProduct[s.ProductId] = s.Quantity;
        }
        catch
        {
            _fromStockByProduct = new Dictionary<int, decimal>();
        }

        foreach (var line in Lines)
            ApplyAvailable(line);
    }

    private void ApplyAvailable(TransferLineItem line)
    {
        if (line.ProductId <= 0)
        {
            line.AvailableQuantity = 0;
            line.AvailableQuantityText = "—";
            line.StockInfo = string.Empty;
            return;
        }

        _fromStockByProduct.TryGetValue(line.ProductId, out var qty);
        line.AvailableQuantity = qty;
        line.AvailableQuantityText = qty.ToString("0.##");
        line.StockInfo = FromWarehouse is null
            ? string.Empty
            : $"متاح: {qty:0.##}";
    }

    private void OnLinesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (TransferLineItem item in e.OldItems)
                item.PropertyChanged -= OnLinePropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (TransferLineItem item in e.NewItems)
            {
                item.PropertyChanged += OnLinePropertyChanged;
                ApplyAvailable(item);
            }
        }

        RenumberLines();
        RefreshDraftStats();
    }

    private void OnLinePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TransferLineItem line)
            return;

        if (e.PropertyName is nameof(TransferLineItem.SelectedProduct)
            or nameof(TransferLineItem.ProductId))
        {
            ApplyAvailable(line);
            RefreshDraftStats();
            return;
        }

        if (e.PropertyName is nameof(TransferLineItem.Quantity))
            RefreshDraftStats();
    }

    private void RenumberLines()
    {
        for (var i = 0; i < Lines.Count; i++)
            Lines[i].RowNumber = i + 1;
    }

    private void EnsureStarterRow()
    {
        if (Lines.Count == 0)
            Lines.Add(new TransferLineItem { Quantity = 1 });
    }

    private IEnumerable<TransferLineItem> ValidLines() =>
        Lines.Where(l => l.ProductId > 0 && l.SelectedProduct is not null);

    private void RefreshDraftStats()
    {
        var valid = ValidLines().ToList();
        var count = valid.Count;
        var totalQty = valid.Sum(l => l.Quantity);
        LinesCountText = count.ToString("N0");
        TotalQuantityText = totalQty.ToString("N2");
        DistinctProductsText = valid.Select(l => l.ProductId).Distinct().Count().ToString("N0");
        DraftStatusText = count == 0 ? "فارغ" : "جاهز للتنفيذ";
    }

    [RelayCommand]
    private void AddRow()
    {
        Lines.Add(new TransferLineItem { Quantity = 1 });
    }

    [RelayCommand]
    private void RemoveRow(TransferLineItem? line)
    {
        if (line is null) return;
        Lines.Remove(line);
        EnsureStarterRow();
    }

    [RelayCommand]
    private void RemoveLine(TransferLineItem? line) => RemoveRow(line);

    [RelayCommand]
    private void IncreaseRowQuantity(TransferLineItem? line)
    {
        if (line is null) return;
        line.Quantity += 1;
    }

    [RelayCommand]
    private void DecreaseRowQuantity(TransferLineItem? line)
    {
        if (line is null || line.Quantity <= 0) return;
        line.Quantity = Math.Max(0, line.Quantity - 1);
    }

    [RelayCommand]
    private void ClearLines()
    {
        if (Lines.Count == 0 || (Lines.Count == 1 && Lines[0].ProductId <= 0))
            return;
        if (!BeautifulMessageDialog.ShowConfirm("هل تريد مسح جميع بنود النقل؟", "مسح البنود"))
            return;
        Lines.Clear();
        EnsureStarterRow();
    }

    [RelayCommand]
    private void ProcessBarcode()
    {
        var code = BarcodeInput?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code))
        {
            BeautifulMessageDialog.ShowWarning("أدخل الباركود أولاً");
            return;
        }

        var product = Products.FirstOrDefault(p =>
            !string.IsNullOrWhiteSpace(p.Barcode)
            && string.Equals(p.Barcode.Trim(), code, StringComparison.OrdinalIgnoreCase));

        if (product is null)
        {
            BeautifulMessageDialog.ShowWarning($"لا يوجد منتج بالباركود: {code}");
            BarcodeInput = string.Empty;
            return;
        }

        var existing = Lines.FirstOrDefault(l => l.ProductId == product.Id);
        if (existing is not null)
        {
            existing.Quantity += 1;
        }
        else
        {
            var empty = Lines.FirstOrDefault(l => l.ProductId <= 0);
            if (empty is not null)
            {
                empty.SelectedProduct = product;
                empty.Quantity = 1;
            }
            else
            {
                Lines.Add(new TransferLineItem
                {
                    SelectedProduct = product,
                    Quantity = 1
                });
            }
        }

        BarcodeInput = string.Empty;
        RefreshDraftStats();
    }

    [RelayCommand]
    private async Task SaveTransferAsync()
    {
        if (FromWarehouse is null || ToWarehouse is null || FromWarehouse.Id == ToWarehouse.Id)
        {
            BeautifulMessageDialog.ShowWarning("اختر مخزنين مختلفين");
            return;
        }

        var valid = ValidLines().ToList();
        if (valid.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("أضف بنود النقل");
            return;
        }

        if (valid.Any(l => l.Quantity <= 0))
        {
            BeautifulMessageDialog.ShowWarning("جميع الكميات يجب أن تكون أكبر من صفر");
            return;
        }

        var overStock = valid
            .Where(l => l.Quantity > l.AvailableQuantity)
            .Select(l => l.ProductName)
            .Distinct()
            .ToList();
        if (overStock.Count > 0)
        {
            var names = string.Join("، ", overStock.Take(5));
            if (!BeautifulMessageDialog.ShowConfirm(
                    $"كمية بعض المنتجات تتجاوز المتاح في المخزن المصدر ({names}). هل تريد المتابعة؟",
                    "تنبيه الكمية"))
                return;
        }

        // دمج الصفوف لنفس المنتج قبل الإرسال لتفادي خصم مزدوج
        var merged = valid
            .GroupBy(l => l.ProductId)
            .Select(g => new WarehouseTransferItem
            {
                ProductId = g.Key,
                Quantity = g.Sum(x => x.Quantity)
            })
            .ToList();

        try
        {
            IsBusy = true;
            var transfer = new WarehouseTransfer
            {
                FromWarehouseId = FromWarehouse.Id,
                ToWarehouseId = ToWarehouse.Id,
                Date = TransferDate.Date == DateTime.Today
                    ? DateTime.Now
                    : TransferDate.Date.Add(DateTime.Now.TimeOfDay),
                Notes = Notes,
                BranchId = FromWarehouse.BranchId
            };
            await _transferService.CreateTransferAsync(transfer, merged);
            Lines.Clear();
            Notes = null;
            TransferDate = DateTime.Today;
            await ReloadFromStockAsync();
            EnsureStarterRow();
            BeautifulMessageDialog.ShowSuccess($"تم النقل — {transfer.TransferNumber}");
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
}

public partial class TransferLineItem : ObservableObject
{
    [ObservableProperty] private int _rowNumber;
    [ObservableProperty] private int _productId;
    [ObservableProperty] private string _productName = string.Empty;
    [ObservableProperty] private Product? _selectedProduct;
    [ObservableProperty] private decimal _quantity = 1;
    [ObservableProperty] private decimal _availableQuantity;
    [ObservableProperty] private string _availableQuantityText = "—";
    [ObservableProperty] private string _stockInfo = string.Empty;

    /// <summary>للتوافق مع قالب خلية كمية الفاتورة.</summary>
    public bool IsPriceEditable => true;

    /// <summary>للتوافق مع قالب خلية منتج الفاتورة.</summary>
    public bool IsOfferGift => false;

    /// <summary>Alias for invoice product cell template compatibility.</summary>
    public string ItemName
    {
        get => ProductName;
        set => ProductName = value;
    }

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

    partial void OnProductNameChanged(string value) => OnPropertyChanged(nameof(ItemName));
}

public sealed class WarehouseTransferWarehouseOption
{
    public required Warehouse Warehouse { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public int Id => Warehouse.Id;
    public int BranchId => Warehouse.BranchId;
    public string Name => Warehouse.Name;
}
