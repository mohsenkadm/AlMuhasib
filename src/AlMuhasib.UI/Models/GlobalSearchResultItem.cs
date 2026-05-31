using AlMuhasib.Core.Models.Ux;
using AlMuhasib.UI.ViewModels;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Models;

public class GlobalSearchResultItem
{
    public required string Title { get; init; }
    public string Subtitle { get; init; } = string.Empty;
    public PackIconKind Icon { get; init; }
    public string Category { get; init; } = string.Empty;

    public NavigationMenuItem? MenuItem { get; init; }
    public GlobalSearchHit? EntityHit { get; init; }

    public static GlobalSearchResultItem FromMenu(NavigationMenuItem menu) => new()
    {
        Title = menu.Title,
        Subtitle = "قائمة النظام",
        Icon = menu.Icon,
        Category = "القوائم",
        MenuItem = menu
    };

    public static GlobalSearchResultItem FromHit(GlobalSearchHit hit) => new()
    {
        Title = hit.Title,
        Subtitle = hit.Subtitle,
        Icon = hit.Kind switch
        {
            GlobalSearchKind.Customer => PackIconKind.Account,
            GlobalSearchKind.Supplier => PackIconKind.Factory,
            GlobalSearchKind.Product => PackIconKind.PackageVariant,
            GlobalSearchKind.SalesInvoice => PackIconKind.CashRegister,
            GlobalSearchKind.PurchaseInvoice => PackIconKind.CartArrowDown,
            GlobalSearchKind.Voucher => PackIconKind.FileDocument,
            _ => PackIconKind.Magnify
        },
        Category = hit.Kind switch
        {
            GlobalSearchKind.Customer => "عملاء",
            GlobalSearchKind.Supplier => "موردون",
            GlobalSearchKind.Product => "منتجات",
            GlobalSearchKind.SalesInvoice => "مبيعات",
            GlobalSearchKind.PurchaseInvoice => "مشتريات",
            GlobalSearchKind.Voucher => "سندات",
            _ => "أخرى"
        },
        EntityHit = hit
    };
}
