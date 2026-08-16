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
            Item("مرتجع مبيعات", PackIconKind.KeyboardReturn, typeof(SalesInvoiceViewModel), ScreenPermissionRegistry.SalesReturn),
            Item("فاتورة تلف", PackIconKind.DeleteAlert, typeof(SalesInvoiceViewModel), ScreenPermissionRegistry.DamageInvoice),
            Item("بيع سريع (POS)", PackIconKind.PointOfSale, typeof(PosQuickSaleViewModel), "SaleInvoice"),
            Item("فحص السعر بالباركود", PackIconKind.BarcodeScan, typeof(BarcodePriceCheckViewModel), ScreenPermissionRegistry.BarcodePriceCheck),

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
                    ("أنواع التعبئة", PackIconKind.PackageVariant, typeof(PackagingTypesViewModel), "PackagingTypes"),
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
                    ("السواقين", PackIconKind.AccountHardHat, typeof(DriversViewModel), "Drivers"),
                    ("الموردون", PackIconKind.Factory, typeof(SuppliersViewModel), "Suppliers"),
                    ("أرصدة العملاء الافتتاحية", PackIconKind.AccountCash, typeof(OpeningCustomerBalanceViewModel), "OpeningCustomerBalances"),
                    ("أرصدة الموردين الافتتاحية", PackIconKind.CashRefund, typeof(OpeningSupplierBalanceViewModel), "OpeningSupplierBalances"),
                    ("ملف الشخص", PackIconKind.AccountDetails, typeof(PersonProfileViewModel), "PersonProfile"),
                ]),
            FlyoutGroup(
                key: "product-offers",
                title: "عروض المنتجات",
                icon: PackIconKind.Sale,
                accent: "#E65100",
                accentLight: "#FFF8E1",
                [
                    ("إدارة العروض", PackIconKind.TagMultiple, typeof(ProductOffersViewModel), "ProductOffers"),
                ]),
            FlyoutGroup(
                key: "loyalty",
                title: "نظام الولاء",
                icon: PackIconKind.GiftOutline,
                accent: "#C62828",
                accentLight: "#FFEBEE",
                [
                    ("إعدادات الولاء", PackIconKind.TuneVariant, typeof(LoyaltySettingsViewModel), "LoyaltySettings"),
                    ("حسابات ولاء الزبائن", PackIconKind.AccountStar, typeof(LoyaltyAccountsViewModel), "LoyaltyAccounts"),
                    ("سجل حركات النقاط", PackIconKind.SwapVertical, typeof(LoyaltyLedgerViewModel), "LoyaltyLedger"),
                    ("تقرير ملخص الولاء", PackIconKind.ChartPie, typeof(LoyaltySummaryReportViewModel), "LoyaltyReports"),
                    ("أكثر الزبائن ولاءً", PackIconKind.Trophy, typeof(LoyaltyTopCustomersReportViewModel), "LoyaltyReports"),
                ]),
            FlyoutGroup(
                key: "sales-reps",
                title: "المندوبين",
                icon: PackIconKind.AccountTie,
                accent: "#1565C0",
                accentLight: "#E3F2FD",
                [
                    ("ملفات المندوبين", PackIconKind.AccountTie, typeof(SalesRepresentativesViewModel), "SalesRepresentatives"),
                    ("قواعد العمولة", PackIconKind.Percent, typeof(SalesRepCommissionRulesViewModel), "SalesRepCommissionRules"),
                    ("أهداف المندوبين", PackIconKind.Target, typeof(SalesRepTargetsViewModel), "SalesRepTargets"),
                    ("تحصيلات المندوبين", PackIconKind.CashMultiple, typeof(SalesRepCollectionsViewModel), "SalesRepCollections"),
                    ("كشف حساب المندوب", PackIconKind.FileDocumentOutline, typeof(SalesRepStatementViewModel), "SalesRepStatement"),
                    ("أداء المندوبين", PackIconKind.ChartBar, typeof(SalesRepPerformanceReportViewModel), "SalesRepPerformance"),
                    ("عملاء المندوب", PackIconKind.AccountGroup, typeof(SalesRepCustomersReportViewModel), "SalesRepCustomers"),
                ]),
            FlyoutGroup(
                key: "purchases",
                title: "المشتريات",
                icon: PackIconKind.CartArrowDown,
                accent: "#EF6C00",
                accentLight: "#FFF3E0",
                [
                    ("فاتورة مشتريات", PackIconKind.CartArrowDown, typeof(PurchaseInvoiceViewModel), "PurchaseInvoice"),
                    ("مرتجع مشتريات", PackIconKind.KeyboardReturn, typeof(PurchaseInvoiceViewModel), ScreenPermissionRegistry.PurchaseReturn),
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
                    ("تسديد استقطاع المنصة", PackIconKind.FileExcel, typeof(PlatformDeductionSettlementViewModel), "Installments"),
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
                ("إعدادات الحقول", PackIconKind.FormSelect, typeof(CustomFieldSettingsViewModel), "CustomFieldSettings"),
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
