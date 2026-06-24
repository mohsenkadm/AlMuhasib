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
            new()
            {
                Title = "لوحة التحكم",
                Icon = PackIconKind.ViewDashboard,
                ViewModelType = typeof(DashboardViewModel),
                ScreenName = ScreenPermissionRegistry.Dashboard
            },
            new()
            {
                Title = "المنتجات",
                Icon = PackIconKind.PackageVariantClosed,
                ViewModelType = typeof(ProductsViewModel),
                ScreenName = "Products"
            },
            new()
            {
                Title = "تصنيفات المنتجات",
                Icon = PackIconKind.TagMultiple,
                ViewModelType = typeof(CategoriesViewModel),
                ScreenName = "Categories"
            },
            new()
            {
                Title = "العملاء",
                Icon = PackIconKind.AccountGroup,
                ViewModelType = typeof(CustomersViewModel),
                ScreenName = "Customers"
            },
            new()
            {
                Title = "الموردون",
                Icon = PackIconKind.Factory,
                ViewModelType = typeof(SuppliersViewModel),
                ScreenName = "Suppliers"
            },
            new()
            {
                Title = "فاتورة مشتريات",
                Icon = PackIconKind.CartArrowDown,
                ViewModelType = typeof(PurchaseInvoiceViewModel),
                ScreenName = "PurchaseInvoice"
            },
            new()
            {
                Title = "فاتورة مبيعات",
                Icon = PackIconKind.CashRegister,
                ViewModelType = typeof(SalesInvoiceViewModel),
                ScreenName = "SaleInvoice"
            },
            new()
            {
                Title = "بيع سريع (POS)",
                Icon = PackIconKind.PointOfSale,
                ViewModelType = typeof(PosQuickSaleViewModel),
                ScreenName = "SaleInvoice"
            },
            new()
            {
                Title = "فاتورة أقساط",
                Icon = PackIconKind.CalendarClock,
                ViewModelType = typeof(InstallmentInvoiceViewModel),
                ScreenName = "InstallmentInvoice"
            },
            new()
            {
                Title = "لوحة التحصيل",
                Icon = PackIconKind.CashMultiple,
                ViewModelType = typeof(CollectionDashboardViewModel),
                ScreenName = "Installments"
            },
            new()
            {
                Title = "الأقساط",
                Icon = PackIconKind.CalendarMultipleCheck,
                ViewModelType = typeof(InstallmentsViewModel),
                ScreenName = "Installments"
            },
            new()
            {
                Title = "أرصدة الأقساط الافتتاحية",
                Icon = PackIconKind.History,
                ViewModelType = typeof(OpeningInstallmentBalanceViewModel),
                ScreenName = "OpeningInstallments"
            },
            new()
            {
                Title = "السندات",
                Icon = PackIconKind.FileDocumentOutline,
                ViewModelType = typeof(VouchersViewModel),
                ScreenName = "Vouchers"
            },
            new()
            {
                Title = "المصاريف",
                Icon = PackIconKind.CashMinus,
                ViewModelType = typeof(ExpenseViewModel),
                ScreenName = "Expenses"
            },
            new()
            {
                Title = "القاصات والمصرف",
                Icon = PackIconKind.Bank,
                ViewModelType = typeof(CashBankViewModel),
                ScreenName = "CashAndBank"
            },
            new()
            {
                Title = "المستثمرون",
                Icon = PackIconKind.TrendingUp,
                ViewModelType = typeof(InvestorsViewModel),
                ScreenName = "Investors"
            },
            new()
            {
                Title = "أرصدة المستثمرين الافتتاحية",
                Icon = PackIconKind.AccountCashOutline,
                ViewModelType = typeof(OpeningInvestorsViewModel),
                ScreenName = "OpeningInvestors"
            },
            new()
            {
                Title = "المخازن",
                Icon = PackIconKind.Warehouse,
                ViewModelType = typeof(WarehousesViewModel),
                ScreenName = "Warehouses"
            },
            new()
            {
                Title = "الأرصدة الافتتاحية",
                Icon = PackIconKind.PackageVariantClosedPlus,
                ViewModelType = typeof(OpeningStockViewModel),
                ScreenName = "OpeningStock"
            },
            new()
            {
                Title = "تسوية مخزنية",
                Icon = PackIconKind.TuneVerticalVariant,
                ViewModelType = typeof(StockAdjustmentViewModel),
                ScreenName = "StockAdjustment"
            },
            new()
            {
                Title = "التقارير",
                IsMenuSectionLabel = true,
                ScreenName = ScreenPermissionRegistry.Reports
            }
        };

        items.AddRange(ReportMenuCatalog.CreateCategoryMenuItems());

        items.AddRange(
        [
            new NavigationMenuItem
            {
                Title = "رأس المال",
                Icon = PackIconKind.Cash,
                ViewModelType = typeof(CapitalAdjustmentViewModel),
                ScreenName = "Capital"
            },
            new NavigationMenuItem
            {
                Title = "سجل العمليات",
                Icon = PackIconKind.History,
                ViewModelType = typeof(AuditLogViewModel),
                ScreenName = "AuditLog"
            },
            new NavigationMenuItem
            {
                Title = "المستخدمون",
                Icon = PackIconKind.AccountMultiple,
                ViewModelType = typeof(UsersViewModel),
                ScreenName = "Users"
            },
            new NavigationMenuItem
            {
                Title = "الصلاحيات",
                Icon = PackIconKind.ShieldKey,
                ViewModelType = typeof(PermissionsViewModel),
                ScreenName = "Permissions"
            },
            new NavigationMenuItem
            {
                Title = "معالج النقل",
                Icon = PackIconKind.DatabaseImport,
                ViewModelType = typeof(MigrationWizardViewModel),
                ScreenName = "DataImport"
            },
            new NavigationMenuItem
            {
                Title = "نقل مخازن",
                Icon = PackIconKind.TruckDelivery,
                ViewModelType = typeof(WarehouseTransferViewModel),
                ScreenName = "Warehouses"
            },
            new NavigationMenuItem
            {
                Title = "إعدادات الميزات",
                Icon = PackIconKind.TuneVariant,
                ViewModelType = typeof(BusinessFeaturesSettingsViewModel),
                ScreenName = "BusinessFeatures"
            },
            new NavigationMenuItem
            {
                Title = "إعدادات الطباعة",
                Icon = PackIconKind.PrinterSettings,
                ViewModelType = typeof(PrintLayoutSettingsViewModel),
                ScreenName = "PrintSettings"
            },
            new NavigationMenuItem
            {
                Title = "النسخ الاحتياطي",
                Icon = PackIconKind.DatabaseCog,
                ViewModelType = typeof(BackupRestoreViewModel),
                ScreenName = "Backup"
            },
            new NavigationMenuItem
            {
                Title = "المزامنة السحابية",
                Icon = PackIconKind.CloudSync,
                ViewModelType = typeof(CloudSyncSettingsViewModel),
                ScreenName = "CloudSync"
            },
            new NavigationMenuItem
            {
                Title = "تحديث النظام",
                Icon = PackIconKind.CloudDownload,
                ViewModelType = typeof(SystemUpdateViewModel),
                ScreenName = ScreenPermissionRegistry.SystemUpdate
            },
            new NavigationMenuItem
            {
                Title = "تبديل النظام (مطور)",
                Icon = PackIconKind.DeveloperBoard,
                ViewModelType = typeof(DeveloperSystemSwitchViewModel),
                ScreenName = ScreenPermissionRegistry.DeveloperSystem
            }
        ]);

        if (items.Count > 0)
            items[0].IsSelected = true;

        return items;
    }
}
