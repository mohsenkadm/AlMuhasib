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
    private bool _isFavorite;
}
