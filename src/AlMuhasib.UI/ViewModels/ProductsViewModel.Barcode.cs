using System.IO;
using System.Windows.Media.Imaging;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class ProductsViewModel
{
    [ObservableProperty] private bool _isBarcodePrintDialogOpen;
    [ObservableProperty] private string _barcodePrintProductName = string.Empty;
    [ObservableProperty] private string _barcodePrintCode = string.Empty;
    [ObservableProperty] private string _barcodePrintPriceText = "—";
    [ObservableProperty] private int _barcodePrintCopies = 1;
    [ObservableProperty] private BitmapSource? _barcodePreviewImage;

    private Product? _barcodePrintProduct;
    private decimal? _barcodePrintPrice;

    [RelayCommand]
    private async Task GenerateBarcodeAsync()
    {
        for (var attempt = 0; attempt < 25; attempt++)
        {
            var candidate = BuildUniqueBarcodeCandidate();
            var existing = await _productService.GetByBarcodeAsync(candidate);
            if (existing is null)
            {
                EditBarcode = candidate;
                return;
            }
        }

        BeautifulMessageDialog.ShowWarning("تعذّر إنشاء باركود فريد — حاول مجدداً");
    }

    [RelayCommand]
    private async Task OpenBarcodePrintDialogAsync(Product? product)
    {
        if (product is null) return;

        if (string.IsNullOrWhiteSpace(product.Barcode))
        {
            BeautifulMessageDialog.ShowWarning("هذا المنتج بلا باركود — أنشئ باركوداً أولاً من التعديل");
            return;
        }

        _barcodePrintProduct = product;
        BarcodePrintProductName = product.Name;
        BarcodePrintCode = product.Barcode!;
        BarcodePrintCopies = 1;

        decimal? price = null;
        if (_pricingEnabled)
        {
            var prices = await _productPriceService.GetByProductIdsAsync([product.Id]);
            var preferred = prices.FirstOrDefault(p => p.PricingType?.IsDefault == true) ?? prices.FirstOrDefault();
            price = preferred?.SalePrice;
        }

        if (price is null or <= 0)
        {
            var card = ProductCards.FirstOrDefault(c => c.Product.Id == product.Id);
            price = card?.Prices.FirstOrDefault()?.SalePrice;
        }

        _barcodePrintPrice = price is > 0 ? price : null;
        BarcodePrintPriceText = _barcodePrintPrice is > 0 ? $"{_barcodePrintPrice:N0} د.ع" : "—";
        RefreshBarcodePreview();
        IsBarcodePrintDialogOpen = true;
    }

    [RelayCommand]
    private void CloseBarcodePrintDialog()
    {
        IsBarcodePrintDialogOpen = false;
        _barcodePrintProduct = null;
        BarcodePreviewImage = null;
    }

    [RelayCommand]
    private async Task OpenBarcodePrintFromCardAsync(ProductCardDisplay? card)
    {
        if (card?.Product is null) return;
        await OpenBarcodePrintDialogAsync(card.Product);
    }

    [RelayCommand]
    private void ConfirmBarcodePrint()
    {
        if (_barcodePrintProduct is null || string.IsNullOrWhiteSpace(BarcodePrintCode))
            return;

        if (BarcodePrintCopies < 1)
            BarcodePrintCopies = 1;
        if (BarcodePrintCopies > 200)
            BarcodePrintCopies = 200;

        var items = Enumerable.Range(0, BarcodePrintCopies)
            .Select(_ => new BarcodeLabelItem
            {
                ProductName = BarcodePrintProductName,
                Barcode = BarcodePrintCode,
                Price = _barcodePrintPrice
            })
            .ToList();

        _barcodeLabelService.PrintLabels(items);
        IsBarcodePrintDialogOpen = false;
    }

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

    partial void OnBarcodePrintCopiesChanged(int value) => RefreshBarcodePreview();

    private void RefreshBarcodePreview()
    {
        if (string.IsNullOrWhiteSpace(BarcodePrintCode))
        {
            BarcodePreviewImage = null;
            return;
        }

        var png = _barcodeLabelService.CreateBarcodePng(BarcodePrintCode, 320, 100);
        if (png is null || png.Length == 0)
        {
            BarcodePreviewImage = null;
            return;
        }

        var image = new BitmapImage();
        using var ms = new MemoryStream(png);
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = ms;
        image.EndInit();
        image.Freeze();
        BarcodePreviewImage = image;
    }

    private static string BuildUniqueBarcodeCandidate()
    {
        // Code128-friendly numeric barcode: prefix 2 + timestamp + random
        var stamp = DateTime.UtcNow.ToString("yyMMddHHmmss");
        var rand = Random.Shared.Next(100, 999);
        var candidate = $"2{stamp}{rand}";
        return candidate.Length <= 50 ? candidate : candidate[..50];
    }
}
