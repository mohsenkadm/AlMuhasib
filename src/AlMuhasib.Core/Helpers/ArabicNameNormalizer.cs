using System.Text;

namespace AlMuhasib.Core.Helpers;

/// <summary>
/// تطبيع ومقارنة أسماء عربية للمطابقة عند الاستيراد والتسديد.
/// </summary>
public static class ArabicNameNormalizer
{
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var sb = new StringBuilder(text.Length);
        var prevSpace = false;
        foreach (var raw in text.Trim())
        {
            var ch = char.ToLowerInvariant(raw);
            if (char.IsWhiteSpace(ch) || ch is '،' or ',' or '.' or '؟' or '!' or '-' or '_' or '\u0640')
            {
                if (!prevSpace && sb.Length > 0)
                {
                    sb.Append(' ');
                    prevSpace = true;
                }
                continue;
            }

            prevSpace = false;
            sb.Append(ch switch
            {
                'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',
                'ى' => 'ي',
                'ة' => 'ه',
                'ؤ' => 'و',
                'ئ' => 'ي',
                _ => ch
            });
        }

        return sb.ToString().Trim();
    }

    /// <summary>مفتاح مطابقة بدون مسافات (أصرم للمقارنة الدقيقة بعد التطبيع).</summary>
    public static string Compact(string? text) =>
        Normalize(text).Replace(" ", "", StringComparison.Ordinal);

    public static double Similarity(string? a, string? b)
    {
        var na = Compact(a);
        var nb = Compact(b);
        if (na.Length == 0 || nb.Length == 0)
            return 0;
        if (na == nb)
            return 1;

        var distance = LevenshteinDistance(na, nb);
        var maxLen = Math.Max(na.Length, nb.Length);
        return 1.0 - (double)distance / maxLen;
    }

    public static IReadOnlyList<(int Id, string Name, double Score)> FindSuggestions(
        string excelName,
        IEnumerable<(int Id, string Name)> customers,
        double minScore = 0.78,
        int maxResults = 5)
    {
        var indexed = customers
            .Select(c => (c.Id, c.Name, Compact: Compact(c.Name)))
            .Where(c => c.Compact.Length > 0)
            .ToList();
        return FindSuggestionsFast(excelName, indexed, minScore, maxResults);
    }

    /// <summary>
    /// اقتراحات سريعة: يضيّق المرشحين بالبادئة وطول الاسم قبل حساب مسافة التحرير.
    /// </summary>
    public static IReadOnlyList<(int Id, string Name, double Score)> FindSuggestionsFast(
        string excelName,
        IReadOnlyList<(int Id, string Name, string Compact)> customersIndexed,
        double minScore = 0.78,
        int maxResults = 5)
    {
        var compact = Compact(excelName);
        if (compact.Length == 0 || customersIndexed.Count == 0)
            return [];

        var prefixLen = Math.Min(2, compact.Length);
        var prefix = compact[..prefixLen];
        var minLen = Math.Max(1, (int)(compact.Length * 0.65));
        var maxLen = compact.Length + Math.Max(3, compact.Length / 3);

        var best = new List<(int Id, string Name, double Score)>(maxResults * 2);
        foreach (var c in customersIndexed)
        {
            if (c.Compact == compact)
                continue;
            if (c.Compact.Length < minLen || c.Compact.Length > maxLen)
                continue;

            var samePrefix = c.Compact.StartsWith(prefix, StringComparison.Ordinal);
            var sameFirst = c.Compact.Length > 0 && c.Compact[0] == compact[0]
                            && Math.Abs(c.Compact.Length - compact.Length) <= 4;
            if (!samePrefix && !sameFirst)
                continue;

            var score = 1.0 - (double)LevenshteinDistance(compact, c.Compact) / Math.Max(compact.Length, c.Compact.Length);
            if (score < minScore)
                continue;
            best.Add((c.Id, c.Name, score));
        }

        return best
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Name)
            .Take(maxResults)
            .ToList();
    }

    private static int LevenshteinDistance(string a, string b)
    {
        var n = a.Length;
        var m = b.Length;
        var d = new int[n + 1, m + 1];

        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}
