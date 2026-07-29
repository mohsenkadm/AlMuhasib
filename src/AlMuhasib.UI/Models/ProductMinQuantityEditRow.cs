using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

/// <summary>صف تحرير كمية الحد الأدنى لمنتج في مخزن.</summary>
public partial class ProductMinQuantityEditRow : ObservableObject
{
    public int WarehouseId { get; init; }

    [ObservableProperty]
    private string _warehouseName = string.Empty;

    [ObservableProperty]
    private decimal _currentQuantity;

    [ObservableProperty]
    private decimal _minQuantity;

    public int? WarehouseStockId { get; set; }
}
