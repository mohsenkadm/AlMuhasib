using System.Collections.ObjectModel;

namespace AlMuhasib.UI.Services;

public static class PaginationHelper
{
    public static int ComputeTotalPages(int totalCount, int pageSize) =>
        Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));

    public static string BuildPaginationText(int totalCount, int currentPage, int pageSize)
    {
        if (totalCount <= 0)
            return "لا توجد سجلات";

        var totalPages = ComputeTotalPages(totalCount, pageSize);
        var page = Math.Clamp(currentPage, 1, totalPages);
        var start = (page - 1) * pageSize + 1;
        var end = Math.Min(page * pageSize, totalCount);
        return $"عرض {start}-{end} من {totalCount:N0}";
    }

    public static void ComputeStats(
        int totalCount,
        int currentPage,
        int pageSize,
        out int totalPages,
        out string paginationText)
    {
        totalPages = ComputeTotalPages(totalCount, pageSize);
        paginationText = BuildPaginationText(totalCount, currentPage, pageSize);
    }

    public static void ApplyPage<T>(
        IList<T> allItems,
        ObservableCollection<T> displayItems,
        int currentPage,
        int pageSize,
        out int totalCount,
        out int totalPages,
        out string paginationText)
    {
        totalCount = allItems.Count;
        totalPages = ComputeTotalPages(totalCount, pageSize);
        var page = Math.Clamp(currentPage, 1, totalPages);

        displayItems.Clear();
        foreach (var item in allItems.Skip((page - 1) * pageSize).Take(pageSize))
            displayItems.Add(item);

        paginationText = BuildPaginationText(totalCount, page, pageSize);
    }
}
