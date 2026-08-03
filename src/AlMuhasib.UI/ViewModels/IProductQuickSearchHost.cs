using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.ViewModels;

/// <summary>واجهة توفّر كتالوج البحث السريع للمنتجات داخل الفاتورة.</summary>
public interface IProductQuickSearchHost
{
    ProductQuickSearchCatalog QuickSearchCatalog { get; }
}
