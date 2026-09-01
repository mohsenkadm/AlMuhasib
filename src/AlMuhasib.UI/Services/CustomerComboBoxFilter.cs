using System.Collections.ObjectModel;
using AlMuhasib.Core;
using AlMuhasib.Core.Entities;

namespace AlMuhasib.UI.Services;

public static class CustomerComboBoxFilter
{
    private const int DefaultSearchLimit = 50;
    private const int DefaultBrowseLimit = 500;

    public static void Apply(
        ObservableCollection<Customer> allCustomers,
        ObservableCollection<Customer> filtered,
        string? searchText,
        int maxSearchResults = DefaultSearchLimit,
        int maxBrowseResults = DefaultBrowseLimit)
    {
        filtered.Clear();
        var term = searchText?.Trim() ?? string.Empty;
        var source = string.IsNullOrEmpty(term)
            ? allCustomers
            : allCustomers.Where(c => CustomerDisplayHelper.MatchesSearch(c, term));

        var limit = string.IsNullOrEmpty(term) ? maxBrowseResults : maxSearchResults;
        foreach (var c in source.Take(limit))
            filtered.Add(c);
    }
}
