using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Helpers;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Helpers;

public static class InvoiceBarcodeHelper
{
    /// <param name="onRowUpdated">
    /// يُستدعى بعد إضافة/تحديث السطر — في فاتورة البيع يحمّل التسعير والرصيد عبر RefreshProductRowAsync.
    /// </param>
    public static bool TryAddByBarcode(
        string barcode,
        IEnumerable<Product> products,
        ObservableCollection<InvoiceItemRow> items,
        Action<InvoiceItemRow> wireRow,
        Action<InvoiceItemRow> unwireRow,
        Action<InvoiceItemRow>? onRowUpdated,
        out string errorMessage)
    {
        errorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(barcode))
        {
            errorMessage = "أدخل الباركود أولاً";
            return false;
        }

        var tokens = ProductBarcodeHelper.Split(barcode.Trim());
        if (tokens.Count == 0)
        {
            errorMessage = "أدخل الباركود أولاً";
            return false;
        }

        var missing = new List<string>();
        var addedAny = false;

        foreach (var code in tokens)
        {
            var product = products.FirstOrDefault(p => ProductBarcodeHelper.Matches(p.Barcode, code));
            if (product is null)
            {
                missing.Add(code);
                continue;
            }

            // فضّل سطر نفس المنتج ونوع التسعير الحالي إن وُجد؛ وإلا أول سطر للمنتج
            var existing = items.FirstOrDefault(i =>
                             i.ProductId == product.Id && i.PricingTypeId is not null)
                         ?? items.FirstOrDefault(i => i.ProductId == product.Id);
            if (existing is not null)
            {
                existing.Quantity += 1;
                onRowUpdated?.Invoke(existing);
            }
            else
            {
                var row = new InvoiceItemRow
                {
                    SelectedProduct = product,
                    Quantity = 1
                };
                wireRow(row);
                items.Add(row);
                onRowUpdated?.Invoke(row);
            }

            addedAny = true;
        }

        InvoiceProductMergeHelper.TrimEmptyRows(items, unwireRow, wireRow);

        if (!addedAny)
        {
            errorMessage = tokens.Count == 1
                ? $"لا يوجد منتج بالباركود: {tokens[0]}"
                : $"لا يوجد منتج لهذه الباركودات: {string.Join(" | ", missing)}";
            return false;
        }

        if (missing.Count > 0)
            errorMessage = $"تمت الإضافة جزئياً — غير موجود: {string.Join(" | ", missing)}";

        return true;
    }
}
