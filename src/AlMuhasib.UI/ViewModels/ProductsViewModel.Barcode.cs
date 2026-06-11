using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class ProductsViewModel
{
    [RelayCommand]
    private void PrintBarcodeLabels()
    {
        var items = Products
            .Where(p => !string.IsNullOrWhiteSpace(p.Barcode))
            .Select(p => new BarcodeLabelItem { ProductName = p.Name, Barcode = p.Barcode! })
            .ToList();
        if (items.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("لا منتجات بباركود للطباعة");
            return;
        }
        _barcodeLabelService.PrintLabels(items);
    }
}
