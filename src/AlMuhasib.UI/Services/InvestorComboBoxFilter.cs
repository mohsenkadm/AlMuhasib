using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;

namespace AlMuhasib.UI.Services;

public static class InvestorComboBoxFilter
{
    public static void Apply(
        ObservableCollection<Investor> allInvestors,
        ObservableCollection<Investor> filtered,
        string? searchText,
        int maxResults = 30)
    {
        filtered.Clear();
        var term = searchText?.Trim() ?? string.Empty;
        var source = string.IsNullOrEmpty(term)
            ? allInvestors
            : allInvestors.Where(i =>
                i.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (i.Phone?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));

        foreach (var i in source.Take(maxResults))
            filtered.Add(i);
    }
}
