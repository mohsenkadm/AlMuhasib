using System.Text.RegularExpressions;

namespace AlMuhasib.Core.Intelligence;

/// <summary>يرفض النص الذي هو أمر قاعدة بيانات، ولا يمرّره إلى أي تنفيذ.</summary>
public static partial class SqlTextGuard
{
    private static readonly string[] Keywords =
    [
        "drop", "delete", "insert", "update", "alter", "truncate",
        "exec", "execute", "union", "select", "xp_"
    ];

    public static bool IsHostile(string normalized)
    {
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        if (!ContainsKeyword(normalized))
            return false;

        return !ArabicTextNormalizer.ContainsArabic(normalized);
    }

    public static string Strip(string normalized)
    {
        if (string.IsNullOrWhiteSpace(normalized) || !ContainsKeyword(normalized))
            return normalized;

        var text = KeywordRegex().Replace(normalized, " ");
        return ArabicTextNormalizer.Collapse(text);
    }

    private static bool ContainsKeyword(string text)
    {
        foreach (var keyword in Keywords)
        {
            if (ContainsWord(text, keyword))
                return true;
        }

        return text.Contains("--", StringComparison.Ordinal)
               || text.Contains("/*", StringComparison.Ordinal);
    }

    private static bool ContainsWord(string text, string word)
    {
        var index = 0;
        while ((index = text.IndexOf(word, index, StringComparison.Ordinal)) >= 0)
        {
            var before = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
            var end = index + word.Length;
            var after = end >= text.Length || !char.IsLetterOrDigit(text[end]);
            if (before && after)
                return true;
            index = end;
        }

        return false;
    }

    [GeneratedRegex(@"\b(drop|delete|insert|update|alter|truncate|exec|execute|union|select|xp_)\b", RegexOptions.CultureInvariant)]
    private static partial Regex KeywordRegex();
}
