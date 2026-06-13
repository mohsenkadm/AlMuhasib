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
        out int totalPages,
        out string paginationText)
    {
        PaginationHelper.ApplyPage(allItems, displayItems, currentPage, pageSize,
            out totalCount, out totalPages, out paginationText);
    }
}
