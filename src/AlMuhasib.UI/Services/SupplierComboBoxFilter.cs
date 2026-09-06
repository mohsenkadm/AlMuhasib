using System.Collections.ObjectModel;
using AlMuhasib.Core;
using AlMuhasib.Core.Entities;

namespace AlMuhasib.UI.Services;

public static class SupplierComboBoxFilter
{
    public static void Apply(
        ObservableCollection<Supplier> allSuppliers,
        ObservableCollection<Supplier> filtered,
        string? searchText,
        int maxResults = 30)
    {
        filtered.Clear();
        var term = searchText?.Trim() ?? string.Empty;
        var source = string.IsNullOrEmpty(term)
            ? allSuppliers
            : allSuppliers.Where(s => SupplierDisplayHelper.MatchesSearch(s, term));

        foreach (var s in source.Take(maxResults))
            filtered.Add(s);
    }
}
