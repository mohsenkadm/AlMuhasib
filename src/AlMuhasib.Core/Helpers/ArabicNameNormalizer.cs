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
    /// اقتراحات سريعة: فهرسة بالبادئة ثم مسافة تحرير محدودة.
    /// </summary>
    public static IReadOnlyList<(int Id, string Name, double Score)> FindSuggestionsFast(
        string excelName,
        IReadOnlyList<(int Id, string Name, string Compact)> customersIndexed,
        double minScore = 0.82,
        int maxResults = 3)
    {
        var byPrefix = BuildPrefixIndex(customersIndexed);
        return FindSuggestionsWithPrefixIndex(excelName, byPrefix, minScore, maxResults);
    }

    public static Dictionary<string, List<(int Id, string Name, string Compact)>> BuildPrefixIndex(
        IReadOnlyList<(int Id, string Name, string Compact)> customersIndexed)
    {
        var map = new Dictionary<string, List<(int Id, string Name, string Compact)>>(StringComparer.Ordinal);
        foreach (var c in customersIndexed)
        {
            if (c.Compact.Length < 2) continue;
            var key = c.Compact[..2];
            if (!map.TryGetValue(key, out var list))
            {
                list = new List<(int Id, string Name, string Compact)>(8);
                map[key] = list;
            }
            list.Add(c);
        }
        return map;
    }

    public static IReadOnlyList<(int Id, string Name, double Score)> FindSuggestionsWithPrefixIndex(
        string excelName,
        IReadOnlyDictionary<string, List<(int Id, string Name, string Compact)>> byPrefix,
        double minScore = 0.82,
        int maxResults = 3)
    {
        var compact = Compact(excelName);
        if (compact.Length < 2 || byPrefix.Count == 0)
            return [];

        var prefix = compact[..2];
        if (!byPrefix.TryGetValue(prefix, out var candidates) || candidates.Count == 0)
            return [];

        var minLen = Math.Max(2, (int)(compact.Length * 0.7));
        var maxLen = compact.Length + Math.Max(2, compact.Length / 4);
        var best = new List<(int Id, string Name, double Score)>(maxResults);

        foreach (var c in candidates)
        {
            if (c.Compact == compact)
                continue;
            if (c.Compact.Length < minLen || c.Compact.Length > maxLen)
                continue;

            // حد أعلى سريع قبل Levenshtein الكامل
            var maxDist = (int)Math.Floor((1.0 - minScore) * Math.Max(compact.Length, c.Compact.Length));
            var distance = LevenshteinDistanceBounded(compact, c.Compact, maxDist);
            if (distance < 0)
                continue;

            var score = 1.0 - (double)distance / Math.Max(compact.Length, c.Compact.Length);
            if (score < minScore)
                continue;

            InsertTopScore(best, (c.Id, c.Name, score), maxResults);
        }

        return best;
    }

    private static void InsertTopScore(
        List<(int Id, string Name, double Score)> best,
        (int Id, string Name, double Score) item,
        int maxResults)
    {
        var inserted = false;
        for (var i = 0; i < best.Count; i++)
        {
            if (item.Score > best[i].Score)
            {
                best.Insert(i, item);
                inserted = true;
                break;
            }
        }
        if (!inserted && best.Count < maxResults)
            best.Add(item);
        while (best.Count > maxResults)
            best.RemoveAt(best.Count - 1);
    }

    private static int LevenshteinDistance(string a, string b)
    {
        var bound = Math.Max(a.Length, b.Length);
        return LevenshteinDistanceBounded(a, b, bound);
    }

    /// <summary>يعيد -1 إذا تجاوزت المسافة الحد (إيقاف مبكر).</summary>
    private static int LevenshteinDistanceBounded(string a, string b, int maxDistance)
    {
        var n = a.Length;
        var m = b.Length;
        if (Math.Abs(n - m) > maxDistance)
            return -1;
        if (n == 0) return m <= maxDistance ? m : -1;
        if (m == 0) return n <= maxDistance ? n : -1;

        var prev = new int[m + 1];
        var curr = new int[m + 1];
        for (var j = 0; j <= m; j++) prev[j] = j;

        for (var i = 1; i <= n; i++)
        {
            curr[0] = i;
            var rowMin = curr[0];
            for (var j = 1; j <= m; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(
                    Math.Min(prev[j] + 1, curr[j - 1] + 1),
                    prev[j - 1] + cost);
                if (curr[j] < rowMin) rowMin = curr[j];
            }
            if (rowMin > maxDistance)
                return -1;
            (prev, curr) = (curr, prev);
        }

        return prev[m] <= maxDistance ? prev[m] : -1;
    }
}
