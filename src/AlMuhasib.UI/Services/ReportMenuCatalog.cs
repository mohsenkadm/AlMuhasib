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
            ("صافي أرباح المواد", PackIconKind.CashPlus, typeof(MaterialNetProfitReportViewModel), ScreenPermissionRegistry.Reports),
            ("المواد الأقل ربحاً", PackIconKind.TrendingDown, typeof(LeastProfitMaterialsReportViewModel), ScreenPermissionRegistry.Reports),
            ("مبيعات حسب طريقة الدفع", PackIconKind.CashRegister, typeof(SalesByPaymentMethodReportViewModel), ScreenPermissionRegistry.Reports),
            ("يومية المبيعات", PackIconKind.CalendarClock, typeof(DailySalesReportViewModel), ScreenPermissionRegistry.Reports),
            ("ملخص العمل", PackIconKind.ChartBoxOutline, typeof(WorkSummaryReportViewModel), ScreenPermissionRegistry.Reports),
            ("مبيعات حسب المخزن / المستخدم", PackIconKind.Warehouse, typeof(SalesByWarehouseUserReportViewModel), ScreenPermissionRegistry.Reports),
            ("هامش الربح الإجمالي", PackIconKind.ChartPie, typeof(GrossProfitMarginReportViewModel), ScreenPermissionRegistry.Reports),
            ("صافي الربح التشغيلي", PackIconKind.ChartLine, typeof(OperatingProfitReportViewModel), ScreenPermissionRegistry.Reports),
        ]),
        ("installments", "الأقساط", PackIconKind.CalendarClock, "#6A1B9A", "#F3E5F5", ScreenPermissionRegistry.Reports,
        [
            ("أعمار ذمم الأقساط", PackIconKind.TimelineClock, typeof(InstallmentAgingReportViewModel), ScreenPermissionRegistry.Reports),
            ("ملخص الأقساط", PackIconKind.CalendarMultipleCheck, typeof(InstallmentsReportViewModel), ScreenPermissionRegistry.Reports),
            ("تفاصيل الأقساط", PackIconKind.CalendarClock, typeof(InstallmentDetailReportViewModel), ScreenPermissionRegistry.Reports),
            ("الأقساط المسددة", PackIconKind.CheckCircle, typeof(PaidInstallmentsReportViewModel), ScreenPermissionRegistry.Reports),
            ("الأقساط غير المسددة", PackIconKind.AlertCircle, typeof(UnpaidInstallmentsReportViewModel), ScreenPermissionRegistry.Reports),
            ("الأقساط المتأخرة", PackIconKind.ClockAlert, typeof(OverdueReportViewModel), ScreenPermissionRegistry.Reports),
            ("ملخص أرصدة افتتاحية الأقساط", PackIconKind.FileDocumentOutline, typeof(OpeningInstallmentBalancesReportViewModel), ScreenPermissionRegistry.Reports),
            ("عمولة المنصة / رسوم الشركة", PackIconKind.ChartPie, typeof(CompanyFeeReportViewModel), ScreenPermissionRegistry.Reports),
            ("جدول الاستحقاق", PackIconKind.CalendarClock, typeof(InstallmentScheduleReportViewModel), ScreenPermissionRegistry.Reports),
        ]),
        ("partners", "العملاء والموردين", PackIconKind.AccountGroup, "#0277BD", "#E1F5FE", ScreenPermissionRegistry.Reports,
        [
            ("ملخص العملاء", PackIconKind.AccountMultiple, typeof(CustomersOverviewReportViewModel), ScreenPermissionRegistry.Reports),
            ("صافي أرباح العملاء", PackIconKind.AccountCash, typeof(CustomerNetProfitReportViewModel), ScreenPermissionRegistry.Reports),
            ("العملاء الأقل ربحاً", PackIconKind.AccountArrowDown, typeof(LeastProfitCustomersReportViewModel), ScreenPermissionRegistry.Reports),
            ("كشف حساب عميل", PackIconKind.AccountCash, typeof(CustomerStatementViewModel), ScreenPermissionRegistry.Reports),
            ("ملخص الموردين", PackIconKind.TruckDelivery, typeof(SuppliersOverviewReportViewModel), ScreenPermissionRegistry.Reports),
            ("كشف حساب مورد", PackIconKind.Factory, typeof(SupplierStatementViewModel), ScreenPermissionRegistry.Reports),
            ("أعمار الذمم المدينة", PackIconKind.AccountCash, typeof(ReceivablesAgingReportViewModel), ScreenPermissionRegistry.Reports),
            ("أعمار الذمم الدائنة", PackIconKind.TruckDelivery, typeof(PayablesAgingReportViewModel), ScreenPermissionRegistry.Reports),
            ("كشف تحصيلات العملاء", PackIconKind.CashPlus, typeof(CustomerCollectionsReportViewModel), ScreenPermissionRegistry.Reports),
            ("العملاء المتأخرون", PackIconKind.AccountAlert, typeof(OverdueCustomersReportViewModel), ScreenPermissionRegistry.Reports),
            ("كشف مدفوعات الموردين", PackIconKind.CashMinus, typeof(SupplierPaymentsReportViewModel), ScreenPermissionRegistry.Reports),
        ]),
        ("inventory-finance", "المخزون والمالية", PackIconKind.Bank, "#37474F", "#ECEFF1", ScreenPermissionRegistry.Reports,
        [
            ("حركة المنتجات", PackIconKind.SwapVertical, typeof(ProductMovementReportViewModel), ScreenPermissionRegistry.Reports),
            ("تقرير المخازن", PackIconKind.Warehouse, typeof(WarehouseReportViewModel), ScreenPermissionRegistry.Reports),
            ("صحة المخزون", PackIconKind.PackageVariant, typeof(StockHealthReportViewModel), ScreenPermissionRegistry.Reports),
            ("كميات الحد الأدنى", PackIconKind.AlertDecagramOutline, typeof(MinimumQuantityReportViewModel), ScreenPermissionRegistry.MinimumQuantityReport),
            ("تقرير الصلاحية", PackIconKind.CalendarClock, typeof(ExpiryReportViewModel), ScreenPermissionRegistry.Reports),
            ("احتياج المخزون", PackIconKind.PackageVariantClosed, typeof(InventoryReplenishmentReportViewModel), ScreenPermissionRegistry.Reports),
            ("تقرير المصاريف", PackIconKind.CashMinus, typeof(ExpensesReportViewModel), ScreenPermissionRegistry.Reports),
            ("الواردات والمصروفات", PackIconKind.SwapHorizontal, typeof(IncomeExpenseReportViewModel), ScreenPermissionRegistry.Reports),
            ("التدفق النقدي", PackIconKind.ChartTimelineVariantShimmer, typeof(CashFlowReportViewModel), ScreenPermissionRegistry.Reports),
            ("تقرير المستثمرين", PackIconKind.AccountGroup, typeof(InvestorsReportViewModel), ScreenPermissionRegistry.Reports),
            ("موازنة يومية", PackIconKind.ScaleBalance, typeof(BalanceSheetViewModel), ScreenPermissionRegistry.BalanceSheet),
            ("كشف حساب مصرف", PackIconKind.Bank, typeof(BankAccountStatementReportViewModel), ScreenPermissionRegistry.Reports),
            ("حركة صندوق / قاصة", PackIconKind.CashRegister, typeof(CashBoxMovementReportViewModel), ScreenPermissionRegistry.Reports),
            ("ملخص أرصدة نقدية", PackIconKind.CashMultiple, typeof(CashBalancesSummaryReportViewModel), ScreenPermissionRegistry.Reports),
            ("تقرير التحويلات", PackIconKind.SwapHorizontal, typeof(TransfersReportViewModel), ScreenPermissionRegistry.Reports),
            ("تقييم المخزون بالتكلفة", PackIconKind.CashPlus, typeof(InventoryValuationReportViewModel), ScreenPermissionRegistry.Reports),
            ("ربح المنتجات في المخزن", PackIconKind.ChartLineVariant, typeof(WarehouseProductProfitReportViewModel), ScreenPermissionRegistry.Reports),
            ("جرد المخزون", PackIconKind.PackageVariant, typeof(StockTakingReportViewModel), ScreenPermissionRegistry.Reports),
            ("تكلفة البضاعة المباعة", PackIconKind.CartArrowDown, typeof(CogsReportViewModel), ScreenPermissionRegistry.Reports),
            ("تقرير فواتير التلف", PackIconKind.DeleteAlert, typeof(DamageInvoicesReportViewModel), ScreenPermissionRegistry.DamageInvoicesReport),
            ("كميات حسب التعبئة", PackIconKind.PackageVariant, typeof(PackagingStockReportViewModel), ScreenPermissionRegistry.PackagingStockReport),
        ]),
        ("financial", "التقارير المالية", PackIconKind.Bank, "#00695C", "#E0F2F1", ScreenPermissionRegistry.Reports,
        [
            ("ملخص المركز المالي", PackIconKind.ChartPie, typeof(FinancialPositionSummaryReportViewModel), ScreenPermissionRegistry.Reports),
            ("أرباح وخسائر", PackIconKind.ChartLine, typeof(ProfitAndLossReportViewModel), ScreenPermissionRegistry.Reports),
            ("الميزانية العمومية", PackIconKind.ScaleBalance, typeof(StatementOfFinancialPositionReportViewModel), ScreenPermissionRegistry.Reports),
        ]),
        ("supervisory", "التقارير الرقابية", PackIconKind.ShieldSearch, "#C62828", "#FFEBEE", ScreenPermissionRegistry.SupervisoryReports,
        [
            ("فواتير محذوفة", PackIconKind.FileDocumentOutline, typeof(DeletedInvoicesReportViewModel), ScreenPermissionRegistry.SupervisoryReports),
            ("سندات محذوفة", PackIconKind.ReceiptTextOutline, typeof(DeletedVouchersReportViewModel), ScreenPermissionRegistry.SupervisoryReports),
            ("منتجات محذوفة", PackIconKind.PackageVariant, typeof(DeletedProductsReportViewModel), ScreenPermissionRegistry.SupervisoryReports),
            ("عملاء محذوفون", PackIconKind.AccountRemove, typeof(DeletedCustomersReportViewModel), ScreenPermissionRegistry.SupervisoryReports),
            ("موردون محذوفون", PackIconKind.TruckDelivery, typeof(DeletedSuppliersReportViewModel), ScreenPermissionRegistry.SupervisoryReports),
            ("مصاريف محذوفة", PackIconKind.CashMinus, typeof(DeletedExpensesReportViewModel), ScreenPermissionRegistry.SupervisoryReports),
            ("تعديلات الفواتير", PackIconKind.FileDocumentEdit, typeof(InvoiceModificationsReportViewModel), ScreenPermissionRegistry.SupervisoryReports),
            ("تعديلات المنتجات", PackIconKind.PackageVariantClosedPlus, typeof(ProductModificationsReportViewModel), ScreenPermissionRegistry.SupervisoryReports),
            ("توزيعات أرباح المستثمرين", PackIconKind.AccountCash, typeof(InvestorProfitDistributionsReportViewModel), ScreenPermissionRegistry.SupervisoryReports),
            ("حركة رأس المال", PackIconKind.SwapHorizontal, typeof(CapitalMovementReportViewModel), ScreenPermissionRegistry.SupervisoryReports),
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
                CategoryAccentLightColor = cat.AccentLight,
                FlyoutItemLabel = "تقرير"
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

        var accent = category.CategoryAccentColor;
        var accentLight = category.CategoryAccentLightColor;

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
