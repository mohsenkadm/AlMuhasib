using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AlMuhasib.UI.Helpers;

/// <summary>يبني مقاطع نص مع تمييز أصفر للكلمات المطابقة لعبارة البحث.</summary>
public static class HighlightTextHelper
{
    public static readonly Brush HighlightBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xF5, 0x9D));
    public static readonly Brush HighlightForeground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));

    static HighlightTextHelper()
    {
        HighlightBrush.Freeze();
        HighlightForeground.Freeze();
    }

    public static IReadOnlyList<string> SplitTerms(string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return [];

        return searchText
            .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>يعيد بناء Inlines داخل TextBlock مع تمييز المقاطع المطابقة.</summary>
    public static void ApplyHighlight(TextBlock target, string? text, string? searchText)
    {
        target.Inlines.Clear();
        var value = text ?? string.Empty;
        if (value.Length == 0)
            return;

        var terms = SplitTerms(searchText);
        if (terms.Count == 0)
        {
            target.Inlines.Add(new Run(value));
            return;
        }

        var ranges = FindMatchRanges(value, terms);
        if (ranges.Count == 0)
        {
            target.Inlines.Add(new Run(value));
            return;
        }

        var index = 0;
        foreach (var (start, length) in ranges)
        {
            if (start > index)
                target.Inlines.Add(new Run(value[index..start]));

            var run = new Run(value.Substring(start, length))
            {
                Background = HighlightBrush,
                Foreground = HighlightForeground,
                FontWeight = FontWeights.SemiBold
            };
            target.Inlines.Add(run);
            index = start + length;
        }

        if (index < value.Length)
            target.Inlines.Add(new Run(value[index..]));
    }

    private static List<(int Start, int Length)> FindMatchRanges(string text, IReadOnlyList<string> terms)
    {
        var hits = new List<(int Start, int Length)>();
        foreach (var term in terms)
        {
            var start = 0;
            while (start < text.Length)
            {
                var idx = text.IndexOf(term, start, StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                    break;
                hits.Add((idx, term.Length));
                start = idx + Math.Max(1, term.Length);
            }
        }

        if (hits.Count == 0)
            return hits;

        hits.Sort((a, b) => a.Start != b.Start ? a.Start.CompareTo(b.Start) : b.Length.CompareTo(a.Length));

        var merged = new List<(int Start, int Length)>();
        foreach (var hit in hits)
        {
            if (merged.Count == 0)
            {
                merged.Add(hit);
                continue;
            }

            var last = merged[^1];
            var lastEnd = last.Start + last.Length;
            var hitEnd = hit.Start + hit.Length;
            if (hit.Start <= lastEnd)
            {
                var end = Math.Max(lastEnd, hitEnd);
                merged[^1] = (last.Start, end - last.Start);
            }
            else
            {
                merged.Add(hit);
            }
        }

        return merged;
    }
}
