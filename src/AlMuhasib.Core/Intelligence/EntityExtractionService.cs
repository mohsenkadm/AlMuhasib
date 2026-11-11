using System.Globalization;
using System.Text.RegularExpressions;

namespace AlMuhasib.Core.Intelligence;

/// <summary>يستخرج الفترة والعميل والعدد من السؤال المطبّع.</summary>
public static partial class EntityExtractionService
{
    public static QueryEntities Extract(string normalized, ConversationContext context, Understanding understanding)
    {
        var entities = new QueryEntities
        {
            TopCount = ExtractTopCount(normalized)
        };

        if (TryExplicitRange(normalized, out var from, out var to))
        {
            entities.HasPeriod = true;
            entities.From = from;
            entities.To = to;
            entities.PeriodLabel = "من " + ArabicTextNormalizer.FormatDate(from) + " إلى " + ArabicTextNormalizer.FormatDate(to);
        }
        else
        {
            FillNamedPeriod(normalized, entities);
        }

        if (understanding.IsComparison && entities.HasPeriod && !entities.HasComparePeriod)
            FillPreviousWindow(entities);

        entities.CustomerNameQuery = ExtractCustomerName(normalized);
        entities.UseContextCustomer = understanding.Intent == AccountingIntent.CustomerPurchases
                                       && string.IsNullOrWhiteSpace(entities.CustomerNameQuery)
                                       && context.CustomerId is not null;
        return entities;
    }

    private static void FillNamedPeriod(string text, QueryEntities entities)
    {
        var today = DateTime.Today;
        var hasToday = text.Contains("اليوم", StringComparison.Ordinal);
        var hasYesterday = text.Contains("امس", StringComparison.Ordinal) || text.Contains("البارحه", StringComparison.Ordinal);
        var hasThisMonth = text.Contains("هذا الشهر", StringComparison.Ordinal)
                           || text.Contains("الشهر الحالي", StringComparison.Ordinal)
                           || text.Contains("هالشهر", StringComparison.Ordinal);
        var hasLastMonth = text.Contains("الشهر الماضي", StringComparison.Ordinal)
                           || text.Contains("الشهر السابق", StringComparison.Ordinal);
        var hasThisYear = text.Contains("هذه السنه", StringComparison.Ordinal)
                          || text.Contains("السنه الحاليه", StringComparison.Ordinal)
                          || text.Contains("هالسنه", StringComparison.Ordinal);
        var hasLastYear = text.Contains("السنه الماضيه", StringComparison.Ordinal);
        var hasLast7 = text.Contains("اخر 7", StringComparison.Ordinal);
        var hasLast30 = text.Contains("اخر 30", StringComparison.Ordinal);

        if (hasThisMonth && hasLastMonth)
        {
            SetMonth(entities, today, current: true);
            SetMonth(entities, today, current: false, compare: true);
            return;
        }

        if (hasToday)
        {
            Set(entities, today, today, "اليوم");
            return;
        }

        if (hasYesterday)
        {
            var day = today.AddDays(-1);
            Set(entities, day, day, "أمس");
            return;
        }

        if (hasLast7)
        {
            Set(entities, today.AddDays(-6), today, "آخر 7 أيام");
            return;
        }

        if (hasLast30)
        {
            Set(entities, today.AddDays(-29), today, "آخر 30 يوماً");
            return;
        }

        if (hasThisMonth)
        {
            SetMonth(entities, today, current: true);
            return;
        }

        if (hasLastMonth)
        {
            SetMonth(entities, today, current: false);
            return;
        }

        if (hasThisYear)
        {
            Set(entities, new DateTime(today.Year, 1, 1), today, "هذه السنة حتى اليوم");
            return;
        }

        if (hasLastYear)
        {
            Set(entities, new DateTime(today.Year - 1, 1, 1), new DateTime(today.Year - 1, 12, 31), "السنة الماضية");
        }
    }

    private static void SetMonth(QueryEntities entities, DateTime today, bool current, bool compare = false)
    {
        var startThis = new DateTime(today.Year, today.Month, 1);
        if (current)
        {
            Set(entities, startThis, today, "هذا الشهر حتى اليوم");
            return;
        }

        var lastEnd = startThis.AddDays(-1);
        var lastStart = new DateTime(lastEnd.Year, lastEnd.Month, 1);
        if (compare)
        {
            entities.HasComparePeriod = true;
            entities.CompareFrom = lastStart;
            entities.CompareTo = lastEnd;
            entities.CompareLabel = "الشهر الماضي";
            return;
        }

        Set(entities, lastStart, lastEnd, "الشهر الماضي");
    }

    private static void Set(QueryEntities entities, DateTime from, DateTime to, string label)
    {
        entities.HasPeriod = true;
        entities.From = from;
        entities.To = to;
        entities.PeriodLabel = label;
    }

    private static void FillPreviousWindow(QueryEntities entities)
    {
        if (entities.From is null || entities.To is null)
            return;

        var days = (entities.To.Value.Date - entities.From.Value.Date).Days + 1;
        if (days < 1)
            days = 1;
        var compareTo = entities.From.Value.Date.AddDays(-1);
        entities.HasComparePeriod = true;
        entities.CompareTo = compareTo;
        entities.CompareFrom = compareTo.AddDays(-(days - 1));
        entities.CompareLabel = "الفترة السابقة";
    }

    private static int ExtractTopCount(string text)
    {
        var match = TopCountRegex().Match(text);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var count))
            return 5;
        return Math.Clamp(count, 1, 20);
    }

    private static string? ExtractCustomerName(string text)
    {
        var marker = text.Contains("العميل ", StringComparison.Ordinal) ? "العميل " : null;
        marker ??= text.Contains("عميل ", StringComparison.Ordinal) ? "عميل " : null;
        if (marker is not null)
        {
            var after = text[(text.IndexOf(marker, StringComparison.Ordinal) + marker.Length)..];
            var name = CleanName(after);
            if (!string.IsNullOrWhiteSpace(name))
                return name;
        }

        const string bought = "اشترى";
        var index = text.IndexOf(bought, StringComparison.Ordinal);
        if (index < 0)
            return null;

        var rest = text[(index + bought.Length)..];
        var candidate = CleanName(rest);
        return string.IsNullOrWhiteSpace(candidate) ? null : candidate;
    }

    private static string CleanName(string text)
    {
        var stripped = IntentRecognitionService.RemovePeriodWords(text);
        string[] drop = ["وماذا", "ماذا", "عن", "و", "كم", "ما", "من", "عندنا", "لدينا", "الذي", "اكثر", "افضل", "عميل", "العميل", "عملاء", "اشترى", "شرى"];
        var tokens = stripped.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(token => !drop.Contains(token, StringComparer.Ordinal))
            .Take(3)
            .ToArray();
        var name = string.Join(' ', tokens).Trim();
        if (name.Length < 2 || name.Length > 40)
            return string.Empty;
        return name;
    }

    private static bool TryExplicitRange(string text, out DateTime from, out DateTime to)
    {
        from = default;
        to = default;
        var match = RangeRegex().Match(text);
        if (!match.Success)
            return false;
        if (!TryParseDate(match.Groups[1].Value, out from) || !TryParseDate(match.Groups[2].Value, out to))
            return false;
        if (to < from)
            (from, to) = (to, from);
        return true;
    }

    private static bool TryParseDate(string value, out DateTime date)
    {
        var parts = value.Split(['/', '-'], StringSplitOptions.RemoveEmptyEntries);
        date = default;
        if (parts.Length != 3)
            return false;
        if (!int.TryParse(parts[0], out var a) || !int.TryParse(parts[1], out var b) || !int.TryParse(parts[2], out var c))
            return false;

        int year, month, day;
        if (a > 31)
        {
            year = a;
            month = b;
            day = c;
        }
        else
        {
            day = a;
            month = b;
            year = c < 100 ? 2000 + c : c;
        }

        try
        {
            date = new DateTime(year, month, day);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    [GeneratedRegex(@"(?:افضل|اكثر)\s+(\d{1,2})")]
    private static partial Regex TopCountRegex();

    [GeneratedRegex(@"من\s+(\d{1,4}[/-]\d{1,2}[/-]\d{1,4})\s+الى\s+(\d{1,4}[/-]\d{1,2}[/-]\d{1,4})")]
    private static partial Regex RangeRegex();
}
