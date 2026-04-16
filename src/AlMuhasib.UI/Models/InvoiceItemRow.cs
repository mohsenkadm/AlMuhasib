using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

/// <summary>
/// Observable row model for the invoice items DataGrid.
/// </summary>
public partial class InvoiceItemRow : ObservableObject
{
    [ObservableProperty]
    private int? _productId;

    [ObservableProperty]
    private string _itemName = string.Empty;

    [ObservableProperty]
    private decimal _quantity = 1m;

    [ObservableProperty]
    private decimal _unitPrice;

    [ObservableProperty]
    private decimal _totalPrice;

    partial void OnQuantityChanged(decimal value) => RecalcTotal();
    partial void OnUnitPriceChanged(decimal value) => RecalcTotal();

    private void RecalcTotal()
    {
        TotalPrice = Quantity * UnitPrice;
    }

    /// <summary>Event raised when TotalPrice changes so the parent VM can recalculate.</summary>
    public event Action? TotalChanged;

    partial void OnTotalPriceChanged(decimal value) => TotalChanged?.Invoke();
}
