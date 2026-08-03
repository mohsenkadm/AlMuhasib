using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

public sealed class WarehouseStockChip
{
    public required string WarehouseName { get; init; }
    public decimal Quantity { get; init; }
    public string QuantityLabel => Quantity.ToString("N0");
}

public partial class ProductSearchSuggestion : ObservableObject
{
    public required Product Product { get; init; }
    public string Name => Product.Name;
    public string? Barcode => Product.Barcode;
    public string? ScientificName => Product.ScientificName;
    public bool HasScientificName => !string.IsNullOrWhiteSpace(ScientificName);
    public bool HasBarcode => !string.IsNullOrWhiteSpace(Barcode);

    public ObservableCollection<WarehouseStockChip> WarehouseStocks { get; } = [];

    [ObservableProperty]
    private decimal _price;

    [ObservableProperty]
    private string _priceLabel = string.Empty;

    [ObservableProperty]
    private bool _hasPrice;

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    public string StockSummary =>
        WarehouseStocks.Count == 0
            ? "لا يوجد رصيد"
            : string.Join(" · ", WarehouseStocks.Select(s => $"{s.WarehouseName}: {s.QuantityLabel}"));
}
