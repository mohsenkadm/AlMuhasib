using Qaid.TelegramBot.Application.Models;
using Qaid.TelegramBot.Application.Security;

namespace Qaid.TelegramBot.Application.Reports;

public static class ReportCatalog
{
    public static IReadOnlyList<(ReportKind Kind, string Title, string Endpoint, bool NeedsPeriod)> Items { get; } =
    [
        (ReportKind.DailyOperations, "ملخص اليوم", AllowedEndpoints.DailyOperations, true),
        (ReportKind.DailySales, "المبيعات اليومية", AllowedEndpoints.DailySales, true),
        (ReportKind.SalesByPaymentMethod, "المبيعات حسب الدفع", AllowedEndpoints.SalesByPaymentMethod, true),
        (ReportKind.Expenses, "المصروفات", AllowedEndpoints.Expenses, true),
        (ReportKind.ProfitAndLoss, "الأرباح والخسائر", AllowedEndpoints.ProfitAndLoss, true),
        (ReportKind.CashBalances, "أرصدة النقد والبنوك", AllowedEndpoints.CashBalancesSummary, false),
        (ReportKind.CashFlow, "التدفق النقدي", AllowedEndpoints.CashFlow, true),
        (ReportKind.CustomersOverview, "ملخص الزبائن", AllowedEndpoints.CustomersOverview, true),
        (ReportKind.OverdueCustomers, "زبائن متأخرون", AllowedEndpoints.OverdueCustomers, false),
        (ReportKind.ReceivablesAging, "أعمار الذمم", AllowedEndpoints.ReceivablesAging, false),
        (ReportKind.InstallmentsSummary, "ملخص الأقساط", AllowedEndpoints.InstallmentsSummary, true),
        (ReportKind.InstallmentsOverdue, "أقساط متأخرة", AllowedEndpoints.InstallmentsOverdue, false),
        (ReportKind.Purchases, "المشتريات", AllowedEndpoints.Purchases, true),
        (ReportKind.SuppliersOverview, "ملخص الموردين", AllowedEndpoints.SuppliersOverview, true),
        (ReportKind.TopProducts, "أفضل المنتجات", AllowedEndpoints.TopProducts, true),
        (ReportKind.GrossProfitMargin, "هامش مجمل الربح", AllowedEndpoints.GrossProfitMargin, true),
        (ReportKind.Cogs, "تكلفة البضاعة", AllowedEndpoints.Cogs, true),
        (ReportKind.MinimumQuantity, "حد أدنى للمخزون", AllowedEndpoints.MinimumQuantity, false),
        (ReportKind.StockHealth, "صحة المخزون", AllowedEndpoints.StockHealth, false),
        (ReportKind.InventoryValuation, "تقييم المخزون", AllowedEndpoints.InventoryValuation, false),
        (ReportKind.CustomerCollections, "تحصيلات الزبائن", AllowedEndpoints.CustomerCollections, true),
        (ReportKind.SupplierPayments, "مدفوعات الموردين", AllowedEndpoints.SupplierPayments, true),
        (ReportKind.PayablesAging, "أعمار الدائنين", AllowedEndpoints.PayablesAging, false),
        (ReportKind.Transfers, "التحويلات", AllowedEndpoints.Transfers, true),
        (ReportKind.FinancialPosition, "الوضع المالي", AllowedEndpoints.FinancialPositionSummary, false),
        (ReportKind.LastSync, "آخر مزامنة", AllowedEndpoints.SyncStatus, false)
    ];

    public static string TitleOf(ReportKind kind)
        => Items.First(i => i.Kind == kind).Title;

    public static bool NeedsPeriod(ReportKind kind)
        => Items.First(i => i.Kind == kind).NeedsPeriod;

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
