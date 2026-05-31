using System.Collections.Concurrent;

namespace AlMuhasib.UI.Services;

public sealed class RecentActivityService : IRecentActivityService
{
    private const int MaxEntries = 50;
    private readonly ConcurrentQueue<RecentActivityEntry> _entries = new();

    public void Record(string title, string detail, string screenName, Type? viewModelType = null)
    {
        _entries.Enqueue(new RecentActivityEntry(
            DateTime.Now,
            title,
            detail,
            screenName,
            viewModelType?.FullName));

        while (_entries.Count > MaxEntries && _entries.TryDequeue(out _)) { }
    }

    public int Count => _entries.Count;

    public IReadOnlyList<RecentActivityEntry> GetRecent(int count = 20) =>
        _entries.Reverse().Take(count).ToList();

    public void SeedIfEmpty(IEnumerable<RecentActivityEntry> entries)
    {
        if (_entries.Count > 0) return;
        foreach (var entry in entries)
            _entries.Enqueue(entry);
    }
}
