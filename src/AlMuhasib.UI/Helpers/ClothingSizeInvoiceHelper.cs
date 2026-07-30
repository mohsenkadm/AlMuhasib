using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Helpers;

/// <summary>مساعدة اختيار القياس والكمية عند تفعيل ميزة محلات الألبسة.</summary>
public static class ClothingSizeInvoiceHelper
{
    public const string SizeLabel = "المقاس";
    public const string ColorLabel = "اللون";

    public static async Task<SizeQuantitySelection?> PromptAsync(
        IProductSizeService sizeService,
        Product product,
        int? warehouseId,
        bool isSale,
        decimal unitPrice = 0,
        int? pricingTypeId = null,
        string? pricingTypeName = null,
        IReadOnlyDictionary<int, decimal>? seedQuantities = null)
    {
        var sizes = await sizeService.GetByProductAsync(product.Id);
        if (sizes.Count == 0)
            return null;

        Dictionary<int, decimal>? stockMap = null;
        if (isSale && warehouseId is int whId)
        {
            var stocks = await sizeService.GetStocksAsync(product.Id, whId);
            stockMap = stocks.ToDictionary(s => s.ProductSizeId, s => s.Quantity);
        }

        var hint = isSale
            ? "حدد الكمية لكل قياس. يُخصم المخزون من القياس المختار."
            : "حدد الكمية لكل قياس. يُضاف المخزون إلى القياس المختار.";

        return Application.Current.Dispatcher.Invoke(() =>
            ProductSizeQuantityDialog.ShowForProduct(
                Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current.MainWindow,
                product,
                sizes,
                stockMap,
                showStock: isSale,
                modeHint: hint,
                unitPrice,
                pricingTypeId,
                pricingTypeName,
                seedQuantities));
    }

    public static void ApplySelectionToItems(
        SizeQuantitySelection selection,
        ObservableCollection<InvoiceItemRow> items,
        Action<InvoiceItemRow> wireRow,
        Action<InvoiceItemRow> unwireRow,
        Action<InvoiceItemRow>? applyLabels = null,
        InvoiceItemRow? replaceRow = null)
    {
        if (replaceRow is not null)
        {
            unwireRow(replaceRow);
            items.Remove(replaceRow);
        }

        // إزالة أسطر المنتج بدون قياس إن وُجدت (من نافذة الاختيار قبل فتح الدايلوك)
        var bare = items
            .Where(i => i.ProductId == selection.Product.Id
                        && i.PricingTypeId == selection.PricingTypeId
                        && i.ProductSizeId is null
                        && string.IsNullOrWhiteSpace(i.SizeName))
            .ToList();
        foreach (var row in bare)
        {
            unwireRow(row);
            items.Remove(row);
        }

        foreach (var (sizeId, sizeName, qty) in selection.Lines)
        {
            var existing = items.FirstOrDefault(i =>
                i.ProductId == selection.Product.Id
                && i.PricingTypeId == selection.PricingTypeId
                && i.ProductSizeId == sizeId);

            if (existing is not null)
            {
                existing.Quantity += qty;
                if (existing.UnitPrice <= 0 && selection.UnitPrice > 0)
                    existing.UnitPrice = selection.UnitPrice;
                continue;
            }

            var row = new InvoiceItemRow
            {
                SelectedProduct = selection.Product,
                Quantity = qty,
                UnitPrice = selection.UnitPrice,
                PricingTypeId = selection.PricingTypeId,
                PricingTypeName = selection.PricingTypeName ?? string.Empty,
                ProductSizeId = sizeId,
                SizeName = sizeName,
                CustomField1 = sizeName,
                CustomField1Label = SizeLabel
            };
            applyLabels?.Invoke(row);
            row.CustomField1 = sizeName;
            if (string.IsNullOrWhiteSpace(row.CustomField1Label))
                row.CustomField1Label = SizeLabel;
            wireRow(row);
            items.Add(row);
        }

        InvoiceProductMergeHelper.TrimEmptyRows(items, unwireRow, wireRow);
    }

    public static void EnsureSizeOnRow(InvoiceItemRow row, int sizeId, string sizeName)
    {
        row.ProductSizeId = sizeId;
        row.SizeName = sizeName;
        row.CustomField1 = sizeName;
        if (string.IsNullOrWhiteSpace(row.CustomField1Label))
            row.CustomField1Label = SizeLabel;
    }
}
