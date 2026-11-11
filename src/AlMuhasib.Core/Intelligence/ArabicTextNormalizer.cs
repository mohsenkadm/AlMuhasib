using System.Globalization;
using System.Text;

namespace AlMuhasib.Core.Intelligence;

/// <summary>تطبيع الحروف والأرقام العربية دون أي نموذج لغوي.</summary>
public static class ArabicTextNormalizer
{
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var builder = new StringBuilder(input.Length);
        foreach (var ch in input.Trim().ToLowerInvariant())
        {
            var mapped = ch switch
            {
                'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',
                'ى' => 'ي',
                'ة' => 'ه',
                'ؤ' => 'و',
                'ئ' => 'ي',
                'ـ' => '\0',
                _ when ch is >= '\u064B' and <= '\u0652' => '\0',
                '\u0670' => '\0',
                _ => MapDigit(ch)
            };

            if (mapped == '\0')
                continue;

            if (char.IsLetterOrDigit(mapped))
                builder.Append(mapped);
            else if (char.IsWhiteSpace(mapped))
                builder.Append(' ');
            else
                builder.Append(' ');
        }

        return Collapse(builder.ToString());
    }

    private static char MapDigit(char ch)
    {
        var value = ch switch
        {
            '٠' or '۰' => '0',
            '١' or '۱' => '1',
            '٢' or '۲' => '2',
            '٣' or '۳' => '3',
            '٤' or '۴' => '4',
            '٥' or '۵' => '5',
            '٦' or '۶' => '6',
            '٧' or '۷' => '7',
            '٨' or '۸' => '8',
            '٩' or '۹' => '9',
            _ => ch
        };
        return value;
    }

    public static string Collapse(string text)
    {
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts);
    }

    public static bool ContainsArabic(string text)
    {
        foreach (var ch in text)
        {
            if (ch is >= '\u0600' and <= '\u06FF')
                return true;
        }

        return false;
    }

    public static string FormatDate(DateTime date) =>
        date.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
}
