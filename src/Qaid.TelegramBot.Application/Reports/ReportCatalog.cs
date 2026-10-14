using Qaid.TelegramBot.Application.Models;
using Qaid.TelegramBot.Application.Security;

namespace Qaid.TelegramBot.Application.Reports;

public sealed record ReportDefinition(
    ReportKind Kind,
    string Title,
    string Icon,
    ReportGroup Group,
    string Endpoint,
    bool NeedsPeriod);

public static class ReportCatalog
{
    public static IReadOnlyList<(ReportGroup Group, string Title, string Icon)> Groups { get; } =
    [
        (ReportGroup.SalesProfit, "المبيعات والأرباح", "📈"),
        (ReportGroup.Installments, "الأقساط", "📅"),
        (ReportGroup.Partners, "العملاء والموردين", "👥"),
        (ReportGroup.InventoryFinance, "المخزون والمالية", "📦"),
        (ReportGroup.Financial, "التقارير المالية", "🏦"),
        (ReportGroup.Supervisory, "التقارير الرقابية", "🛡️"),
        (ReportGroup.System, "النظام والمزامنة", "⚙️")
    ];

    public static IReadOnlyList<ReportDefinition> Items { get; } =
    [
        // ── المبيعات والأرباح ──
        new(ReportKind.DailyOperations, "ملخص اليوم", "🗓️", ReportGroup.SalesProfit, AllowedEndpoints.DailyOperations, true),
        new(ReportKind.DailySales, "المبيعات اليومية", "📆", ReportGroup.SalesProfit, AllowedEndpoints.DailySales, true),
        new(ReportKind.Sales, "تقرير المبيعات", "🧾", ReportGroup.SalesProfit, AllowedEndpoints.Sales, true),
        new(ReportKind.SalesByPaymentMethod, "المبيعات حسب الدفع", "💳", ReportGroup.SalesProfit, AllowedEndpoints.SalesByPaymentMethod, true),
        new(ReportKind.SalesByWarehouseUser, "المبيعات حسب المخزن/المستخدم", "🏪", ReportGroup.SalesProfit, AllowedEndpoints.SalesByWarehouseUser, true),
        new(ReportKind.Purchases, "المشتريات", "🛍️", ReportGroup.SalesProfit, AllowedEndpoints.Purchases, true),
        new(ReportKind.Profit, "تقرير الأرباح", "💹", ReportGroup.SalesProfit, AllowedEndpoints.Profit, true),
        new(ReportKind.ProfitComparison, "مقارنة الأرباح", "↔️", ReportGroup.SalesProfit, AllowedEndpoints.ProfitComparison, true),
        new(ReportKind.ProfitAndLoss, "الأرباح والخسائر", "📊", ReportGroup.SalesProfit, AllowedEndpoints.ProfitAndLoss, true),
        new(ReportKind.OperatingProfit, "ربح التشغيل", "📉", ReportGroup.SalesProfit, AllowedEndpoints.OperatingProfit, true),
        new(ReportKind.GrossProfitMargin, "هامش مجمل الربح", "🥧", ReportGroup.SalesProfit, AllowedEndpoints.GrossProfitMargin, true),
        new(ReportKind.TopProducts, "أفضل المنتجات", "⭐", ReportGroup.SalesProfit, AllowedEndpoints.TopProducts, true),
        new(ReportKind.ProductMargin, "هامش ربح المنتجات", "٪", ReportGroup.SalesProfit, AllowedEndpoints.ProductMargin, true),

        // ── الأقساط ──
        new(ReportKind.InstallmentsSummary, "ملخص الأقساط", "📋", ReportGroup.Installments, AllowedEndpoints.InstallmentsSummary, true),
        new(ReportKind.InstallmentsAging, "أعمار الأقساط", "⏳", ReportGroup.Installments, AllowedEndpoints.InstallmentsAging, false),
        new(ReportKind.InstallmentsPaid, "الأقساط المدفوعة", "✅", ReportGroup.Installments, AllowedEndpoints.InstallmentsPaid, true),
        new(ReportKind.InstallmentsUnpaid, "الأقساط غير المدفوعة", "⌛", ReportGroup.Installments, AllowedEndpoints.InstallmentsUnpaid, true),
        new(ReportKind.InstallmentsOverdue, "أقساط متأخرة", "⚠️", ReportGroup.Installments, AllowedEndpoints.InstallmentsOverdue, false),
        new(ReportKind.OpeningInstallmentBalances, "أرصدة افتتاحية للأقساط", "📘", ReportGroup.Installments, AllowedEndpoints.OpeningInstallmentBalances, true),
        new(ReportKind.CompanyFees, "رسوم الشركة", "٪", ReportGroup.Installments, AllowedEndpoints.CompanyFees, true),
        new(ReportKind.InstallmentSchedule, "جدول الأقساط", "🗓️", ReportGroup.Installments, AllowedEndpoints.InstallmentSchedule, true),

        // ── العملاء والموردين ──
        new(ReportKind.CustomersOverview, "ملخص الزبائن", "👤", ReportGroup.Partners, AllowedEndpoints.CustomersOverview, true),
        new(ReportKind.SuppliersOverview, "ملخص الموردين", "🏭", ReportGroup.Partners, AllowedEndpoints.SuppliersOverview, true),
        new(ReportKind.ReceivablesAging, "أعمار الذمم", "💰", ReportGroup.Partners, AllowedEndpoints.ReceivablesAging, false),
        new(ReportKind.PayablesAging, "أعمار الدائنين", "📤", ReportGroup.Partners, AllowedEndpoints.PayablesAging, false),
        new(ReportKind.CustomerCollections, "تحصيلات الزبائن", "💵", ReportGroup.Partners, AllowedEndpoints.CustomerCollections, true),
        new(ReportKind.OverdueCustomers, "زبائن متأخرون", "🚫", ReportGroup.Partners, AllowedEndpoints.OverdueCustomers, false),
        new(ReportKind.SupplierPayments, "مدفوعات الموردين", "💸", ReportGroup.Partners, AllowedEndpoints.SupplierPayments, true),

        // ── المخزون والمالية ──
        new(ReportKind.Warehouse, "تقرير المخزن", "🏬", ReportGroup.InventoryFinance, AllowedEndpoints.Warehouse, false),
        new(ReportKind.ProductMovement, "حركة المنتجات", "🔄", ReportGroup.InventoryFinance, AllowedEndpoints.ProductMovement, true),
        new(ReportKind.StockHealth, "صحة المخزون", "🩺", ReportGroup.InventoryFinance, AllowedEndpoints.StockHealth, false),
        new(ReportKind.InventoryReplenishment, "إعادة التموين", "📥", ReportGroup.InventoryFinance, AllowedEndpoints.InventoryReplenishment, true),
        new(ReportKind.MinimumQuantity, "حد أدنى للمخزون", "📉", ReportGroup.InventoryFinance, AllowedEndpoints.MinimumQuantity, false),
        new(ReportKind.InventoryValuation, "تقييم المخزون", "💲", ReportGroup.InventoryFinance, AllowedEndpoints.InventoryValuation, false),
        new(ReportKind.StockTaking, "جرد المخزون", "☑️", ReportGroup.InventoryFinance, AllowedEndpoints.StockTaking, false),
        new(ReportKind.Cogs, "تكلفة البضاعة", "🛒", ReportGroup.InventoryFinance, AllowedEndpoints.Cogs, true),
        new(ReportKind.Expenses, "المصروفات", "🧾", ReportGroup.InventoryFinance, AllowedEndpoints.Expenses, true),
        new(ReportKind.CashFlow, "التدفق النقدي", "💧", ReportGroup.InventoryFinance, AllowedEndpoints.CashFlow, true),
        new(ReportKind.CashBalances, "أرصدة النقد والبنوك", "🏦", ReportGroup.InventoryFinance, AllowedEndpoints.CashBalancesSummary, false),
        new(ReportKind.CashBoxMovement, "حركة الصناديق", "🏧", ReportGroup.InventoryFinance, AllowedEndpoints.CashBoxMovement, true),
        new(ReportKind.BankAccountStatement, "كشف الحساب المصرفي", "🏛️", ReportGroup.InventoryFinance, AllowedEndpoints.BankAccountStatement, true),
        new(ReportKind.Transfers, "التحويلات", "🔁", ReportGroup.InventoryFinance, AllowedEndpoints.Transfers, true),

        // ── التقارير المالية ──
        new(ReportKind.BalanceSheet, "الميزانية العمومية", "⚖️", ReportGroup.Financial, AllowedEndpoints.BalanceSheet, false),
        new(ReportKind.IncomeExpense, "الإيرادات والمصروفات", "↕️", ReportGroup.Financial, AllowedEndpoints.IncomeExpense, true),
        new(ReportKind.FinancialPosition, "الوضع المالي", "📐", ReportGroup.Financial, AllowedEndpoints.FinancialPositionSummary, false),
        new(ReportKind.StatementOfFinancialPosition, "قائمة المركز المالي", "📑", ReportGroup.Financial, AllowedEndpoints.StatementOfFinancialPosition, false),

        // ── التقارير الرقابية ──
        new(ReportKind.InvestorProfitDistributions, "توزيع أرباح المستثمرين", "🤝", ReportGroup.Supervisory, AllowedEndpoints.InvestorProfitDistributions, true),
        new(ReportKind.CapitalMovement, "حركة رأس المال", "💹", ReportGroup.Supervisory, AllowedEndpoints.CapitalMovement, true),

        // ── النظام ──
        new(ReportKind.LastSync, "آخر مزامنة", "🔄", ReportGroup.System, AllowedEndpoints.SyncStatus, false)
    ];

    private static readonly Dictionary<ReportKind, ReportDefinition> ByKind =
        Items.ToDictionary(i => i.Kind);

    public static ReportDefinition Get(ReportKind kind) => ByKind[kind];

    public static bool TryGet(ReportKind kind, out ReportDefinition def)
        => ByKind.TryGetValue(kind, out def!);

    public static string TitleOf(ReportKind kind)
        => ByKind.TryGetValue(kind, out var d) ? $"{d.Icon} {d.Title}" : kind.ToString();

    public static string ShortTitleOf(ReportKind kind)
        => ByKind.TryGetValue(kind, out var d) ? d.Title : kind.ToString();

    public static bool NeedsPeriod(ReportKind kind)
        => ByKind.TryGetValue(kind, out var d) && d.NeedsPeriod;

    public static IReadOnlyList<ReportDefinition> ForGroup(ReportGroup group)
        => Items.Where(i => i.Group == group).ToList();

    public static string GroupTitle(ReportGroup group)
    {
        var g = Groups.FirstOrDefault(x => x.Group == group);
        return g.Title is null ? group.ToString() : $"{g.Icon} {g.Title}";
    }

    public static bool TryGetByCommand(string command, out ReportKind kind)
    {
        kind = default;
        var key = command.Trim().TrimStart('/')
            .Split(['@', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0]
            .ToLowerInvariant();
        kind = key switch
        {
            "today" => ReportKind.DailyOperations,
            "cash" => ReportKind.CashBalances,
            "sales" => ReportKind.DailySales,
            "expenses" => ReportKind.Expenses,
            "pnl" => ReportKind.ProfitAndLoss,
            "overdue" => ReportKind.InstallmentsOverdue,
            "stock" => ReportKind.MinimumQuantity,
            "collections" => ReportKind.CustomerCollections,
            "position" => ReportKind.FinancialPosition,
            "cogs" => ReportKind.Cogs,
            "sync" => ReportKind.LastSync,
            _ => (ReportKind)(-1)
        };
        return (int)kind >= 0;
    }
}

public static class ReportPeriodHelper
{
    public static DateRange Resolve(ReportPeriod period, DateTime? today = null)
    {
        var day = (today ?? DateTime.Today).Date;
        return period switch
        {
            ReportPeriod.Today => new DateRange
            {
                From = day,
                To = day.AddDays(1).AddTicks(-1),
                Label = "اليوم"
            },
            ReportPeriod.Yesterday => new DateRange
            {
                From = day.AddDays(-1),
                To = day.AddTicks(-1),
                Label = "أمس"
            },
            ReportPeriod.Last7Days => new DateRange
            {
                From = day.AddDays(-6),
                To = day.AddDays(1).AddTicks(-1),
                Label = "آخر 7 أيام"
            },
            ReportPeriod.ThisMonth => new DateRange
            {
                From = new DateTime(day.Year, day.Month, 1),
                To = day.AddDays(1).AddTicks(-1),
                Label = "هذا الشهر"
            },
            ReportPeriod.LastMonth => ResolveLastMonth(day),
            ReportPeriod.ThisYear => new DateRange
            {
                From = new DateTime(day.Year, 1, 1),
                To = day.AddDays(1).AddTicks(-1),
                Label = "خلال هذه السنة"
            },
            _ => new DateRange { From = day, To = day.AddDays(1).AddTicks(-1), Label = "اليوم" }
        };
    }

    private static DateRange ResolveLastMonth(DateTime day)
    {
        var firstThis = new DateTime(day.Year, day.Month, 1);
        var firstLast = firstThis.AddMonths(-1);
        return new DateRange
        {
            From = firstLast,
            To = firstThis.AddTicks(-1),
            Label = "الشهر الماضي"
        };
    }

    public static string LabelOf(ReportPeriod period) => Resolve(period).Label;
}
