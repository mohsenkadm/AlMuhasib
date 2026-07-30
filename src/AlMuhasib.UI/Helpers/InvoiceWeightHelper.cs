using System.Globalization;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Helpers;

/// <summary>تجميع أوزان بنود الفاتورة حسب وحدة الوزن دون خلط وحدات مختلفة.</summary>
public static class InvoiceWeightHelper
{
    public static string BuildSummaryText(IEnumerable<InvoiceItemRow> items)
    {
        var totals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.ItemName))
                continue;

            var lineWeight = item.LineWeight;
            if (lineWeight <= 0)
                continue;

            var unit = string.IsNullOrWhiteSpace(item.ProductWeightUnit)
                ? "وحدة"
                : item.ProductWeightUnit.Trim();

            totals[unit] = totals.TryGetValue(unit, out var existing)
                ? existing + lineWeight
                : lineWeight;
        }

        if (totals.Count == 0)
            return "لا يوجد وزن محدد";

        return string.Join(" · ", totals.Select(kv =>
            $"{FormatWeight(kv.Value)} {kv.Key}"));
    }

    public static bool HasAnyWeight(IEnumerable<InvoiceItemRow> items) =>
        items.Any(i => !string.IsNullOrWhiteSpace(i.ItemName) && i.LineWeight > 0);

    private static string FormatWeight(decimal value)
    {
        if (value == decimal.Truncate(value))
            return ((long)value).ToString(CultureInfo.InvariantCulture);
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
