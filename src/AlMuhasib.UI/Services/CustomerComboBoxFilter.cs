using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;

namespace AlMuhasib.UI.Services;

public static class CustomerComboBoxFilter
{
    public static void Apply(
        ObservableCollection<Customer> allCustomers,
        ObservableCollection<Customer> filtered,
        string? searchText,
        int maxResults = 30)
    {
        filtered.Clear();
        var term = searchText?.Trim() ?? string.Empty;
        var source = string.IsNullOrEmpty(term)
            ? allCustomers
            : allCustomers.Where(c =>
                c.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (c.Phone?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (c.FileNumber?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));

        foreach (var c in source.Take(maxResults))
            filtered.Add(c);
    }
}
