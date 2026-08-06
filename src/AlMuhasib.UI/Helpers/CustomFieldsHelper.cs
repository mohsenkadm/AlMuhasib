using System.Globalization;
using System.Text.Json;
using System.Windows.Data;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.CustomFields;

namespace AlMuhasib.UI.Helpers;

public static class CustomFieldsHelper
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string SlotKey(int slot) => $"cf{slot}";

    public static Dictionary<string, string> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions)
                   ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static string? Serialize(IDictionary<string, string> values)
    {
        var cleaned = values
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value.Trim(), StringComparer.OrdinalIgnoreCase);

        return cleaned.Count == 0 ? null : JsonSerializer.Serialize(cleaned, JsonOptions);
    }

    public static string GetDisplayValue(string? json, int slot, CustomFieldValueType type = CustomFieldValueType.Text)
    {
        var dict = Parse(json);
        if (!dict.TryGetValue(SlotKey(slot), out var raw) || string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        raw = raw.Trim();
        if (type == CustomFieldValueType.Boolean
            || string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase))
            return IsTruthy(raw) ? "نعم" : "لا";

        return raw;
    }

    public static bool IsTruthy(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "نعم", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);

    public static string FormatBooleanStorage(bool value) => value ? "true" : "false";
}

/// <summary>يعرض قيمة حقل مخصص من CustomFieldsJson حسب رقم الفتحة (ConverterParameter = 1..8).</summary>
public sealed class CustomFieldSlotConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!int.TryParse(parameter?.ToString(), out var slot))
            return string.Empty;

        return CustomFieldsHelper.GetDisplayValue(value as string, slot);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
