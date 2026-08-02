using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.ViewModels;

public partial class PosQuickSaleViewModel
{
    private IProductSerialService? _productSerialService;
    private IProductSizeService? _productSizeService;
    private IProductColorService? _productColorService;

    [ObservableProperty] private bool _showExpiryTracking;
    [ObservableProperty] private bool _showSerialNumbers;
    [ObservableProperty] private bool _showProductPricing;
    [ObservableProperty] private bool _showClothingSizes;
    [ObservableProperty] private bool _showPharmacy;
    [ObservableProperty] private bool _showCustomField1;
    [ObservableProperty] private bool _showCustomField2;
    [ObservableProperty] private string _customField1Header = string.Empty;
    [ObservableProperty] private string _customField2Header = string.Empty;

    private void ConfigurePosFeatureServices(
        IProductSerialService productSerialService,
        IProductSizeService productSizeService,
        IProductColorService productColorService)
    {
        _productSerialService = productSerialService;
        _productSizeService = productSizeService;
        _productColorService = productColorService;
        RefreshAllFeatureVisibility();
        _featureFlags.FlagsChanged += (_, _) => FeatureUiRefresh.Invoke(RefreshAllFeatureVisibility);
    }

    private void RefreshAllFeatureVisibility()
    {
        ShowProductDiscount = _featureFlags.ProductDiscountEnabled;
        ShowExpiryTracking = _featureFlags.ExpiryTracking;
        ShowSerialNumbers = _featureFlags.SerialNumbers;
        ShowProductPricing = _featureFlags.ProductPricingEnabled;
        ShowClothingSizes = _featureFlags.TemplateClothing;
        ShowPharmacy = _featureFlags.TemplatePharmacy;

        ApplyMarketTemplateHeaders();

        foreach (var line in CartLines)
        {
            line.ProductDiscountFeatureEnabled = ShowProductDiscount;
            line.RefreshProductDiscount();
        }

        if (!ShowProductDiscount)
        {
            InvoiceDiscountType = DiscountType.None;
            InvoiceDiscountValue = 0m;
            InvoiceDiscountAmount = 0m;
            SelectedInvoiceDiscountOption = InvoiceDiscountTypeOptions[0];
        }

        RecalcCartTotals();
    }

    private void ApplyMarketTemplateHeaders()
    {
        if (_featureFlags.TemplateClothing)
        {
            // أعمدة القياس/اللون المخصّصة تغني عن حقول القالب النصية
            CustomField1Header = ClothingSizeInvoiceHelper.SizeLabel;
            CustomField2Header = ClothingSizeInvoiceHelper.ColorLabel;
            ShowCustomField1 = false;
            ShowCustomField2 = false;
            return;
        }

        if (_featureFlags.TemplateMobileShop)
        {
            CustomField1Header = "IMEI";
            CustomField2Header = "اللون";
            ShowCustomField1 = true;
            ShowCustomField2 = true;
            return;
        }

        if (_featureFlags.TemplateConstruction)
        {
            CustomField1Header = "الوحدة";
            CustomField2Header = "المواصفات";
            ShowCustomField1 = true;
            ShowCustomField2 = true;
            return;
        }

        if (_featureFlags.TemplatePharmacy)
        {
            // عند تفعيل تتبع الصلاحية تُعرض أعمدة الدفعة الحقيقية؛ وإلا حقول نصية من القالب
            CustomField1Header = "تاريخ الانتهاء";
            CustomField2Header = "رقم الدفعة";
            ShowCustomField1 = !ShowExpiryTracking;
            ShowCustomField2 = !ShowExpiryTracking;
            return;
        }

        CustomField1Header = string.Empty;
        CustomField2Header = string.Empty;
        ShowCustomField1 = false;
        ShowCustomField2 = false;
    }

    private async Task AddOrIncrementProductAsync(Product product)
    {
        var price = _suggestedPrices.GetValueOrDefault(product.Id);
        int? pricingTypeId = _defaultPricingTypeByProduct.TryGetValue(product.Id, out var tid) ? tid : null;
        if (price <= 0)
        {
            BeautifulMessageDialog.ShowWarning($"لا يوجد سعر سابق لـ «{product.Name}» — أدخل السعر من السلة");
            price = 0;
        }

        if (ShowClothingSizes && _productSizeService is not null
            && await _productSizeService.HasSizesAsync(product.Id))
        {
            await AddClothingProductAsync(product, price, pricingTypeId);
            return;
        }

        // Merge key includes size/color/serial/custom when relevant
        var existing = FindMergeableLine(product.Id, pricingTypeId, sizeId: null, colorId: null, serial: null);
        if (existing is not null && !ShowSerialNumbers)
        {
            existing.Quantity += 1;
            StatusMessage = $"زيادة كمية {product.Name}";
            return;
        }

        var line = PosCartLine.FromProduct(product, price, 1m, pricingTypeId, ShowProductDiscount);
        ApplyTemplateLabelsToLine(line);
        await EnrichLineFeatureDataAsync(line);
        CartLines.Add(line);
        StatusMessage = $"أُضيف {product.Name}";
    }

    private async Task AddClothingProductAsync(Product product, decimal price, int? pricingTypeId)
    {
        var selection = await ClothingSizeInvoiceHelper.PromptAsync(
            _productSizeService!,
            product,
            SelectedWarehouse?.Id,
            isSale: true,
            price,
            pricingTypeId,
            pricingTypeName: null);

        if (selection is null)
        {
            StatusMessage = "تم إلغاء اختيار القياس";
            return;
        }

        ProductColor? chosenColor = null;
        if (_productColorService is not null)
        {
            var colors = await _productColorService.GetByProductAsync(product.Id);
            if (colors.Count > 0)
            {
                var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                            ?? Application.Current.MainWindow;
                var (color, cancelled) = ProductColorPickDialog.ShowForProduct(owner, product, colors);
                if (cancelled)
                {
                    StatusMessage = "تم إلغاء اختيار اللون";
                    return;
                }
                chosenColor = color;
            }
        }

        foreach (var (sizeId, sizeName, qty) in selection.Lines)
        {
            var existing = CartLines.FirstOrDefault(l =>
                l.ProductId == product.Id
                && l.PricingTypeId == pricingTypeId
                && l.ProductSizeId == sizeId
                && l.ProductColorId == chosenColor?.Id);

            if (existing is not null)
            {
                existing.Quantity += qty;
                continue;
            }

            var line = PosCartLine.FromProduct(
                product, price, qty, pricingTypeId, ShowProductDiscount,
                sizeId, sizeName, chosenColor?.Id, chosenColor?.ColorName);

            ApplyTemplateLabelsToLine(line);
            await EnrichLineFeatureDataAsync(line);
            CartLines.Add(line);
        }

        StatusMessage = $"أُضيف {product.Name} بالقياسات المختارة";
    }

    private PosCartLine? FindMergeableLine(int productId, int? pricingTypeId, int? sizeId, int? colorId, string? serial)
    {
        return CartLines.FirstOrDefault(l =>
            l.ProductId == productId
            && l.PricingTypeId == pricingTypeId
            && l.ProductSizeId == sizeId
            && l.ProductColorId == colorId
            && string.Equals(l.SerialNumber ?? string.Empty, serial ?? string.Empty, StringComparison.OrdinalIgnoreCase));
    }

    private void ApplyTemplateLabelsToLine(PosCartLine line)
    {
        if (ShowClothingSizes)
        {
            if (string.IsNullOrWhiteSpace(line.CustomField1Label))
                line.CustomField1Label = ClothingSizeInvoiceHelper.SizeLabel;
            if (string.IsNullOrWhiteSpace(line.CustomField2Label))
                line.CustomField2Label = ClothingSizeInvoiceHelper.ColorLabel;
            return;
        }

        if (!string.IsNullOrWhiteSpace(CustomField1Header))
            line.CustomField1Label = CustomField1Header;
        if (!string.IsNullOrWhiteSpace(CustomField2Header))
            line.CustomField2Label = CustomField2Header;
    }

    private async Task EnrichLineFeatureDataAsync(PosCartLine line)
    {
        if (ShowExpiryTracking && SelectedWarehouse is not null)
        {
            var batches = await _productBatchService.GetByProductAsync(
                line.ProductId, SelectedWarehouse.Id, inStockOnly: true);
            line.AvailableBatches.Clear();
            foreach (var b in batches)
                line.AvailableBatches.Add(b);
            // FEFO default: earliest expiry
            line.SelectedBatch = batches
                .OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue)
                .FirstOrDefault();
        }

        if (ShowSerialNumbers && _productSerialService is not null)
        {
            var serials = await _productSerialService.GetAvailableAsync(line.ProductId);
            line.AvailableSerials.Clear();
            foreach (var s in serials)
                line.AvailableSerials.Add(s.SerialNumber);
            if (serials.Count == 1)
                line.SerialNumber = serials[0].SerialNumber;
        }

        if (ShowProductPricing)
        {
            var prices = await _productPriceService.GetByProductIdsAsync([line.ProductId]);
            line.AvailablePricingOptions.Clear();
            foreach (var p in prices.OrderByDescending(x => x.PricingType?.IsDefault == true))
            {
                line.AvailablePricingOptions.Add(new ProductPricingOption
                {
                    PricingTypeId = p.PricingTypeId,
                    Name = p.PricingType?.Name ?? "",
                    Price = p.SalePrice,
                    IsDefault = p.PricingType?.IsDefault == true
                });
            }

            var preferred = line.AvailablePricingOptions.FirstOrDefault(o => o.PricingTypeId == line.PricingTypeId)
                            ?? line.AvailablePricingOptions.FirstOrDefault(o => o.IsDefault)
                            ?? line.AvailablePricingOptions.FirstOrDefault();
            if (preferred is not null)
                line.SelectedPricingOption = preferred;
        }

        if (ShowClothingSizes && _productColorService is not null && line.AvailableColors.Count == 0)
        {
            var colors = await _productColorService.GetByProductAsync(line.ProductId);
            foreach (var c in colors)
                line.AvailableColors.Add(c);
            if (line.ProductColorId is int cid)
                line.SelectedColor = colors.FirstOrDefault(c => c.Id == cid);
        }
    }

    private async Task ApplyPosFeatureSideEffectsOnSaveAsync(IReadOnlyList<PosCartLine> lines, IReadOnlyList<InvoiceItem> savedItems)
    {
        for (var i = 0; i < lines.Count && i < savedItems.Count; i++)
        {
            var line = lines[i];
            var item = savedItems[i];
            var stockQty = Math.Abs(line.Quantity);
            if (stockQty <= 0) continue;

            if (ShowExpiryTracking && SelectedWarehouse is not null)
            {
                if (line.BatchId is int batchId)
                {
                    var selected = line.AvailableBatches.FirstOrDefault(b => b.Id == batchId)
                                   ?? (await _productBatchService.GetByProductAsync(line.ProductId, SelectedWarehouse.Id, inStockOnly: true))
                                       .FirstOrDefault(b => b.Id == batchId);
                    if (selected is not null && selected.Quantity >= stockQty)
                    {
                        await _productBatchService.DeductAsync(batchId, stockQty);
                    }
                    else
                    {
                        var allocations = await _productBatchService.AllocateFefoAsync(
                            line.ProductId, SelectedWarehouse.Id, stockQty);
                        await _productBatchService.DeductAllocationsAsync(allocations);
                    }
                }
                else
                {
                    var allocations = await _productBatchService.AllocateFefoAsync(
                        line.ProductId, SelectedWarehouse.Id, stockQty);
                    await _productBatchService.DeductAllocationsAsync(allocations);
                }
            }

            if (ShowSerialNumbers && _productSerialService is not null
                && !string.IsNullOrWhiteSpace(line.SerialNumber))
            {
                await _productSerialService.MarkSoldAsync(line.SerialNumber, line.ProductId, item.Id);
            }

            if (ShowClothingSizes && _productSizeService is not null
                && line.ProductSizeId is int sizeId
                && SelectedWarehouse is not null)
            {
                await _productSizeService.DeductStockAsync(line.ProductId, sizeId, SelectedWarehouse.Id, stockQty);
            }
        }
    }
}
