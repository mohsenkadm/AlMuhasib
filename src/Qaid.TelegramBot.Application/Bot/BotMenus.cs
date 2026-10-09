using Qaid.TelegramBot.Application.Abstractions;
using Qaid.TelegramBot.Application.Models;
using Qaid.TelegramBot.Application.Reports;

namespace Qaid.TelegramBot.Application.Bot;

public static class BotMenus
{
    public static IReadOnlyList<IReadOnlyList<BotButton>> MainMenu(string companyName)
    {
        var rows = new List<IReadOnlyList<BotButton>>();
        var buffer = new List<BotButton>();
        foreach (var item in ReportCatalog.Items)
        {
            buffer.Add(new BotButton { Text = item.Title, CallbackData = CallbackData.Report(item.Kind) });
            if (buffer.Count == 2)
            {
                rows.Add(buffer);
                buffer = [];
            }
        }
        if (buffer.Count > 0)
            rows.Add(buffer);
        rows.Add([new BotButton { Text = "إلغاء الربط", CallbackData = CallbackData.Unlink }]);
        return rows;
    }

    public static IReadOnlyList<IReadOnlyList<BotButton>> PeriodMenu(ReportKind kind)
        =>
        [
            [
                new BotButton { Text = "اليوم", CallbackData = CallbackData.Period(kind, ReportPeriod.Today) },
                new BotButton { Text = "أمس", CallbackData = CallbackData.Period(kind, ReportPeriod.Yesterday) }
            ],
            [
                new BotButton { Text = "آخر 7 أيام", CallbackData = CallbackData.Period(kind, ReportPeriod.Last7Days) },
                new BotButton { Text = "هذا الشهر", CallbackData = CallbackData.Period(kind, ReportPeriod.ThisMonth) }
            ],
            [
                new BotButton { Text = "الشهر الماضي", CallbackData = CallbackData.Period(kind, ReportPeriod.LastMonth) }
            ],
            [
                new BotButton { Text = "القائمة الرئيسية", CallbackData = CallbackData.Main }
            ]
        ];

    public static IReadOnlyList<IReadOnlyList<BotButton>> AfterResult(ReportKind kind, bool needsPeriod)
    {
        var rows = new List<IReadOnlyList<BotButton>>();
        if (needsPeriod)
            rows.Add([new BotButton { Text = "تغيير الفترة", CallbackData = CallbackData.BackPeriod(kind) }]);
        rows.Add([new BotButton { Text = "القائمة الرئيسية", CallbackData = CallbackData.Main }]);
        return rows;
    }

    public static IReadOnlyList<IReadOnlyList<BotButton>> UnlinkConfirm()
        =>
        [
            [
                new BotButton { Text = "نعم، إلغاء الربط", CallbackData = CallbackData.UnlinkConfirm },
                new BotButton { Text = "إلغاء", CallbackData = CallbackData.Main }
            ]
        ];
}
