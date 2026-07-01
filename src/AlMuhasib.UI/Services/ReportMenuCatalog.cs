using AlMuhasib.UI.Models;
using AlMuhasib.UI.ViewModels;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Services;

/// <summary>تصنيفات التقارير وبنودها — مصدر واحد للقائمة واللوحة الجانبية.</summary>
public static class ReportMenuCatalog
{
    private static readonly (string Key, string Title, PackIconKind Icon, string Accent, string AccentLight, string Permission,
        (string Title, PackIconKind Icon, Type Vm, string Screen)[] Reports)[] Catalog =
    [
        ("sales-profit", "المبيعات والأرباح", PackIconKind.ChartLine, "#1565C0", "#E3F2FD", ScreenPermissionRegistry.Reports,
        [
            ("تقرير المبيعات", PackIconKind.CashRegister, typeof(SalesReportViewModel), ScreenPermissionRegistry.Reports),
            ("تقرير المشتريات", PackIconKind.CartArrowDown, typeof(PurchasesReportViewModel), ScreenPermissionRegistry.Reports),
            ("تقرير الأرباح", PackIconKind.TrendingUp, typeof(ProfitReportViewModel), ScreenPermissionRegistry.Reports),
            ("أفضل المنتجات", PackIconKind.StarCircle, typeof(TopProductsReportViewModel), ScreenPermissionRegistry.Reports),
            ("مقارنة الأرباح", PackIconKind.Compare, typeof(ProfitComparisonReportViewModel), ScreenPermissionRegistry.Reports),
            ("هامش ربح المنتجات", PackIconKind.ChartPie, typeof(ProductProfitMarginReportViewModel), ScreenPermissionRegistry.Reports),
        ]),
        ("installments", "الأقساط", PackIconKind.CalendarClock, "#6A1B9A", "#F3E5F5", ScreenPermissionRegistry.Reports,
        [
            ("أعمار ذمم الأقساط", PackIconKind.TimelineClock, typeof(InstallmentAgingReportViewModel), ScreenPermissionRegistry.Reports),
            ("ملخص الأقساط", PackIconKind.CalendarMultipleCheck, typeof(InstallmentsReportViewModel), ScreenPermissionRegistry.Reports),
            ("تفاصيل الأقساط", PackIconKind.CalendarClock, typeof(InstallmentDetailReportViewModel), ScreenPermissionRegistry.Reports),
            ("الأقساط المسددة", PackIconKind.CheckCircle, typeof(PaidInstallmentsReportViewModel), ScreenPermissionRegistry.Reports),
            ("الأقساط غير المسددة", PackIconKind.AlertCircle, typeof(UnpaidInstallmentsReportViewModel), ScreenPermissionRegistry.Reports),
            ("الأقساط المتأخرة", PackIconKind.ClockAlert, typeof(OverdueReportViewModel), ScreenPermissionRegistry.Reports),
        ]),
        ("partners", "العملاء والموردين", PackIconKind.AccountGroup, "#0277BD", "#E1F5FE", ScreenPermissionRegistry.Reports,
        [
            ("ملخص العملاء", PackIconKind.AccountMultiple, typeof(CustomersOverviewReportViewModel), ScreenPermissionRegistry.Reports),
            ("كشف حساب عميل", PackIconKind.AccountCash, typeof(CustomerStatementViewModel), ScreenPermissionRegistry.Reports),
            ("ملخص الموردين", PackIconKind.TruckDelivery, typeof(SuppliersOverviewReportViewModel), ScreenPermissionRegistry.Reports),
            ("كشف حساب مورد", PackIconKind.Factory, typeof(SupplierStatementViewModel), ScreenPermissionRegistry.Reports),
        ]),
        ("inventory-finance", "المخزون والمالية", PackIconKind.Bank, "#37474F", "#ECEFF1", ScreenPermissionRegistry.Reports,
        [
            ("حركة المنتجات", PackIconKind.SwapVertical, typeof(ProductMovementReportViewModel), ScreenPermissionRegistry.Reports),
            ("تقرير المخازن", PackIconKind.Warehouse, typeof(WarehouseReportViewModel), ScreenPermissionRegistry.Reports),
            ("صحة المخزون", PackIconKind.PackageVariant, typeof(StockHealthReportViewModel), ScreenPermissionRegistry.Reports),
            ("احتياج المخزون", PackIconKind.PackageVariantClosed, typeof(InventoryReplenishmentReportViewModel), ScreenPermissionRegistry.Reports),
            ("تقرير المصاريف", PackIconKind.CashMinus, typeof(ExpensesReportViewModel), ScreenPermissionRegistry.Reports),
            ("الواردات والمصروفات", PackIconKind.SwapHorizontal, typeof(IncomeExpenseReportViewModel), ScreenPermissionRegistry.Reports),
            ("التدفق النقدي", PackIconKind.ChartTimelineVariantShimmer, typeof(CashFlowReportViewModel), ScreenPermissionRegistry.Reports),
            ("تقرير المستثمرين", PackIconKind.AccountGroup, typeof(InvestorsReportViewModel), ScreenPermissionRegistry.Reports),
            ("موازنة يومية", PackIconKind.ScaleBalance, typeof(BalanceSheetViewModel), ScreenPermissionRegistry.BalanceSheet),
        ]),
    ];

    public static IEnumerable<NavigationMenuItem> CreateCategoryMenuItems()
    {
        foreach (var cat in Catalog)
        {
            var item = new NavigationMenuItem
            {
                Title = cat.Title,
                Icon = cat.Icon,
                IsReportCategory = true,
                CategoryKey = cat.Key,
                ScreenName = cat.Permission,
                CategoryAccentColor = cat.Accent,
                CategoryAccentLightColor = cat.AccentLight
            };

            foreach (var r in cat.Reports)
            {
                item.Children.Add(new NavigationMenuItem
                {
                    Title = r.Title,
                    Icon = r.Icon,
                    ViewModelType = r.Vm,
                    ScreenName = r.Screen,
                    IsSubItem = true
                });
            }

            yield return item;
        }
    }

    public static IEnumerable<ReportMenuEntry> GetVisibleReports(NavigationMenuItem category)
    {
        if (!category.IsReportCategory)
            yield break;

        var meta = Catalog.FirstOrDefault(c => c.Key == category.CategoryKey);
        var accent = meta.Accent;
        var accentLight = meta.AccentLight;

        foreach (var child in category.Children.Where(c => c.IsVisible && c.ViewModelType is not null))
        {
            yield return new ReportMenuEntry
            {
                Title = child.Title,
                Icon = child.Icon,
                ViewModelType = child.ViewModelType!,
                ScreenName = child.ScreenName,
                AccentColor = accent,
                AccentLightColor = accentLight
            };
        }
    }
}
