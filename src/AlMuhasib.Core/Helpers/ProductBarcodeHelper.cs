using System.Linq.Expressions;
using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Helpers;

/// <summary>
/// عدة باركودات لنفس المنتج داخل حقل واحد، مفصولة بـ |.
/// مثال: 123|456|789
/// </summary>
public static class ProductBarcodeHelper
{
    public const char Separator = '|';
    public const string SeparatorString = "|";
    public const int MaxStoredLength = 500;

    /// <summary>يقصّ الأجزاء، يزيل الفراغات الفارغة، ويزيل التكرار (بدون تمييز حالة).</summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var parts = value
            .Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => p.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return parts.Length == 0 ? null : string.Join(Separator, parts);
    }

    public static IReadOnlyList<string> Split(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Array.Empty<string>();

        return value
            .Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => p.Length > 0)
            .ToArray();
    }

    /// <summary>أول باركود — للطباعة أو العرض المختصر.</summary>
    public static string? Primary(string? value)
    {
        var parts = Split(value);
        return parts.Count == 0 ? null : parts[0];
    }

    public static bool IsWithinLength(string? normalized) =>
        normalized is null || normalized.Length <= MaxStoredLength;

    /// <summary>تطابق تام (بدون تمييز حالة) مع أي باركود فرعي في الحقل.</summary>
    public static bool Matches(string? storedField, string? scannedOrLookup)
    {
        if (string.IsNullOrWhiteSpace(storedField) || string.IsNullOrWhiteSpace(scannedOrLookup))
            return false;

        var code = scannedOrLookup.Trim();
        foreach (var part in Split(storedField))
        {
            if (string.Equals(part, code, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>هل يشارك الحقل أي باركود مع القائمة المعطاة؟</summary>
    public static bool SharesAnyToken(string? storedField, IEnumerable<string> tokens)
    {
        if (string.IsNullOrWhiteSpace(storedField))
            return false;

        foreach (var token in tokens)
        {
            if (Matches(storedField, token))
                return true;
        }

        return false;
    }

    /// <summary>
    /// تعبير EF يطابق باركوداً فرعياً دون تطابق جزئي داخل رقم أطول
    /// (مساواة كاملة أو محاط بفاصل |).
    /// </summary>
    public static Expression<Func<Product, bool>> MatchesExpression(string barcode)
    {
        var code = barcode.Trim();
        var prefix = code + SeparatorString;
        var suffix = SeparatorString + code;
        var middle = SeparatorString + code + SeparatorString;

        return p => p.Barcode != null && (
            p.Barcode == code
            || p.Barcode.StartsWith(prefix)
            || p.Barcode.EndsWith(suffix)
            || p.Barcode.Contains(middle));
    }
}
