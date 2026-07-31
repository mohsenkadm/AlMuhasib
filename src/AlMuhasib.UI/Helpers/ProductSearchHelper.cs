using AlMuhasib.Core.Entities;

namespace AlMuhasib.UI.Helpers;

/// <summary>مطابقة بحث المنتجات بالاسم أو الباركود أو الاسم العلمي.</summary>
public static class ProductSearchHelper
{
    public static bool Matches(Product product, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return true;

        var term = searchText.Trim();
        return product.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
               || (product.Barcode?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
               || (product.ScientificName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false);
    }
}
