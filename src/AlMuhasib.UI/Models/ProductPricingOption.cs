using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

/// <summary>خيار تسعير لمنتج (نوع التسعير + السعر المعروض).</summary>
public partial class ProductPricingOption : ObservableObject
{
    public int PricingTypeId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public bool IsDefault { get; init; }

    public string Display => $"{Name} — {Price:N0}";
}
