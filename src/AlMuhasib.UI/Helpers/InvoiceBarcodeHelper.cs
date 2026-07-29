using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
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

        var code = barcode.Trim();
        var product = products.FirstOrDefault(p =>
            !string.IsNullOrWhiteSpace(p.Barcode)
            && string.Equals(p.Barcode.Trim(), code, StringComparison.OrdinalIgnoreCase));

        if (product is null)
        {
            errorMessage = $"لا يوجد منتج بالباركود: {code}";
            return false;
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
            // يستدعي المستدعي تحميل أسعار الكتالوج (النوع الافتراضي) عبر onRowUpdated
            onRowUpdated?.Invoke(row);
        }

        InvoiceProductMergeHelper.TrimEmptyRows(items, unwireRow, wireRow);
        return true;
    }
}
