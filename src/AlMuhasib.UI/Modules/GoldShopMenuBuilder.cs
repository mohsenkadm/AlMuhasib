using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using AlMuhasib.UI.ViewModels;
using AlMuhasib.UI.ViewModels.Gold;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Modules;

public static class GoldShopMenuBuilder
{
    public static List<NavigationMenuItem> Build()
    {
        var items = new List<NavigationMenuItem>
        {
            Item("لوحة التحكم", PackIconKind.ViewDashboard, typeof(GoldDashboardViewModel), GoldShopPermissionRegistry.Dashboard),
            Item("فاتورة بيع", PackIconKind.CashRegister, typeof(GoldSaleInvoiceViewModel), GoldShopPermissionRegistry.SaleInvoice),

            FlyoutGroup(
                key: "pricing",
                title: "التسعير",
                icon: PackIconKind.Gold,
                accent: "#B8860B",
                accentLight: "#FFF8E1",
                [
                    ("أسعار المثقال", PackIconKind.CurrencyUsd, typeof(GoldMithqalPricesViewModel), GoldShopPermissionRegistry.MithqalPrices),
                    ("أسعار الصرف", PackIconKind.CashMultiple, typeof(GoldFxRatesViewModel), GoldShopPermissionRegistry.FxRates),
                ]),
            FlyoutGroup(
                key: "inventory",
                title: "المخزون",
                icon: PackIconKind.PackageVariantClosed,
                accent: "#8B6914",
                accentLight: "#FFF8E1",
                [
                    ("أصناف الذهب", PackIconKind.DiamondStone, typeof(GoldItemsViewModel), GoldShopPermissionRegistry.Items),
                    ("تصنيفات الذهب", PackIconKind.TagMultiple, typeof(GoldCategoriesViewModel), GoldShopPermissionRegistry.Categories),
                    ("المخزون", PackIconKind.PackageVariantClosed, typeof(GoldStockViewModel), GoldShopPermissionRegistry.Stock),
                    ("تسوية مخزون", PackIconKind.TuneVerticalVariant, typeof(GoldStockAdjustmentViewModel), GoldShopPermissionRegistry.StockAdjustment),
                    ("رصيد افتتاحي", PackIconKind.PackageDown, typeof(GoldOpeningStockViewModel), GoldShopPermissionRegistry.OpeningStock),
                    ("مخازن", PackIconKind.Warehouse, typeof(GoldWarehousesViewModel), GoldShopPermissionRegistry.Warehouses),
                    ("نقل مخازن", PackIconKind.SwapHorizontal, typeof(GoldWarehouseTransferViewModel), GoldShopPermissionRegistry.WarehouseTransfer),
                ]),
            FlyoutGroup(
                key: "sales",
                title: "المبيعات",
                icon: PackIconKind.PointOfSale,
                accent: "#D4AF37",
                accentLight: "#FFFDE7",
                [
                    ("مرتجع بيع", PackIconKind.BackupRestore, typeof(GoldSaleReturnViewModel), GoldShopPermissionRegistry.SaleReturn),
                    ("مبيعات الآجل", PackIconKind.CreditCardClock, typeof(GoldCreditSalesViewModel), GoldShopPermissionRegistry.CreditSales),
                    ("التحصيل", PackIconKind.CashCheck, typeof(GoldCollectionViewModel), GoldShopPermissionRegistry.Collection),
                    ("تبديل ذهب", PackIconKind.SwapHorizontal, typeof(GoldExchangeInvoiceViewModel), GoldShopPermissionRegistry.ExchangeInvoice),
                ]),
            FlyoutGroup(
                key: "purchases",
                title: "المشتريات",
                icon: PackIconKind.CartArrowDown,
                accent: "#C9A227",
                accentLight: "#FFF8E1",
                [
                    ("فاتورة شراء", PackIconKind.CartArrowDown, typeof(GoldPurchaseInvoiceViewModel), GoldShopPermissionRegistry.PurchaseInvoice),
                    ("موردون", PackIconKind.TruckDelivery, typeof(GoldSuppliersViewModel), GoldShopPermissionRegistry.Suppliers),
                    ("كشف حساب مورد", PackIconKind.AccountDetails, typeof(GoldSupplierStatementViewModel), GoldShopPermissionRegistry.SupplierStatement),
                    ("تسديد الموردين", PackIconKind.CashCheck, typeof(GoldSupplierPaymentViewModel), GoldShopPermissionRegistry.SupplierPayment),
                    ("أرصدة الموردين الافتتاحية", PackIconKind.CashRefund, typeof(GoldOpeningSupplierBalanceViewModel), GoldShopPermissionRegistry.OpeningSupplierBalance),
                ]),
            FlyoutGroup(
                key: "customers",
                title: "الزبائن",
                icon: PackIconKind.AccountGroup,
                accent: "#A67C00",
                accentLight: "#FFF8E1",
                [
                    ("الزبائن", PackIconKind.AccountGroup, typeof(GoldCustomersViewModel), GoldShopPermissionRegistry.Customers),
                    ("كشف حساب زبون", PackIconKind.AccountDetails, typeof(GoldCustomerStatementViewModel), GoldShopPermissionRegistry.CustomerStatement),
                    ("أرصدة الزبائن الافتتاحية", PackIconKind.AccountCash, typeof(GoldOpeningCustomerBalanceViewModel), GoldShopPermissionRegistry.OpeningCustomerBalance),
                ]),
            FlyoutGroup(
                key: "finance",
                title: "المالية",
                icon: PackIconKind.Bank,
                accent: "#6D4C00",
                accentLight: "#EFEBE9",
                [
                    ("القاصات", PackIconKind.CashRegister, typeof(GoldCashBoxesViewModel), GoldShopPermissionRegistry.CashBoxes),
                    ("السندات", PackIconKind.FileDocumentOutline, typeof(GoldVouchersViewModel), GoldShopPermissionRegistry.Vouchers),
                    ("المصاريف", PackIconKind.CashMinus, typeof(GoldExpensesViewModel), GoldShopPermissionRegistry.Expenses),
                    ("أنواع المصاريف", PackIconKind.TagMultiple, typeof(GoldExpenseTypesViewModel), GoldShopPermissionRegistry.ExpenseTypes),
                ]),
            FlyoutGroup(
                key: "reports",
                title: "التقارير",
                icon: PackIconKind.ChartBar,
                accent: "#5D4037",
                accentLight: "#EFEBE9",
                [
                    ("تقرير المخزون", PackIconKind.PackageVariant, typeof(GoldStockReportViewModel), GoldShopPermissionRegistry.StockReport),
                    ("تقرير المبيعات", PackIconKind.ChartLine, typeof(GoldSalesReportViewModel), GoldShopPermissionRegistry.SalesReport),
                    ("تقرير الآجل", PackIconKind.ChartTimelineVariant, typeof(GoldCreditReportViewModel), GoldShopPermissionRegistry.CreditReport),
                    ("أعمار الذمم", PackIconKind.CalendarClock, typeof(GoldAgingReportViewModel), GoldShopPermissionRegistry.AgingReport),
                    ("حركة العيارات", PackIconKind.SwapVertical, typeof(GoldKaratMovementReportViewModel), GoldShopPermissionRegistry.KaratMovementReport),
                    ("الربحية", PackIconKind.ChartAreaspline, typeof(GoldProfitabilityReportViewModel), GoldShopPermissionRegistry.ProfitabilityReport),
                    ("سجل التدقيق", PackIconKind.ClipboardTextClock, typeof(GoldAuditReportViewModel), GoldShopPermissionRegistry.AuditReport),
                    ("الفواتير المحذوفة", PackIconKind.DeleteForever, typeof(GoldDeletedInvoicesReportViewModel), GoldShopPermissionRegistry.DeletedInvoicesReport),
                    ("أداء المستخدمين", PackIconKind.AccountStar, typeof(GoldUserPerformanceReportViewModel), GoldShopPermissionRegistry.UserPerformanceReport),
                    ("حركة القاصات", PackIconKind.CashMultiple, typeof(GoldCashBoxMovementReportViewModel), GoldShopPermissionRegistry.CashBoxMovementReport),
                    ("تقرير المشتريات", PackIconKind.CartOutline, typeof(GoldPurchasesReportViewModel), GoldShopPermissionRegistry.PurchasesReport),
                ]),
            FlyoutGroup(
                key: "system",
                title: "النظام والإعدادات",
                icon: PackIconKind.CogOutline,
                accent: "#455A64",
                accentLight: "#ECEFF1",
                [
                    ("التنبيهات", PackIconKind.BellOutline, typeof(GoldNotificationsViewModel), GoldShopPermissionRegistry.Notifications),
                    ("إعدادات الذهب", PackIconKind.Cog, typeof(GoldSettingsViewModel), GoldShopPermissionRegistry.Settings),
                    ("المستخدمون", PackIconKind.AccountMultiple, typeof(UsersViewModel), GoldShopPermissionRegistry.Users),
                    ("الصلاحيات", PackIconKind.ShieldKey, typeof(PermissionsViewModel), GoldShopPermissionRegistry.Permissions),
                    ("إعدادات الطباعة", PackIconKind.PrinterSettings, typeof(PrintLayoutSettingsViewModel), GoldShopPermissionRegistry.PrintSettings),
                    ("النسخ الاحتياطي", PackIconKind.DatabaseCog, typeof(BackupRestoreViewModel), GoldShopPermissionRegistry.Backup),
                    ("ربط الحاسبات", PackIconKind.LanConnect, typeof(NetworkConnectionSettingsViewModel), ScreenPermissionRegistry.NetworkConnection),
                    ("المزامنة السحابية", PackIconKind.CloudSync, typeof(CloudSyncSettingsViewModel), GoldShopPermissionRegistry.CloudSync),
                    ("تحديث النظام", PackIconKind.CloudDownload, typeof(SystemUpdateViewModel), ScreenPermissionRegistry.SystemUpdate),
                    ("تبديل النظام (مطور)", PackIconKind.DeveloperBoard, typeof(DeveloperSystemSwitchViewModel), ScreenPermissionRegistry.DeveloperSystem),
                ]),
        };

        if (items.Count > 0)
            items[0].IsSelected = true;

        return items;
    }

    private static NavigationMenuItem Item(string title, PackIconKind icon, Type viewModelType, string screenName) =>
        new()
        {
            Title = title,
            Icon = icon,
            ViewModelType = viewModelType,
            ScreenName = screenName
        };

    private static NavigationMenuItem FlyoutGroup(
        string key,
        string title,
        PackIconKind icon,
        string accent,
        string accentLight,
        (string Title, PackIconKind Icon, Type Vm, string Screen)[] children)
    {
        var group = new NavigationMenuItem
        {
            Title = title,
            Icon = icon,
            IsReportCategory = true,
            CategoryKey = key,
            ScreenName = $"MenuGroup:{key}",
            CategoryAccentColor = accent,
            CategoryAccentLightColor = accentLight,
            FlyoutItemLabel = "شاشة"
        };

        foreach (var child in children)
        {
            group.Children.Add(new NavigationMenuItem
            {
                Title = child.Title,
                Icon = child.Icon,
                ViewModelType = child.Vm,
                ScreenName = child.Screen,
                IsSubItem = true
            });
        }

        return group;
    }
}
