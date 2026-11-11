namespace AlMuhasib.Core.Intelligence;

/// <summary>تعريف العمليات المحاسبية الموجودة فعلياً وربطها بالتقارير والصلاحيات.</summary>
public static class AccountingKnowledgeBase
{
    public static AssistantDataDomain DomainOf(AccountingIntent intent) => intent switch
    {
        AccountingIntent.SalesSummary or AccountingIntent.SalesComparison or AccountingIntent.TopProducts
            => AssistantDataDomain.Sales,
        AccountingIntent.PurchasesSummary => AssistantDataDomain.Purchases,
        AccountingIntent.ExpensesSummary or AccountingIntent.ExpenseComparison => AssistantDataDomain.Expenses,
        AccountingIntent.ProfitSummary or AccountingIntent.ProfitComparison => AssistantDataDomain.Profit,
        AccountingIntent.CustomerDebt or AccountingIntent.CustomerPurchases or AccountingIntent.TopCustomers
            or AccountingIntent.CustomerCollections => AssistantDataDomain.CustomerBalances,
        AccountingIntent.SupplierDebt or AccountingIntent.SupplierPayments => AssistantDataDomain.Suppliers,
        AccountingIntent.CashBalance => AssistantDataDomain.Cash,
        AccountingIntent.LowStock => AssistantDataDomain.Inventory,
        AccountingIntent.FinancialSummary => AssistantDataDomain.FinancialReports,
        _ => AssistantDataDomain.None
    };

    public static string ReportName(AccountingIntent intent) => intent switch
    {
        AccountingIntent.SalesSummary or AccountingIntent.SalesComparison or AccountingIntent.CustomerPurchases
            => "تقرير المبيعات",
        AccountingIntent.TopCustomers => "ملخص العملاء",
        AccountingIntent.CustomerDebt => "أعمار ذمم العملاء",
        AccountingIntent.TopProducts => "أفضل المنتجات",
        AccountingIntent.ExpensesSummary or AccountingIntent.ExpenseComparison => "تقرير المصروفات",
        AccountingIntent.CashBalance => "أرصدة الصندوق والمصرف",
        AccountingIntent.PurchasesSummary => "تقرير المشتريات",
        AccountingIntent.ProfitSummary or AccountingIntent.ProfitComparison or AccountingIntent.FinancialSummary
            => "قائمة الدخل",
        AccountingIntent.LowStock => "تقرير كميات الحد الأدنى",
        AccountingIntent.SupplierDebt => "أعمار ذمم الموردين",
        AccountingIntent.CustomerCollections => "تحصيلات العملاء",
        AccountingIntent.SupplierPayments => "دفعات الموردين",
        _ => "بيانات النظام"
    };

    public static string DenyMessage(AssistantDataDomain domain) => domain switch
    {
        AssistantDataDomain.CustomerBalances => "عذراً، لا تمتلك الصلاحية اللازمة للوصول إلى بيانات أرصدة العملاء.",
        AssistantDataDomain.Suppliers => "عذراً، لا تمتلك الصلاحية اللازمة للوصول إلى بيانات الموردين.",
        AssistantDataDomain.Inventory => "عذراً، لا تمتلك الصلاحية اللازمة للوصول إلى بيانات المخزون.",
        AssistantDataDomain.Cash => "عذراً، لا تمتلك الصلاحية اللازمة للوصول إلى بيانات الصندوق.",
        AssistantDataDomain.Expenses => "عذراً، لا تمتلك الصلاحية اللازمة للوصول إلى بيانات المصروفات.",
        AssistantDataDomain.Profit => "عذراً، لا تمتلك الصلاحية اللازمة للوصول إلى بيانات الأرباح.",
        AssistantDataDomain.Sales => "عذراً، لا تمتلك الصلاحية اللازمة للوصول إلى بيانات المبيعات.",
        AssistantDataDomain.Purchases => "عذراً، لا تمتلك الصلاحية اللازمة للوصول إلى بيانات المشتريات.",
        AssistantDataDomain.FinancialReports => "عذراً، لا تمتلك الصلاحية اللازمة للوصول إلى التقارير المالية.",
        _ => "عذراً، لا تمتلك الصلاحية اللازمة لاستخدام المساعد الذكي."
    };

    public static bool UsesDateRange(AccountingIntent intent) => intent is not (
        AccountingIntent.CashBalance
        or AccountingIntent.LowStock
        or AccountingIntent.Unknown
        or AccountingIntent.Help
        or AccountingIntent.Unsafe
        or AccountingIntent.Unsupported);

    public static bool IsPointInTime(AccountingIntent intent) =>
        intent is AccountingIntent.CustomerDebt or AccountingIntent.SupplierDebt or AccountingIntent.CashBalance or AccountingIntent.LowStock;
}
