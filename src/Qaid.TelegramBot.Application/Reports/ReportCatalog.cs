using Qaid.TelegramBot.Application.Models;
using Qaid.TelegramBot.Application.Security;

namespace Qaid.TelegramBot.Application.Reports;

public static class ReportCatalog
{
    public static IReadOnlyList<(ReportKind Kind, string Title, string Endpoint, bool NeedsPeriod)> Items { get; } =
    [
        (ReportKind.DailySales, "المبيعات اليومية", AllowedEndpoints.DailySales, true),
        (ReportKind.Expenses, "المصروفات", AllowedEndpoints.Expenses, true),
        (ReportKind.ProfitAndLoss, "الأرباح والخسائر", AllowedEndpoints.ProfitAndLoss, true),
        (ReportKind.CashBalances, "أرصدة النقد والبنوك", AllowedEndpoints.CashBalancesSummary, false),
        (ReportKind.CashFlow, "التدفق النقدي", AllowedEndpoints.CashFlow, true),
        (ReportKind.CustomersOverview, "ملخص الزبائن", AllowedEndpoints.CustomersOverview, true),
        (ReportKind.LastSync, "آخر مزامنة", AllowedEndpoints.SyncStatus, false)
    ];

    public static string TitleOf(ReportKind kind)
        => Items.First(i => i.Kind == kind).Title;

    public static bool NeedsPeriod(ReportKind kind)
        => Items.First(i => i.Kind == kind).NeedsPeriod;
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
