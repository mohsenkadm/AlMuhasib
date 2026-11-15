using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

/// <summary>
/// حالة دايلوج طباعة الملصق المشتركة بين المنتجات وطباعة الباركودات.
/// </summary>
public partial class PriceLabelPrintEditor : ObservableObject
{
    private readonly IProductPriceService _productPriceService;
    private readonly IBarcodeLabelService _barcodeLabelService;
    private readonly IUserPreferencesService _userPreferences;
    private readonly bool _pricingEnabled;

    private System.Timers.Timer? _previewTimer;
    private int _productId;
    private bool _suppressCustomSizeSync;
    private bool _suppressDesign;

    public ObservableCollection<string> AvailableBarcodes { get; } = [];
    public ObservableCollection<BarcodePriceOption> PriceOptions { get; } = [];

    public IReadOnlyList<BarcodeLabelSizeOption> LabelSizeOptions { get; } = BarcodeLabelSizeOption.CreateAll();
    public IReadOnlyList<PriceLabelPaperOption> PaperOptions { get; } = PriceLabelPaperOption.CreateAll();
    public IReadOnlyList<PriceLabelSymbologyOption> SymbologyOptions { get; } = PriceLabelSymbologyOption.CreateAll();

    [ObservableProperty] private bool _isOpen;
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
    [ObservableProperty] private double _marginLeftMm;
    [ObservableProperty] private double _marginRightMm;
    [ObservableProperty] private double _marginTopMm;
    [ObservableProperty] private double _marginBottomMm;
    [ObservableProperty] private int _columns = 3;
    [ObservableProperty] private double _rowSpacingMm = 2;
    [ObservableProperty] private double _columnSpacingMm = 2;
    [ObservableProperty] private PriceLabelPaperOption? _selectedPaper;
    [ObservableProperty] private PriceLabelSymbologyOption? _selectedSymbology;
    [ObservableProperty] private bool _showSkuOnLabel = true;
    [ObservableProperty] private bool _showBarcodeGraphic = true;
    [ObservableProperty] private bool _showTaxInclusivePrice;
    [ObservableProperty] private bool _showBorder = true;
    [ObservableProperty] private double _productNameFontSize = 12;
    [ObservableProperty] private double _priceFontSize = 16;
    [ObservableProperty] private double _barcodeHeightMm = 12;
    [ObservableProperty] private double _taxPercent;
    [ObservableProperty] private string? _barcodeValidationMessage;
    [ObservableProperty] private string? _fitWarning;
    [ObservableProperty] private bool _canPrintSheet = true;
    [ObservableProperty] private string _previewSummary = string.Empty;

    public event Action<string>? Notice;

    public PriceLabelPrintEditor(
        IBarcodeLabelService barcodeLabelService,
        IProductPriceService productPriceService,
        IUserPreferencesService userPreferences,
        bool pricingEnabled)
    {
        _barcodeLabelService = barcodeLabelService;
        _productPriceService = productPriceService;
        _userPreferences = userPreferences;
        _pricingEnabled = pricingEnabled;
        _selectedLabelSize = LabelSizeOptions.FirstOrDefault(o => o.Size == BarcodeLabelSizeHelper.DefaultSize)
                             ?? LabelSizeOptions[0];
        _isCustomSizeSelected = _selectedLabelSize.IsCustom;
        _selectedPaper = PaperOptions[0];
        _selectedSymbology = SymbologyOptions[0];
    }

    public async Task OpenAsync(int productId, string productName, string barcodeField, decimal? fallbackPrice)
    {
        _productId = productId;
        PrintProductName = productName;
        LoadSavedDesign();
        _suppressDesign = true;
        try
        {
            PrintCopies = 1;
            AvailableBarcodes.Clear();
            foreach (var code in ProductBarcodeHelper.Split(barcodeField))
                AvailableBarcodes.Add(code);
            if (AvailableBarcodes.Count == 0 && !string.IsNullOrWhiteSpace(barcodeField))
                AvailableBarcodes.Add(barcodeField.Trim());

            HasMultipleBarcodes = AvailableBarcodes.Count > 1;
            HasSingleBarcode = !HasMultipleBarcodes;
            SelectedBarcode = AvailableBarcodes.FirstOrDefault();
            await LoadPriceOptionsAsync(productId, fallbackPrice);
        }
        finally
        {
            _suppressDesign = false;
        }

        RefreshPreview();
        IsOpen = true;
    }

    [RelayCommand]
    private void ClosePrintDialog()
    {
        IsOpen = false;
        PreviewImage = null;
        AvailableBarcodes.Clear();
        PriceOptions.Clear();
        _previewTimer?.Stop();
    }

    [RelayCommand]
    private void ChangePrintProduct() => ClosePrintDialog();

    [RelayCommand]
    private void IncreaseCopies()
    {
        if (PrintCopies < 200)
            PrintCopies++;
    }

    [RelayCommand]
    private void DecreaseCopies()
    {
        if (PrintCopies > 1)
            PrintCopies--;
    }

    [RelayCommand]
    private void SaveLabelDesign()
    {
        var design = CaptureDesign();
        _userPreferences.Update(p => p.PriceLabelDesign = design);
        Notice?.Invoke("تم حفظ إعدادات بطاقة السعر");
    }

    [RelayCommand]
    private void RefreshPrintPreview() => RefreshPreview();

    [RelayCommand]
    private void RestorePrintDefaults()
    {
        _suppressDesign = true;
        try
        {
            ApplyDesign(new PriceLabelDesign());
            PrintCopies = 1;
        }
        finally
        {
            _suppressDesign = false;
        }

        if (AvailableBarcodes.Count > 0)
            SelectedBarcode = AvailableBarcodes[0];

        SelectedPriceOption = PriceOptions.FirstOrDefault(o => o.IsDefault) ?? PriceOptions.FirstOrDefault();
        RefreshPreview();
    }

    [RelayCommand]
    private void ConfirmPrint()
    {
        if (string.IsNullOrWhiteSpace(SelectedBarcode))
            return;

        if (PrintCopies < 1) PrintCopies = 1;
        if (PrintCopies > 200) PrintCopies = 200;

        var design = CaptureDesign();
        var ean = EanBarcode.Normalize(SelectedBarcode, design.Symbology);
        if (design.ShowBarcode && !ean.IsValid)
        {
            BarcodeValidationMessage = ean.Error;
            BeautifulMessageDialog.ShowWarning(ean.Error ?? "الباركود غير صالح للنوع المختار ولن يُطبع.");
            return;
        }

        var fit = design.Measure(PrintCopies);
        if (fit.PlacedCards == 0)
        {
            FitWarning = fit.Warning;
            BeautifulMessageDialog.ShowWarning(fit.Warning ?? "البطاقة لا تتسع في الورقة.");
            return;
        }

        var printed = _barcodeLabelService.PrintSheet(BuildLabelItem(design), design, PrintCopies);
        if (!printed)
            return;

        IsOpen = false;
        Notice?.Invoke($"تمت طباعة {PrintCopies} بطاقة للمنتج {PrintProductName}");
    }

    private async Task LoadPriceOptionsAsync(int productId, decimal? fallbackPrice)
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

        if (PriceOptions.Count == 0 && fallbackPrice is > 0)
            PriceOptions.Add(new BarcodePriceOption(null, "سعر مفرد", fallbackPrice, true));

        HasPriceOptions = PriceOptions.Count > 0;
        HasNoPriceOptions = !HasPriceOptions;
        SelectedPriceOption = PriceOptions.FirstOrDefault(o => o.IsDefault) ?? PriceOptions.FirstOrDefault();
        PrintPriceText = SelectedPriceOption?.SalePrice is > 0
            ? $"{SelectedPriceOption.SalePrice:N0} د.ع"
            : "—";
    }

    partial void OnSelectedLabelSizeChanged(BarcodeLabelSizeOption? value)
    {
        if (_suppressCustomSizeSync || _suppressDesign) return;
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

        RequestPreview();
    }

    partial void OnCustomWidthMmChanged(double value)
    {
        if (_suppressCustomSizeSync || _suppressDesign) return;
        EnsureCustomSizeSelected();
        RequestPreview();
    }

    partial void OnCustomHeightMmChanged(double value)
    {
        if (_suppressCustomSizeSync || _suppressDesign) return;
        EnsureCustomSizeSelected();
        RequestPreview();
    }

    partial void OnSelectedBarcodeChanged(string? value) => RequestPreview();
    partial void OnSelectedPriceOptionChanged(BarcodePriceOption? value)
    {
        PrintPriceText = value?.SalePrice is > 0 ? $"{value.SalePrice:N0} د.ع" : "—";
        RequestPreview();
    }

    partial void OnPrintCopiesChanged(int value) => RequestPreview();
    partial void OnGapMmChanged(double value) => RequestPreview();
    partial void OnShowProductNameOnLabelChanged(bool value) => RequestPreview();
    partial void OnShowBranchNameOnLabelChanged(bool value) => RequestPreview();
    partial void OnShowCompanyNameOnLabelChanged(bool value) => RequestPreview();
    partial void OnShowPriceOnLabelChanged(bool value) => RequestPreview();
    partial void OnShowBarcodeNumberOnLabelChanged(bool value) => RequestPreview();
    partial void OnMarginLeftMmChanged(double value) => RequestPreview();
    partial void OnMarginRightMmChanged(double value) => RequestPreview();
    partial void OnMarginTopMmChanged(double value) => RequestPreview();
    partial void OnMarginBottomMmChanged(double value) => RequestPreview();
    partial void OnColumnsChanged(int value) => RequestPreview();
    partial void OnRowSpacingMmChanged(double value) => RequestPreview();
    partial void OnColumnSpacingMmChanged(double value) => RequestPreview();
    partial void OnSelectedPaperChanged(PriceLabelPaperOption? value) => RequestPreview();
    partial void OnSelectedSymbologyChanged(PriceLabelSymbologyOption? value) => RequestPreview();
    partial void OnShowSkuOnLabelChanged(bool value) => RequestPreview();
    partial void OnShowBarcodeGraphicChanged(bool value) => RequestPreview();
    partial void OnShowTaxInclusivePriceChanged(bool value) => RequestPreview();
    partial void OnShowBorderChanged(bool value) => RequestPreview();
    partial void OnProductNameFontSizeChanged(double value) => RequestPreview();
    partial void OnPriceFontSizeChanged(double value) => RequestPreview();
    partial void OnBarcodeHeightMmChanged(double value) => RequestPreview();
    partial void OnTaxPercentChanged(double value) => RequestPreview();

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

    private BarcodeLabelSizeOption MatchPreset(double width, double height)
    {
        foreach (var option in LabelSizeOptions)
        {
            if (option.IsCustom)
                continue;
            var (w, h) = BarcodeLabelSizeHelper.GetMillimeters(option.Size);
            if (Math.Abs(w - width) < 0.2 && Math.Abs(h - height) < 0.2)
                return option;
        }

        return LabelSizeOptions.First(o => o.IsCustom);
    }

    private void RequestPreview()
    {
        if (_suppressDesign || !IsOpen)
            return;

        _previewTimer?.Stop();
        _previewTimer?.Dispose();
        _previewTimer = new System.Timers.Timer(90);
        _previewTimer.AutoReset = false;
        _previewTimer.Elapsed += (_, _) =>
        {
            _previewTimer?.Stop();
            Application.Current?.Dispatcher.Invoke(RefreshPreview);
        };
        _previewTimer.Start();
    }

    private void RefreshPreview()
    {
        if (string.IsNullOrWhiteSpace(SelectedBarcode))
        {
            PreviewImage = null;
            BarcodeValidationMessage = "لا يوجد باركود للمعاينة.";
            CanPrintSheet = false;
            return;
        }

        var design = CaptureDesign();
        var ean = EanBarcode.Normalize(SelectedBarcode, design.Symbology);
        BarcodeValidationMessage = design.ShowBarcode && !ean.IsValid ? ean.Error : null;
        var marginsFit = design.MarginLeftMm + design.MarginRightMm < design.LabelWidthMm - 4
                         && design.MarginTopMm + design.MarginBottomMm < design.LabelHeightMm - 4;
        FitWarning = marginsFit ? null : "الهوامش أكبر من مساحة البطاقة.";
        CanPrintSheet = marginsFit && string.IsNullOrWhiteSpace(BarcodeValidationMessage);
        PreviewSummary = "معاينة البطاقة";

        var png = _barcodeLabelService.CreateSingleLabelPreviewPng(BuildLabelItem(design), design);
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

    private void LoadSavedDesign()
    {
        var saved = _userPreferences.Current.PriceLabelDesign?.Clone() ?? new PriceLabelDesign();
        _suppressDesign = true;
        try
        {
            ApplyDesign(saved);
        }
        finally
        {
            _suppressDesign = false;
        }
    }

    private void ApplyDesign(PriceLabelDesign design)
    {
        design.Normalize();
        MarginLeftMm = design.MarginLeftMm;
        MarginRightMm = design.MarginRightMm;
        MarginTopMm = design.MarginTopMm;
        MarginBottomMm = design.MarginBottomMm;
        CustomWidthMm = design.LabelWidthMm;
        CustomHeightMm = design.LabelHeightMm;
        SelectedLabelSize = MatchPreset(design.LabelWidthMm, design.LabelHeightMm);
        IsCustomSizeSelected = SelectedLabelSize?.IsCustom == true;
        RowSpacingMm = design.RowSpacingMm;
        ColumnSpacingMm = design.ColumnSpacingMm;
        SelectedPaper = PaperOptions.FirstOrDefault(o => o.Paper == design.Paper) ?? PaperOptions[0];
        SelectedSymbology = SymbologyOptions.FirstOrDefault(o => o.Symbology == design.Symbology) ?? SymbologyOptions[0];
        ShowProductNameOnLabel = design.ShowProductName;
        ShowPriceOnLabel = design.ShowPrice;
        ShowSkuOnLabel = design.ShowSku;
        ShowBarcodeGraphic = design.ShowBarcode;
        ShowTaxInclusivePrice = design.ShowTaxInclusivePrice;
        ShowBorder = design.ShowBorder;
        ProductNameFontSize = design.ProductNameFontSize;
        PriceFontSize = design.PriceFontSize;
        BarcodeHeightMm = design.BarcodeHeightMm;
        TaxPercent = design.TaxPercent;
        ShowBarcodeNumberOnLabel = true;
    }

    private PriceLabelDesign CaptureDesign()
    {
        var design = new PriceLabelDesign
        {
            MarginLeftMm = MarginLeftMm,
            MarginRightMm = MarginRightMm,
            MarginTopMm = MarginTopMm,
            MarginBottomMm = MarginBottomMm,
            Columns = 8,
            LabelWidthMm = CustomWidthMm,
            LabelHeightMm = CustomHeightMm,
            RowSpacingMm = RowSpacingMm,
            ColumnSpacingMm = ColumnSpacingMm,
            Paper = SelectedPaper?.Paper ?? PriceLabelPaper.A4,
            Symbology = SelectedSymbology?.Symbology ?? PriceLabelSymbology.Ean13,
            ShowProductName = ShowProductNameOnLabel,
            ShowPrice = ShowPriceOnLabel,
            ShowSku = ShowSkuOnLabel,
            ShowBarcode = ShowBarcodeGraphic,
            ShowTaxInclusivePrice = false,
            ShowBorder = ShowBorder,
            ProductNameFontSize = ProductNameFontSize,
            PriceFontSize = PriceFontSize,
            BarcodeHeightMm = BarcodeHeightMm,
            TaxPercent = 0
        };
        design.Normalize();
        return design;
    }

    private BarcodeLabelItem BuildLabelItem(PriceLabelDesign design) => new()
    {
        ProductName = PrintProductName,
        Barcode = SelectedBarcode ?? string.Empty,
        Sku = _productId > 0 ? _productId.ToString() : string.Empty,
        Price = SelectedPriceOption?.SalePrice,
        ShowProductName = design.ShowProductName,
        ShowPrice = design.ShowPrice,
        ShowSku = design.ShowSku,
        ShowBarcodeGraphic = design.ShowBarcode,
        ShowTaxInclusivePrice = design.ShowTaxInclusivePrice,
        ShowBorder = design.ShowBorder,
        TaxPercent = design.TaxPercent,
        ProductNameFontSize = design.ProductNameFontSize,
        PriceFontSize = design.PriceFontSize,
        BarcodeHeightMm = design.BarcodeHeightMm,
        Symbology = design.Symbology,
        ShowBarcodeNumber = true
    };
}
