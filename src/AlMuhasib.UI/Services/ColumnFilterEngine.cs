using System.Globalization;
using System.Reflection;

namespace AlMuhasib.UI.Services;

public static class ColumnFilterEngine
{
    public static List<T> Apply<T>(IEnumerable<T> source, IReadOnlyDictionary<string, string> filters)
    {
        if (filters.Count == 0)
            return source.ToList();

        var active = filters
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value.Trim(), StringComparer.OrdinalIgnoreCase);

        if (active.Count == 0)
            return source.ToList();

        return source.Where(item => MatchesAll(item, active)).ToList();
    }

    public static int CountActive(IReadOnlyDictionary<string, string> filters) =>
        filters.Count(kv => !string.IsNullOrWhiteSpace(kv.Value));

    private static bool MatchesAll<T>(T item, Dictionary<string, string> filters)
    {
        foreach (var (path, term) in filters)
        {
            if (!MatchesProperty(item, path, term))
                return false;
        }

        return true;
    }

    private static bool MatchesProperty<T>(T item, string propertyPath, string term)
    {
        var value = GetPropertyValue(item, propertyPath);
        var text = FormatValue(value);
        return text.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatValue(object? value) => value switch
    {
        null => string.Empty,
        DateTime dt => dt.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture),
        DateTimeOffset dto => dto.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture),
        decimal d => d.ToString("N0", CultureInfo.InvariantCulture),
        double dbl => dbl.ToString("N2", CultureInfo.InvariantCulture),
        float f => f.ToString("N2", CultureInfo.InvariantCulture),
        bool b => b ? "نعم" : "لا",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
    };

    private static object? GetPropertyValue(object? obj, string propertyPath)
    {
        if (obj is null || string.IsNullOrWhiteSpace(propertyPath))
            return null;

        object? current = obj;
        foreach (var segment in propertyPath.Split('.'))
        {
            if (current is null)
                return null;

            var type = current.GetType();
            var prop = type.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop is null)
                return null;

            current = prop.GetValue(current);
        }

        return current;
    }
}
