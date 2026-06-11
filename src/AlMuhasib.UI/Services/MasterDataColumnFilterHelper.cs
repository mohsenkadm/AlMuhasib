using System.Collections.ObjectModel;

namespace AlMuhasib.UI.Services;

public static class MasterDataColumnFilterHelper
{
    public static bool HasActiveColumnFilters(IReadOnlyDictionary<string, string> filters) =>
        filters.Any(kv => !string.IsNullOrWhiteSpace(kv.Value));

    public static void ApplyClientPagination<T>(
        IList<T> allItems,
        ObservableCollection<T> displayItems,
        int currentPage,
        int pageSize,
        out int totalCount,
        out int totalPages)
    {
        totalCount = allItems.Count;
        totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));

        var page = currentPage;
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        displayItems.Clear();
        foreach (var item in allItems.Skip((page - 1) * pageSize).Take(pageSize))
            displayItems.Add(item);
    }
}
