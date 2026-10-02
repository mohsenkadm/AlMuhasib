using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace AlMuhasib.UI.ViewModels;

public partial class WarehouseTransferViewModel : ViewModelBase
{
    private readonly IWarehouseTransferService _transferService;
    private readonly IUserPreferencesService _preferences;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ObservableCollection<WarehouseTransferWarehouseOption> Warehouses { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<TransferLineItem> Lines { get; } = [];

    [ObservableProperty] private WarehouseTransferWarehouseOption? _fromWarehouse;
    [ObservableProperty] private WarehouseTransferWarehouseOption? _toWarehouse;
    [ObservableProperty] private Product? _selectedProduct;
    [ObservableProperty] private decimal _lineQuantity = 1;
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
        IDbContextFactory<AppDbContext> dbFactory)
    {
        _transferService = transferService;
        _preferences = preferences;
        _dbFactory = dbFactory;
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

        foreach (var p in await db.Products.AsNoTracking().Where(p => !p.IsDeleted).OrderBy(p => p.Name).ToListAsync())
            Products.Add(p);

        RefreshDraftStats();
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
                item.PropertyChanged += OnLinePropertyChanged;
        }

        RefreshDraftStats();
    }

    private void OnLinePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TransferLineItem.Quantity))
            RefreshDraftStats();
    }

    private void RefreshDraftStats()
    {
        var count = Lines.Count;
        var totalQty = Lines.Sum(l => l.Quantity);
        LinesCountText = count.ToString("N0");
        TotalQuantityText = totalQty.ToString("N2");
        DistinctProductsText = Lines.Select(l => l.ProductId).Distinct().Count().ToString("N0");
        DraftStatusText = count == 0 ? "فارغ" : "جاهز للتنفيذ";
    }

    [RelayCommand]
    private void AddLine()
    {
        if (SelectedProduct is null || LineQuantity <= 0)
        {
            BeautifulMessageDialog.ShowWarning("اختر منتجاً وكمية أكبر من صفر");
            return;
        }

        var existing = Lines.FirstOrDefault(l => l.ProductId == SelectedProduct.Id);
        if (existing is not null)
            existing.Quantity += LineQuantity;
        else
        {
            Lines.Add(new TransferLineItem
            {
                ProductId = SelectedProduct.Id,
                ProductName = SelectedProduct.Name,
                Quantity = LineQuantity
            });
        }

        LineQuantity = 1;
    }

    [RelayCommand]
    private void RemoveLine(TransferLineItem? line)
    {
        if (line is null) return;
        Lines.Remove(line);
    }

    [RelayCommand]
    private void ClearLines()
    {
        if (Lines.Count == 0) return;
        if (!BeautifulMessageDialog.ShowConfirm("هل تريد مسح جميع بنود النقل؟", "مسح البنود"))
            return;
        Lines.Clear();
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
            existing.Quantity += 1;
        else
        {
            Lines.Add(new TransferLineItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = 1
            });
        }

        BarcodeInput = string.Empty;
    }

    [RelayCommand]
    private async Task SaveTransferAsync()
    {
        if (FromWarehouse is null || ToWarehouse is null || FromWarehouse.Id == ToWarehouse.Id)
        {
            BeautifulMessageDialog.ShowWarning("اختر مخزنين مختلفين");
            return;
        }

        if (Lines.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("أضف بنود النقل");
            return;
        }

        if (Lines.Any(l => l.Quantity <= 0))
        {
            BeautifulMessageDialog.ShowWarning("جميع الكميات يجب أن تكون أكبر من صفر");
            return;
        }

        try
        {
            IsBusy = true;
            var transfer = new WarehouseTransfer
            {
                FromWarehouseId = FromWarehouse.Id,
                ToWarehouseId = ToWarehouse.Id,
                Date = DateTime.Now,
                Notes = Notes,
                BranchId = FromWarehouse.BranchId
            };
            var items = Lines.Select(l => new WarehouseTransferItem
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity
            });
            await _transferService.CreateTransferAsync(transfer, items);
            Lines.Clear();
            Notes = null;
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

public class TransferLineItem : ObservableObject
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;

    private decimal _quantity;
    public decimal Quantity
    {
        get => _quantity;
        set => SetProperty(ref _quantity, value);
    }
}

public sealed class WarehouseTransferWarehouseOption
{
    public required Warehouse Warehouse { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public int Id => Warehouse.Id;
    public int BranchId => Warehouse.BranchId;
    public string Name => Warehouse.Name;
}
