using AlMuhasib.Core.Entities;
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

    [ObservableProperty]
    private Product? _selectedProduct;

    /// <summary>معلومات الرصيد في المخازن</summary>
    [ObservableProperty]
    private string _stockInfo = string.Empty;

    /// <summary>إجمالي الرصيد المتاح في المخزن المحدد</summary>
    [ObservableProperty]
    private decimal _availableStock;

    private bool _isManualTotal;

    partial void OnSelectedProductChanged(Product? value)
    {
        if (value is not null)
        {
            ProductId = value.Id;
            ItemName = value.Name;
        }
        ProductChanged?.Invoke(this);
    }

    partial void OnQuantityChanged(decimal value)
    {
        _isManualTotal = false;
        RecalcTotal();
    }

    partial void OnUnitPriceChanged(decimal value)
    {
        _isManualTotal = false;
        RecalcTotal();
    }

    partial void OnTotalPriceChanged(decimal oldValue, decimal newValue)
    {
        if (!_isRecalculating)
        {
            if (Quantity != 0)
            {
                _isRecalculating = true;
                UnitPrice = newValue / Quantity;
                _isRecalculating = false;
                _isManualTotal = false;
            }
            else
            {
                _isManualTotal = true;
            }
        }

        TotalChanged?.Invoke();
    }

    private bool _isRecalculating;

    private void RecalcTotal()
    {
        if (_isManualTotal) return;
        _isRecalculating = true;
        TotalPrice = Quantity * UnitPrice;
        _isRecalculating = false;
    }

    /// <summary>Event raised when TotalPrice changes so the parent VM can recalculate.</summary>
    public event Action? TotalChanged;

    /// <summary>Event raised when the selected product changes so the parent VM can load stock info.</summary>
    public event Action<InvoiceItemRow>? ProductChanged;
}
