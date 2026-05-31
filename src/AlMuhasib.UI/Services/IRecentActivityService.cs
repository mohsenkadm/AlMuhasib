namespace AlMuhasib.UI.Services;

public record RecentActivityEntry(
    DateTime Timestamp,
    string Title,
    string Detail,
    string ScreenName,
    string? ViewModelTypeName);

public interface IRecentActivityService
{
    void Record(string title, string detail, string screenName, Type? viewModelType = null);
    void SeedIfEmpty(IEnumerable<RecentActivityEntry> entries);
    IReadOnlyList<RecentActivityEntry> GetRecent(int count = 20);
    int Count { get; }
}
