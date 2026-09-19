using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Helpers;

namespace AlMuhasib.UI.Helpers;

/// <summary>مطابقة بحث المنتجات بالاسم أو الباركود أو الاسم العلمي أو حقول معرض السيارات.</summary>
public static class ProductSearchHelper
{
    public static IEnumerable<Product> ActiveOnly(IEnumerable<Product> products) =>
        products.Where(p => !p.IsDeleted);

    public static bool Matches(Product product, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return true;

        var term = searchText.Trim();
        if (MatchesSingleTerm(product, term))
            return true;

        // مطابقة أي كلمة من كلمات البحث داخل الحقول
        var words = HighlightTextHelper.SplitTerms(term);
        if (words.Count <= 1)
            return false;

        return words.Any(w => MatchesSingleTerm(product, w));
    }

    private static bool MatchesSingleTerm(Product product, string term) =>
        ContainsIgnoreCase(product.Name, term)
        || ContainsIgnoreCase(product.Barcode, term)
        || ContainsIgnoreCase(product.ScientificName, term)
        || ContainsIgnoreCase(product.VehicleType, term)
        || ContainsIgnoreCase(product.ChassisNumber, term)
        || ContainsIgnoreCase(product.VehicleColor, term)
        || ContainsIgnoreCase(product.PlateNumber, term)
        || ContainsIgnoreCase(product.PassengerCount?.ToString(), term)
        || MatchesPlateType(product, term);

    private static bool MatchesPlateType(Product product, string term)
    {
        var display = VehiclePlateTypeHelper.ToDisplay(product.PlateType);
        if (ContainsIgnoreCase(display, term))
            return true;

        var parsed = VehiclePlateTypeHelper.Parse(term);
        return parsed != Core.Enums.VehiclePlateType.None && product.PlateType == parsed;
    }

    private static bool ContainsIgnoreCase(string? source, string term) =>
        !string.IsNullOrEmpty(source)
        && source.Contains(term, StringComparison.OrdinalIgnoreCase);
}
