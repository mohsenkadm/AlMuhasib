using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using AlMuhasib.UI.ViewModels;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Modules;

public static class AccountingMenuBuilder
{
    public static List<NavigationMenuItem> Build()
    {
        var items = new List<NavigationMenuItem>
        {
            // شاشات مباشرة (ليست داخل كروب)
            Item("لوحة التحكم", PackIconKind.ViewDashboard, typeof(DashboardViewModel), ScreenPermissionRegistry.Dashboard),
            Item("فاتورة مبيعات", PackIconKind.CashRegister, typeof(SalesInvoiceViewModel), "SaleInvoice"),
            Item("بيع سريع (POS)", PackIconKind.PointOfSale, typeof(PosQuickSaleViewModel), "SaleInvoice"),

            // كروبات تفتح نافذة جانبية مثل التقارير
            FlyoutGroup(
                key: "inventory",
                title: "المنتجات والمخزون",
                icon: PackIconKind.PackageVariantClosed,
                accent: "#2E7D32",
                accentLight: "#E8F5E9",
                [
                    ("المنتجات", PackIconKind.PackageVariantClosed, typeof(ProductsViewModel), "Products"),
                    ("تصنيفات المنتجات", PackIconKind.TagMultiple, typeof(CategoriesViewModel), "Categories"),
                    ("أنواع التسعير", PackIconKind.CashMultiple, typeof(PricingTypesViewModel), "PricingTypes"),
                    ("تسعير منتجات", PackIconKind.TagTextOutline, typeof(ProductPricingViewModel), "ProductPricing"),
                    ("المخازن", PackIconKind.Warehouse, typeof(WarehousesViewModel), "Warehouses"),
                    ("الأرصدة الافتتاحية", PackIconKind.PackageVariantClosedPlus, typeof(OpeningStockViewModel), "OpeningStock"),
                    ("تسوية مخزنية", PackIconKind.TuneVerticalVariant, typeof(StockAdjustmentViewModel), "StockAdjustment"),
                    ("نقل مخازن", PackIconKind.TruckDelivery, typeof(WarehouseTransferViewModel), "Warehouses"),
                ]),
            FlyoutGroup(
                key: "partners",
                title: "العملاء والموردين",
                icon: PackIconKind.AccountGroup,
                accent: "#0277BD",
                accentLight: "#E1F5FE",
                [
                    ("العملاء", PackIconKind.AccountGroup, typeof(CustomersViewModel), "Customers"),
                    ("الموردون", PackIconKind.Factory, typeof(SuppliersViewModel), "Suppliers"),
                ]),
            FlyoutGroup(
                key: "purchases",
                title: "المشتريات",
                icon: PackIconKind.CartArrowDown,
                accent: "#EF6C00",
                accentLight: "#FFF3E0",
                [
                    ("فاتورة مشتريات", PackIconKind.CartArrowDown, typeof(PurchaseInvoiceViewModel), "PurchaseInvoice"),
                    ("مرتجع مشتريات", PackIconKind.KeyboardReturn, typeof(PurchaseInvoiceViewModel), "PurchaseReturn"),
                ]),
            FlyoutGroup(
                key: "installments",
                title: "الأقساط والتحصيل",
                icon: PackIconKind.CalendarMultipleCheck,
                accent: "#6A1B9A",
                accentLight: "#F3E5F5",
                [
                    ("فاتورة أقساط", PackIconKind.CalendarClock, typeof(InstallmentInvoiceViewModel), "InstallmentInvoice"),
                    ("لوحة التحصيل", PackIconKind.CashMultiple, typeof(CollectionDashboardViewModel), "Installments"),
                    ("الأقساط", PackIconKind.CalendarMultipleCheck, typeof(InstallmentsViewModel), "Installments"),
                    ("أرصدة الأقساط الافتتاحية", PackIconKind.History, typeof(OpeningInstallmentBalanceViewModel), "OpeningInstallments"),
                ]),
            FlyoutGroup(
                key: "finance",
                title: "المالية والخزينة",
                icon: PackIconKind.Bank,
                accent: "#00838F",
                accentLight: "#E0F7FA",
                [
                    ("السندات", PackIconKind.FileDocumentOutline, typeof(VouchersViewModel), "Vouchers"),
                    ("المصاريف", PackIconKind.CashMinus, typeof(ExpenseViewModel), "Expenses"),
                    ("القاصات والمصرف", PackIconKind.Bank, typeof(CashBankViewModel), "CashAndBank"),
                    ("رأس المال", PackIconKind.Cash, typeof(CapitalAdjustmentViewModel), "Capital"),
                ]),
            FlyoutGroup(
                key: "investors",
                title: "المستثمرون",
                icon: PackIconKind.TrendingUp,
                accent: "#558B2F",
                accentLight: "#F1F8E9",
                [
                    ("المستثمرون", PackIconKind.TrendingUp, typeof(InvestorsViewModel), "Investors"),
                    ("أرصدة المستثمرين الافتتاحية", PackIconKind.AccountCashOutline, typeof(OpeningInvestorsViewModel), "OpeningInvestors"),
                ]),

            new NavigationMenuItem
            {
                Title = "التقارير",
                IsMenuSectionLabel = true,
                ScreenName = ScreenPermissionRegistry.Reports
            }
        };

        items.AddRange(ReportMenuCatalog.CreateCategoryMenuItems());

        items.Add(FlyoutGroup(
            key: "system",
            title: "النظام والإعدادات",
            icon: PackIconKind.CogOutline,
            accent: "#455A64",
            accentLight: "#ECEFF1",
            [
                ("سجل العمليات", PackIconKind.History, typeof(AuditLogViewModel), "AuditLog"),
                ("المستخدمون", PackIconKind.AccountMultiple, typeof(UsersViewModel), "Users"),
                ("الصلاحيات", PackIconKind.ShieldKey, typeof(PermissionsViewModel), "Permissions"),
                ("معالج النقل", PackIconKind.DatabaseImport, typeof(MigrationWizardViewModel), "DataImport"),
                ("إعدادات الميزات", PackIconKind.TuneVariant, typeof(BusinessFeaturesSettingsViewModel), "BusinessFeatures"),
                ("إعدادات الطباعة", PackIconKind.PrinterSettings, typeof(PrintLayoutSettingsViewModel), "PrintSettings"),
                ("النسخ الاحتياطي", PackIconKind.DatabaseCog, typeof(BackupRestoreViewModel), "Backup"),
                ("ربط الحاسبات", PackIconKind.LanConnect, typeof(NetworkConnectionSettingsViewModel), ScreenPermissionRegistry.NetworkConnection),
                ("المزامنة السحابية", PackIconKind.CloudSync, typeof(CloudSyncSettingsViewModel), "CloudSync"),
                ("تحديث النظام", PackIconKind.CloudDownload, typeof(SystemUpdateViewModel), ScreenPermissionRegistry.SystemUpdate),
                ("تبديل النظام (مطور)", PackIconKind.DeveloperBoard, typeof(DeveloperSystemSwitchViewModel), ScreenPermissionRegistry.DeveloperSystem),
            ]));

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
