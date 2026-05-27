using System.Collections.ObjectModel;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Helpers;

public static class InvoiceProductMergeHelper
{
    public static void Merge(
        IReadOnlyList<ProductPickerResult> picks,
        ObservableCollection<InvoiceItemRow> items,
        Action<InvoiceItemRow> wireRow,
        Action<InvoiceItemRow> unwireRow)
    {
        foreach (var pick in picks)
        {
            if (pick.Quantity <= 0)
                continue;

            var existing = items.FirstOrDefault(i => i.ProductId == pick.Product.Id);
            if (existing is not null)
            {
                existing.Quantity += pick.Quantity;
                if (existing.UnitPrice <= 0 && pick.SuggestedUnitPrice > 0)
                    existing.UnitPrice = pick.SuggestedUnitPrice;
                continue;
            }

            var row = new InvoiceItemRow
            {
                SelectedProduct = pick.Product,
                Quantity = pick.Quantity,
                UnitPrice = pick.SuggestedUnitPrice
            };
            wireRow(row);
            items.Add(row);
        }

        TrimEmptyRows(items, unwireRow, wireRow);
    }

    public static void TrimEmptyRows(
        ObservableCollection<InvoiceItemRow> items,
        Action<InvoiceItemRow> unwireRow,
        Action<InvoiceItemRow> wireRow)
    {
        var empties = items
            .Where(r => r.ProductId is null
                        && string.IsNullOrWhiteSpace(r.ItemName)
                        && r.UnitPrice <= 0
                        && r.TotalPrice <= 0)
            .ToList();

        foreach (var row in empties)
        {
            unwireRow(row);
            items.Remove(row);
        }

        if (!items.Any(r => r.ProductId is null && string.IsNullOrWhiteSpace(r.ItemName)))
        {
            var manual = new InvoiceItemRow();
            wireRow(manual);
            items.Add(manual);
        }
    }
}
