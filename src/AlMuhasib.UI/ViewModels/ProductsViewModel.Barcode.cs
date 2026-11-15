using System.ComponentModel;
using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AlMuhasib.UI.ViewModels;

public partial class ProductsViewModel
{
    public PriceLabelPrintEditor LabelPrint { get; private set; } = null!;

    [ObservableProperty] private bool _isBarcodePrintDialogOpen;

    public void AttachLabelPrint(PriceLabelPrintEditor editor)
    {
        LabelPrint = editor;
        LabelPrint.PropertyChanged += OnLabelPrintChanged;
    }

    private void OnLabelPrintChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PriceLabelPrintEditor.IsOpen))
            IsBarcodePrintDialogOpen = LabelPrint.IsOpen;
    }

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

        decimal? fallbackPrice = null;
        if (!_pricingEnabled)
        {
            var card = ProductCards.FirstOrDefault(c => c.Product.Id == product.Id);
            fallbackPrice = card?.Prices.FirstOrDefault()?.SalePrice;
        }

        await LabelPrint.OpenAsync(product.Id, product.Name, product.Barcode, fallbackPrice);
    }

    [RelayCommand]
    private async Task OpenBarcodePrintFromCardAsync(ProductCardDisplay? card)
    {
        if (card?.Product is null) return;
        await OpenBarcodePrintDialogAsync(card.Product);
    }

    [RelayCommand]
    private void PrintBarcodeLabels()
    {
        var companyName = PrintBrandingProvider.Current.CompanyName ?? string.Empty;
        var branchName = ResolveBranchName();
        var items = Products
            .Where(p => !string.IsNullOrWhiteSpace(p.Barcode))
            .Select(p => new BarcodeLabelItem
            {
                CompanyName = companyName,
                BranchName = branchName,
                ProductName = p.Name,
                Barcode = ProductBarcodeHelper.Primary(p.Barcode) ?? p.Barcode!,
                ShowCompanyName = true,
                ShowBranchName = false,
                ShowProductName = true,
                ShowPrice = true,
                ShowBarcodeNumber = true
            })
            .ToList();
        if (items.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("لا منتجات بباركود للطباعة");
            return;
        }
        _barcodeLabelService.PrintLabels(items);
    }

    private string ResolveBranchName() =>
        _services.GetService<AlMuhasib.Core.Interfaces.IBranchContext>()?.CurrentBranchName ?? string.Empty;

    private static string BuildUniqueBarcodeCandidate()
    {
        var stamp = DateTime.UtcNow.ToString("yyMMddHHmmss");
        var rand = Random.Shared.Next(100, 999);
        var candidate = $"2{stamp}{rand}";
        return candidate.Length <= ProductBarcodeHelper.MaxStoredLength
            ? candidate
            : candidate[..ProductBarcodeHelper.MaxStoredLength];
    }
}
