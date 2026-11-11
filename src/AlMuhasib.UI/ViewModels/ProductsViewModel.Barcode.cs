using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AlMuhasib.UI.ViewModels;

public partial class ProductsViewModel
{
    public IReadOnlyList<BarcodeLabelSizeOption> LabelSizeOptions { get; } = BarcodeLabelSizeOption.CreateAll();

    public ObservableCollection<string> AvailableBarcodes { get; } = [];
    public ObservableCollection<BarcodePriceOption> PriceOptions { get; } = [];

    [ObservableProperty] private bool _isBarcodePrintDialogOpen;
    [ObservableProperty] private string _printProductName = string.Empty;
    [ObservableProperty] private string? _selectedBarcode;
    [ObservableProperty] private string _printPriceText = "—";
    [ObservableProperty] private BarcodePriceOption? _selectedPriceOption;
    [ObservableProperty] private int _printCopies = 1;
    [ObservableProperty] private BarcodeLabelSizeOption? _selectedLabelSize;
    [ObservableProperty] private double _customWidthMm = BarcodeLabelSizeHelper.DefaultCustomWidthMm;
    [ObservableProperty] private double _customHeightMm = BarcodeLabelSizeHelper.DefaultCustomHeightMm;
    [ObservableProperty] private double _gapMm = BarcodeLabelSizeHelper.DefaultGapMm;
    [ObservableProperty] private BitmapSource? _previewImage;
    [ObservableProperty] private bool _showProductNameOnLabel = true;
    [ObservableProperty] private bool _showBranchNameOnLabel;
    [ObservableProperty] private bool _showCompanyNameOnLabel;
    [ObservableProperty] private bool _showPriceOnLabel = true;
    [ObservableProperty] private bool _showBarcodeNumberOnLabel = true;
    [ObservableProperty] private bool _hasMultipleBarcodes;
    [ObservableProperty] private bool _hasSingleBarcode = true;
    [ObservableProperty] private bool _hasPriceOptions;
    [ObservableProperty] private bool _hasNoPriceOptions = true;
    [ObservableProperty] private bool _isCustomSizeSelected;

    private Product? _barcodePrintProduct;
    private bool _suppressCustomSizeSync;

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
        PrintProductName = product.Name;
        ApplyDefaultContentFlags();
        PrintCopies = 1;
        GapMm = BarcodeLabelSizeHelper.DefaultGapMm;
        SelectDefaultLabelSize();

        AvailableBarcodes.Clear();
        foreach (var code in ProductBarcodeHelper.Split(product.Barcode))
            AvailableBarcodes.Add(code);
        if (AvailableBarcodes.Count == 0)
            AvailableBarcodes.Add(product.Barcode.Trim());

        HasMultipleBarcodes = AvailableBarcodes.Count > 1;
        HasSingleBarcode = !HasMultipleBarcodes;
        SelectedBarcode = AvailableBarcodes.FirstOrDefault();

        decimal? fallbackPrice = null;
        if (!_pricingEnabled)
        {
            var card = ProductCards.FirstOrDefault(c => c.Product.Id == product.Id);
            fallbackPrice = card?.Prices.FirstOrDefault()?.SalePrice;
        }

        await LoadBarcodePriceOptionsAsync(product.Id, fallbackPrice);
        RefreshPreview();
        IsBarcodePrintDialogOpen = true;
    }

    private async Task LoadBarcodePriceOptionsAsync(int productId, decimal? fallbackPrice)
    {
        PriceOptions.Clear();
        if (_pricingEnabled)
        {
            var prices = await _productPriceService.GetByProductIdAsync(productId);
            foreach (var p in prices.OrderByDescending(x => x.PricingType?.IsDefault == true)
                                    .ThenBy(x => x.PricingType?.Name))
            {
                var name = p.PricingType?.Name?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    name = "سعر";
                PriceOptions.Add(new BarcodePriceOption(
                    p.PricingTypeId,
                    name,
                    p.SalePrice > 0 ? p.SalePrice : null,
                    p.PricingType?.IsDefault == true));
            }
        }

        if (PriceOptions.Count == 0)
        {
            if (fallbackPrice is null or <= 0)
            {
                var card = ProductCards.FirstOrDefault(c => c.Product.Id == productId);
                fallbackPrice = card?.Prices.FirstOrDefault()?.SalePrice;
            }

            if (fallbackPrice is > 0)
                PriceOptions.Add(new BarcodePriceOption(null, "سعر مفرد", fallbackPrice, true));
        }

        HasPriceOptions = PriceOptions.Count > 0;
        HasNoPriceOptions = !HasPriceOptions;
        SelectedPriceOption = PriceOptions.FirstOrDefault(o => o.IsDefault) ?? PriceOptions.FirstOrDefault();
        PrintPriceText = SelectedPriceOption?.SalePrice is > 0
            ? $"{SelectedPriceOption.SalePrice:N0} د.ع"
            : "—";
    }

    [RelayCommand]
    private void ClosePrintDialog()
    {
        IsBarcodePrintDialogOpen = false;
        _barcodePrintProduct = null;
        PreviewImage = null;
        AvailableBarcodes.Clear();
        PriceOptions.Clear();
    }

    [RelayCommand]
    private async Task OpenBarcodePrintFromCardAsync(ProductCardDisplay? card)
    {
        if (card?.Product is null) return;
        await OpenBarcodePrintDialogAsync(card.Product);
    }

    [RelayCommand]
    private void RestorePrintDefaults()
    {
        ApplyDefaultContentFlags();
        PrintCopies = 1;
        GapMm = BarcodeLabelSizeHelper.DefaultGapMm;
        SelectDefaultLabelSize();
        CustomWidthMm = BarcodeLabelSizeHelper.DefaultCustomWidthMm;
        CustomHeightMm = BarcodeLabelSizeHelper.DefaultCustomHeightMm;

        if (AvailableBarcodes.Count > 0)
            SelectedBarcode = AvailableBarcodes[0];

        SelectedPriceOption = PriceOptions.FirstOrDefault(o => o.IsDefault) ?? PriceOptions.FirstOrDefault();
        RefreshPreview();
    }

    [RelayCommand]
    private void ConfirmPrint()
    {
        if (_barcodePrintProduct is null || string.IsNullOrWhiteSpace(SelectedBarcode))
            return;

        if (PrintCopies < 1)
            PrintCopies = 1;
        if (PrintCopies > 200)
            PrintCopies = 200;

        var template = BuildLabelItem();
        var items = Enumerable.Range(0, PrintCopies)
            .Select(_ => CloneLabelItem(template))
            .ToList();

        var size = SelectedLabelSize?.Size ?? BarcodeLabelSizeHelper.DefaultSize;
        var (customW, customH) = GetCustomDimensionsOrNull(size);
        _barcodeLabelService.PrintLabels(items, size, customW, customH, GapMm);
        IsBarcodePrintDialogOpen = false;
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

    private void ApplyDefaultContentFlags()
    {
        ShowProductNameOnLabel = true;
        ShowBranchNameOnLabel = false;
        ShowCompanyNameOnLabel = false;
        ShowPriceOnLabel = true;
        ShowBarcodeNumberOnLabel = true;
    }

    private void SelectDefaultLabelSize()
    {
        _suppressCustomSizeSync = true;
        try
        {
            SelectedLabelSize = LabelSizeOptions.FirstOrDefault(o => o.Size == BarcodeLabelSizeHelper.DefaultSize)
                                ?? LabelSizeOptions[0];
            IsCustomSizeSelected = SelectedLabelSize?.IsCustom == true;
        }
        finally
        {
            _suppressCustomSizeSync = false;
        }
    }

    partial void OnSelectedLabelSizeChanged(BarcodeLabelSizeOption? value)
    {
        if (_suppressCustomSizeSync) return;
        IsCustomSizeSelected = value?.IsCustom == true;
        if (value is { IsCustom: true })
        {
            if (CustomWidthMm <= 0) CustomWidthMm = BarcodeLabelSizeHelper.DefaultCustomWidthMm;
            if (CustomHeightMm <= 0) CustomHeightMm = BarcodeLabelSizeHelper.DefaultCustomHeightMm;
        }
        else if (value is not null)
        {
            var (w, h) = BarcodeLabelSizeHelper.GetMillimeters(value.Size);
            _suppressCustomSizeSync = true;
            try
            {
                CustomWidthMm = w;
                CustomHeightMm = h;
            }
            finally
            {
                _suppressCustomSizeSync = false;
            }
        }

        RefreshPreview();
    }

    partial void OnCustomWidthMmChanged(double value)
    {
        if (_suppressCustomSizeSync) return;
        EnsureCustomSizeSelected();
        RefreshPreview();
    }

    partial void OnCustomHeightMmChanged(double value)
    {
        if (_suppressCustomSizeSync) return;
        EnsureCustomSizeSelected();
        RefreshPreview();
    }

    private void EnsureCustomSizeSelected()
    {
        if (SelectedLabelSize?.IsCustom == true) return;
        _suppressCustomSizeSync = true;
        try
        {
            SelectedLabelSize = LabelSizeOptions.FirstOrDefault(o => o.IsCustom);
            IsCustomSizeSelected = true;
        }
        finally
        {
            _suppressCustomSizeSync = false;
        }
    }

    partial void OnSelectedBarcodeChanged(string? value) => RefreshPreview();
    partial void OnSelectedPriceOptionChanged(BarcodePriceOption? value)
    {
        PrintPriceText = value?.SalePrice is > 0 ? $"{value.SalePrice:N0} د.ع" : "—";
        RefreshPreview();
    }

    partial void OnPrintCopiesChanged(int value) => RefreshPreview();
    partial void OnGapMmChanged(double value) => RefreshPreview();
    partial void OnShowProductNameOnLabelChanged(bool value) => RefreshPreview();
    partial void OnShowBranchNameOnLabelChanged(bool value) => RefreshPreview();
    partial void OnShowCompanyNameOnLabelChanged(bool value) => RefreshPreview();
    partial void OnShowPriceOnLabelChanged(bool value) => RefreshPreview();
    partial void OnShowBarcodeNumberOnLabelChanged(bool value) => RefreshPreview();

    private void RefreshPreview()
    {
        if (string.IsNullOrWhiteSpace(SelectedBarcode))
        {
            PreviewImage = null;
            return;
        }

        var size = SelectedLabelSize?.Size ?? BarcodeLabelSizeHelper.DefaultSize;
        var (customW, customH) = GetCustomDimensionsOrNull(size);
        var png = _barcodeLabelService.CreateLabelPreviewPng(
            BuildLabelItem(),
            size,
            customW,
            customH,
            scale: 3.0);

        if (png is null || png.Length == 0)
        {
            PreviewImage = null;
            return;
        }

        var image = new BitmapImage();
        using var ms = new MemoryStream(png);
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = ms;
        image.EndInit();
        image.Freeze();
        PreviewImage = image;
    }

    private BarcodeLabelItem BuildLabelItem() => new()
    {
        CompanyName = PrintBrandingProvider.Current.CompanyName ?? string.Empty,
        BranchName = ResolveBranchName(),
        ProductName = PrintProductName,
        Barcode = SelectedBarcode ?? string.Empty,
        Price = SelectedPriceOption?.SalePrice,
        ShowCompanyName = ShowCompanyNameOnLabel,
        ShowBranchName = ShowBranchNameOnLabel,
        ShowProductName = ShowProductNameOnLabel,
        ShowPrice = ShowPriceOnLabel,
        ShowBarcodeNumber = ShowBarcodeNumberOnLabel
    };

    private static BarcodeLabelItem CloneLabelItem(BarcodeLabelItem source) => new()
    {
        CompanyName = source.CompanyName,
        BranchName = source.BranchName,
        ProductName = source.ProductName,
        Barcode = source.Barcode,
        Price = source.Price,
        KaratValue = source.KaratValue,
        WeightGrams = source.WeightGrams,
        ShowCompanyName = source.ShowCompanyName,
        ShowBranchName = source.ShowBranchName,
        ShowProductName = source.ShowProductName,
        ShowPrice = source.ShowPrice,
        ShowBarcodeNumber = source.ShowBarcodeNumber
    };

    private (double? Width, double? Height) GetCustomDimensionsOrNull(BarcodeLabelSize size)
    {
        if (size != BarcodeLabelSize.Custom)
            return (null, null);
        return (
            BarcodeLabelSizeHelper.ClampDimension(CustomWidthMm),
            BarcodeLabelSizeHelper.ClampDimension(CustomHeightMm));
    }

    private string ResolveBranchName() =>
        _services.GetService<IBranchContext>()?.CurrentBranchName ?? string.Empty;

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
