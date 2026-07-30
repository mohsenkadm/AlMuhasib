using System.Text.Json;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Helpers;

public static class InvoiceCustomFieldsHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string? ToJson(InvoiceItemRow row, IReadOnlyList<string>? labels = null)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (labels is { Count: > 0 })
        {
            if (labels.Count > 0 && !string.IsNullOrWhiteSpace(row.CustomField1))
                dict[labels[0]] = row.CustomField1.Trim();
            if (labels.Count > 1 && !string.IsNullOrWhiteSpace(row.CustomField2))
                dict[labels[1]] = row.CustomField2.Trim();
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(row.CustomField1Label) && !string.IsNullOrWhiteSpace(row.CustomField1))
                dict[row.CustomField1Label] = row.CustomField1.Trim();
            if (!string.IsNullOrWhiteSpace(row.CustomField2Label) && !string.IsNullOrWhiteSpace(row.CustomField2))
                dict[row.CustomField2Label] = row.CustomField2.Trim();
        }

        if (!string.IsNullOrWhiteSpace(row.SelectedUnitName))
            dict["__unit"] = row.SelectedUnitName.Trim();
        if (row.UnitConversionFactor != 1m)
            dict["__unitFactor"] = row.UnitConversionFactor.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(row.BatchNumber))
            dict["__batch"] = row.BatchNumber.Trim();
        if (row.ExpiryDate.HasValue)
            dict["__expiry"] = row.ExpiryDate.Value.ToString("yyyy-MM-dd");
        if (row.BatchId.HasValue)
            dict["__batchId"] = row.BatchId.Value.ToString();
        if (!string.IsNullOrWhiteSpace(row.SerialNumber))
            dict["__serial"] = row.SerialNumber.Trim();
        if (row.ProductSizeId.HasValue)
            dict["__sizeId"] = row.ProductSizeId.Value.ToString();
        if (!string.IsNullOrWhiteSpace(row.SizeName))
            dict["__size"] = row.SizeName.Trim();

        return dict.Count == 0 ? null : JsonSerializer.Serialize(dict, JsonOptions);
    }

    public static void ApplyFromJson(InvoiceItemRow row, string? json, IReadOnlyList<string>? labels = null)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        Dictionary<string, string>? dict;
        try
        {
            dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
        }
        catch
        {
            return;
        }

        if (dict is null || dict.Count == 0)
            return;

        if (labels is { Count: > 0 })
        {
            if (labels.Count > 0 && dict.TryGetValue(labels[0], out var v1))
                row.CustomField1 = v1;
            if (labels.Count > 1 && dict.TryGetValue(labels[1], out var v2))
                row.CustomField2 = v2;
        }

        if (dict.TryGetValue("__unit", out var unit))
            row.SelectedUnitName = unit;
        if (dict.TryGetValue("__unitFactor", out var factorText)
            && decimal.TryParse(factorText, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var factor))
            row.UnitConversionFactor = factor;
        if (dict.TryGetValue("__batch", out var batch))
            row.BatchNumber = batch;
        if (dict.TryGetValue("__expiry", out var expiryText) && DateTime.TryParse(expiryText, out var expiry))
            row.ExpiryDate = expiry;
        if (dict.TryGetValue("__batchId", out var batchIdText) && int.TryParse(batchIdText, out var batchId))
            row.BatchId = batchId;
        if (dict.TryGetValue("__serial", out var serial))
            row.SerialNumber = serial;
        if (dict.TryGetValue("__sizeId", out var sizeIdText) && int.TryParse(sizeIdText, out var sizeId))
            row.ProductSizeId = sizeId;
        if (dict.TryGetValue("__size", out var sizeName))
            row.SizeName = sizeName;
        else if (string.IsNullOrWhiteSpace(row.SizeName) && !string.IsNullOrWhiteSpace(row.CustomField1)
                 && string.Equals(row.CustomField1Label, "المقاس", StringComparison.Ordinal))
            row.SizeName = row.CustomField1;
    }

    /// <summary>يعرض اسم الصنف مع القياس إن وُجد (للفواتير والطباعة).</summary>
    public static string FormatItemDisplayName(string itemName, string? sizeName)
    {
        if (string.IsNullOrWhiteSpace(sizeName))
            return itemName;
        if (string.IsNullOrWhiteSpace(itemName))
            return sizeName.Trim();
        return $"{itemName.Trim()} — {sizeName.Trim()}";
    }

    public static string? ExtractSizeName(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
            if (dict is null) return null;
            if (dict.TryGetValue("__size", out var size) && !string.IsNullOrWhiteSpace(size))
                return size.Trim();
            if (dict.TryGetValue("المقاس", out var labelSize) && !string.IsNullOrWhiteSpace(labelSize))
                return labelSize.Trim();
            return null;
        }
        catch
        {
            return null;
        }
    }

    public static int? ExtractSizeId(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
            if (dict is null) return null;
            if (dict.TryGetValue("__sizeId", out var idText) && int.TryParse(idText, out var id))
                return id;
            return null;
        }
        catch
        {
            return null;
        }
    }

    public static decimal ToStockQuantity(InvoiceItemRow row) =>
        row.Quantity * (row.UnitConversionFactor <= 0 ? 1m : row.UnitConversionFactor);

    /// <summary>يعيد تسميات الحقول العامة (غير المفاتيح الداخلية __*) من JSON محفوظ.</summary>
    public static IReadOnlyList<string> ExtractPublicLabels(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
            if (dict is null || dict.Count == 0)
                return [];

            return dict.Keys
                .Where(k => !k.StartsWith("__", StringComparison.Ordinal))
                .Take(2)
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}
