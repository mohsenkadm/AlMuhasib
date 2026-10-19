using AlMuhasib.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

public partial class PosProductTile : ObservableObject
{
    public required Product Product { get; init; }
    public string Name => Product.Name;
    public string? ScientificName => Product.ScientificName;
    public bool HasScientificName => !string.IsNullOrWhiteSpace(ScientificName);
    public string? Barcode => Product.Barcode;
    public decimal Price { get; init; }

    [ObservableProperty]
    private decimal _availableQuantity;

    public string AvailableQuantityText => AvailableQuantity.ToString("N0");

    partial void OnAvailableQuantityChanged(decimal value) =>
        OnPropertyChanged(nameof(AvailableQuantityText));
}
