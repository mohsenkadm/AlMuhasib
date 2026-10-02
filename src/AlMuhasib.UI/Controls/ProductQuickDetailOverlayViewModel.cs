using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Controls;

public partial class ProductQuickDetailOverlayViewModel : ObservableObject
{
    [ObservableProperty] private bool _isBusy = true;
    [ObservableProperty] private string _name = "جاري التحميل…";
    [ObservableProperty] private string? _barcode;
    [ObservableProperty] private string? _categoryName;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private string _totalStockText = "—";
    [ObservableProperty] private string _lastPurchaseText = "—";
    [ObservableProperty] private string _lastSaleText = "—";
    [ObservableProperty] private string _lastPurchaseUsdText = "—";
    [ObservableProperty] private string _lastSaleUsdText = "—";
    [ObservableProperty] private string _currentSalePriceText = "—";
    [ObservableProperty] private string _currentPurchasePriceText = "—";
    [ObservableProperty] private string _currentSalePriceUsdText = "—";
    [ObservableProperty] private string _currentPurchasePriceUsdText = "—";
    [ObservableProperty] private string _saleSummaryText = "—";
    [ObservableProperty] private string _purchaseSummaryText = "—";
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _hasData;
    [ObservableProperty] private bool _showMultiCurrency;

    public ObservableCollection<ProductQuickStockRow> StocksByWarehouse { get; } = [];

    public void Apply(ProductQuickDetailResult data, bool showMultiCurrency = false)
    {
        ShowMultiCurrency = showMultiCurrency;
        Name = data.Name;
        Barcode = string.IsNullOrWhiteSpace(data.Barcode) ? null : data.Barcode;
        CategoryName = string.IsNullOrWhiteSpace(data.CategoryName) ? null : data.CategoryName;
        Description = string.IsNullOrWhiteSpace(data.Description) ? null : data.Description;
        TotalStockText = data.TotalStockQuantity.ToString("N2");
        LastPurchaseText = data.LastPurchaseDate is null
            ? "لا يوجد شراء سابق"
            : $"{data.LastPurchasePrice:N0} د.ع — {data.LastPurchaseDate:yyyy/MM/dd}";
        LastSaleText = data.LastSaleDate is null
            ? "لا يوجد بيع سابق"
            : $"{data.LastSalePrice:N0} د.ع — {data.LastSaleDate:yyyy/MM/dd}";
        LastPurchaseUsdText = data.LastPurchaseDateUsd is null
            ? "لا يوجد شراء $"
            : $"$ {data.LastPurchasePriceUsd:N2} — {data.LastPurchaseDateUsd:yyyy/MM/dd}";
        LastSaleUsdText = data.LastSaleDateUsd is null
            ? "لا يوجد بيع $"
            : $"$ {data.LastSalePriceUsd:N2} — {data.LastSaleDateUsd:yyyy/MM/dd}";
        CurrentSalePriceText = data.CurrentSalePrice is null or <= 0 ? "—" : $"{data.CurrentSalePrice:N0} د.ع";
        CurrentPurchasePriceText = data.CurrentPurchasePrice is null or <= 0 ? "—" : $"{data.CurrentPurchasePrice:N0} د.ع";
        CurrentSalePriceUsdText = data.CurrentSalePriceUsd is null or <= 0 ? "—" : $"$ {data.CurrentSalePriceUsd:N2}";
        CurrentPurchasePriceUsdText = data.CurrentPurchasePriceUsd is null or <= 0 ? "—" : $"$ {data.CurrentPurchasePriceUsd:N2}";
        SaleSummaryText = $"{data.SaleDealCount:N0} تعامل — كمية {data.TotalSoldQuantity:N2}";
        PurchaseSummaryText = $"{data.PurchaseDealCount:N0} تعامل — كمية {data.TotalPurchasedQuantity:N2}";

        StocksByWarehouse.Clear();
        foreach (var s in data.StocksByWarehouse)
            StocksByWarehouse.Add(s);

        HasData = true;
        IsBusy = false;
        ErrorMessage = null;
    }

    public void SetError(string message)
    {
        ErrorMessage = message;
        IsBusy = false;
        HasData = false;
        Name = "تعذر التحميل";
    }
}
