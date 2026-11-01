using System.Linq.Expressions;
using System.Text;
using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Helpers;

/// <summary>
/// عدة باركودات لنفس المنتج داخل حقل واحد، مفصولة بـ |.
/// نوع التخزين: انفاركار ماكس — يقبل الفراغ أو سلسلة طويلة من الباركودات.
/// مثال: 123|456|789
/// </summary>
public static class ProductBarcodeHelper
{
    public const char Separator = '|';
    public const string SeparatorString = "|";

    /// <summary>اسم نوع الباركود المعتمد للمنتجات (عدة باركودات + فراغ).</summary>
    public const string InfarkarMaxTypeName = "انفاركار ماكس";

    /// <summary>الحد الأقصى لطول حقل الباركود بعد التطبيع (انفاركار ماكس).</summary>
    public const int MaxStoredLength = 2000;

    /// <summary>فواصل شبيهة بـ | من اللصق/الاستيراد تُوحَّد قبل التقسيم.</summary>
    private static readonly char[] AlternateSeparators =
    [
        '\uFF5C', // FULLWIDTH VERTICAL LINE ｜
        '\u00A6', // BROKEN BAR ¦
        '\u2502', // BOX DRAWINGS LIGHT VERTICAL │
        '\u2223', // DIVIDES ∣
        '\u01C0', // LATIN LETTER DENTAL CLICK ǀ
    ];

    /// <summary>يقصّ الأجزاء، يزيل الفراغات الفارغة، ويزيل التكرار (بدون تمييز حالة). الفراغ → null.</summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var parts = Split(value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return parts.Length == 0 ? null : string.Join(Separator, parts);
    }

    public static IReadOnlyList<string> Split(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Array.Empty<string>();

        var unified = UnifySeparators(value.Trim());
        return unified
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

    /// <summary>
    /// تطابق تام (بدون تمييز حالة) مع أي باركود فرعي في الحقل.
    /// إذا كان المدخل نفسه يحتوي | يُطابق عند وجود كل الأجزاء في نفس المنتج.
    /// </summary>
    public static bool Matches(string? storedField, string? scannedOrLookup)
    {
        if (string.IsNullOrWhiteSpace(storedField) || string.IsNullOrWhiteSpace(scannedOrLookup))
            return false;

        var lookupTokens = Split(scannedOrLookup);
        if (lookupTokens.Count == 0)
            return false;

        var storedTokens = Split(storedField);
        if (storedTokens.Count == 0)
            return false;

        // مدخل متعدد: المنتج يطابق إذا وُجدت كل الأجزاء فيه (نفس بطاقة المنتج)
        if (lookupTokens.Count > 1)
        {
            return lookupTokens.All(code =>
                storedTokens.Any(part => string.Equals(part, code, StringComparison.OrdinalIgnoreCase)));
        }

        var single = lookupTokens[0];
        foreach (var part in storedTokens)
        {
            if (string.Equals(part, single, StringComparison.OrdinalIgnoreCase))
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
        var code = UnifySeparators(barcode.Trim());
        // إن وُجدت عدة توكنات في المدخل، نستخدم الأول للتعبير SQL (الكاشير يمرّر توكن واحد عادة)
        var pipeIdx = code.IndexOf(Separator);
        if (pipeIdx >= 0)
            code = code[..pipeIdx].Trim();

        var prefix = code + SeparatorString;
        var suffix = SeparatorString + code;
        var middle = SeparatorString + code + SeparatorString;

        return p => p.Barcode != null && (
            p.Barcode == code
            || p.Barcode.StartsWith(prefix)
            || p.Barcode.EndsWith(suffix)
            || p.Barcode.Contains(middle));
    }

    /// <summary>يستبدل الفواصل الشبيهة بـ | القياسي.</summary>
    public static string UnifySeparators(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var needsReplace = false;
        foreach (var ch in value)
        {
            if (IsAlternateSeparator(ch))
            {
                needsReplace = true;
                break;
            }
        }

        if (!needsReplace)
            return value;

        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
            sb.Append(IsAlternateSeparator(ch) ? Separator : ch);
        return sb.ToString();
    }

    private static bool IsAlternateSeparator(char ch)
    {
        foreach (var alt in AlternateSeparators)
        {
            if (ch == alt)
                return true;
        }

        return false;
    }
}
