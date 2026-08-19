using AlMuhasib.Core.Entities;

namespace AlMuhasib.UI.Helpers;

/// <summary>مطابقة بحث المنتجات بالاسم أو الباركود أو الاسم العلمي.</summary>
public static class ProductSearchHelper
{
    public static IEnumerable<Product> ActiveOnly(IEnumerable<Product> products) =>
        products.Where(p => !p.IsDeleted);

    public static bool Matches(Product product, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return true;

        var term = searchText.Trim();
        if (ContainsIgnoreCase(product.Name, term)
            || ContainsIgnoreCase(product.Barcode, term)
            || ContainsIgnoreCase(product.ScientificName, term))
            return true;

        // مطابقة أي كلمة من كلمات البحث داخل الاسم الطويل
        var words = HighlightTextHelper.SplitTerms(term);
        if (words.Count <= 1)
            return false;

        return words.Any(w =>
            ContainsIgnoreCase(product.Name, w)
            || ContainsIgnoreCase(product.Barcode, w)
            || ContainsIgnoreCase(product.ScientificName, w));
    }

    private static bool ContainsIgnoreCase(string? source, string term) =>
        !string.IsNullOrEmpty(source)
        && source.Contains(term, StringComparison.OrdinalIgnoreCase);
}
