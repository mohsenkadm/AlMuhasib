using System.Text.Json;
using AlMuhasib.Core;
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
        if (row.ProductColorId.HasValue)
            dict["__colorId"] = row.ProductColorId.Value.ToString();
        if (!string.IsNullOrWhiteSpace(row.ColorName))
            dict["__color"] = row.ColorName.Trim();
        else if (!string.IsNullOrWhiteSpace(row.CustomField2)
                 && string.Equals(row.CustomField2Label, ClothingSizeInvoiceHelper.ColorLabel, StringComparison.Ordinal))
            dict["__color"] = row.CustomField2.Trim();
        if (row.ProductWeight > 0)
        {
            dict["__weight"] = row.ProductWeight.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(row.ProductWeightUnit))
                dict["__weightUnit"] = row.ProductWeightUnit.Trim();
        }

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
                 && string.Equals(row.CustomField1Label, ClothingSizeInvoiceHelper.SizeLabel, StringComparison.Ordinal))
            row.SizeName = row.CustomField1;

        if (dict.TryGetValue("__colorId", out var colorIdText) && int.TryParse(colorIdText, out var colorId))
            row.ProductColorId = colorId;
        if (dict.TryGetValue("__color", out var colorName) && !string.IsNullOrWhiteSpace(colorName))
            row.ColorName = colorName.Trim();
        else if (dict.TryGetValue(ClothingSizeInvoiceHelper.ColorLabel, out var labelColor)
                 && !string.IsNullOrWhiteSpace(labelColor))
            row.ColorName = labelColor.Trim();

        if (!string.IsNullOrWhiteSpace(row.ColorName) && string.IsNullOrWhiteSpace(row.CustomField2))
            row.CustomField2 = row.ColorName;

        if (dict.TryGetValue("__weight", out var weightText)
            && decimal.TryParse(weightText, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var weight)
            && weight > 0
            && row.ProductWeight <= 0)
            row.ProductWeight = weight;

        if (dict.TryGetValue("__weightUnit", out var weightUnit)
            && !string.IsNullOrWhiteSpace(weightUnit)
            && string.IsNullOrWhiteSpace(row.ProductWeightUnit))
            row.ProductWeightUnit = weightUnit;
    }

    /// <summary>
    /// يعرض اسم الصنف مع تفاصيل الميزات المحفوظة (قياس، لون، وحدة، دفعة، صلاحية، سيريال).
    /// المعامل الثاني هو JSON الحقول المخصصة للسطر.
    /// </summary>
    public static string FormatItemDisplayName(string itemName, string? customFieldsJson)
    {
        var size = ExtractSizeName(customFieldsJson);
        var color = ExtractColorName(customFieldsJson);
        var unit = ExtractField(customFieldsJson, "__unit");
        var batch = ExtractField(customFieldsJson, "__batch");
        var expiry = ExtractField(customFieldsJson, "__expiry");
        var serial = ExtractField(customFieldsJson, "__serial");

        var name = (itemName ?? string.Empty).Trim();
        var attrs = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(size)) attrs.Add(size!);
        if (!string.IsNullOrWhiteSpace(color)) attrs.Add(color!);
        if (attrs.Count > 0)
            name = string.IsNullOrWhiteSpace(name)
                ? string.Join(" — ", attrs)
                : $"{name} — {string.Join(" — ", attrs)}";

        var extras = new List<string>(4);
        if (!string.IsNullOrWhiteSpace(unit)) extras.Add(unit!);
        if (!string.IsNullOrWhiteSpace(batch)) extras.Add($"دفعة: {batch}");
        if (!string.IsNullOrWhiteSpace(expiry)) extras.Add($"انتهاء: {expiry}");
        if (!string.IsNullOrWhiteSpace(serial)) extras.Add($"سيريال: {serial}");

        if (extras.Count == 0)
            return name;
        if (string.IsNullOrWhiteSpace(name))
            return string.Join(" | ", extras);
        return $"{name} | {string.Join(" | ", extras)}";
    }

    public static string? ExtractSizeName(string? json)
    {
        var size = ExtractField(json, "__size");
        if (!string.IsNullOrWhiteSpace(size))
            return size;
        return ExtractField(json, ClothingSizeInvoiceHelper.SizeLabel);
    }

    public static string? ExtractColorName(string? json)
    {
        var color = ExtractField(json, "__color");
        if (!string.IsNullOrWhiteSpace(color))
            return color;
        return ExtractField(json, ClothingSizeInvoiceHelper.ColorLabel);
    }

    public static int? ExtractSizeId(string? json)
    {
        var text = ExtractField(json, "__sizeId");
        return int.TryParse(text, out var id) ? id : null;
    }

    public static int? ExtractColorId(string? json)
    {
        var text = ExtractField(json, "__colorId");
        return int.TryParse(text, out var id) ? id : null;
    }

    private static string? ExtractField(string? json, string key)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
            if (dict is null) return null;
            if (dict.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return value.Trim();
            return null;
        }
        catch
        {
            return null;
        }
    }

    public static decimal ToStockQuantity(InvoiceItemRow row) =>
        ProductDiscountHelper.ToBaseQuantity(row.Quantity, row.UnitConversionFactor);

    /// <summary>
    /// بعد تحميل بند محفوظ بالكميات الأساسية: يعرض الكمية بالتعبئة ويُبقي السعر للوحدة الأساسية.
    /// </summary>
    public static void ApplyPackDisplayFromStored(InvoiceItemRow row, decimal storedQuantity, decimal storedUnitPrice)
    {
        var factor = ProductDiscountHelper.NormalizeConversionFactor(row.UnitConversionFactor);
        if (factor != 1m)
        {
            row.Quantity = storedQuantity / factor;
            row.UnitPrice = storedUnitPrice;
        }
        else
        {
            row.Quantity = storedQuantity;
            row.UnitPrice = storedUnitPrice;
        }
    }

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
