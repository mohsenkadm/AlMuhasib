using Qaid.TelegramBot.Application.Abstractions;
using Qaid.TelegramBot.Application.Models;
using Qaid.TelegramBot.Application.Reports;

namespace Qaid.TelegramBot.Application.Bot;

public static class BotMenus
{
    public static IReadOnlyList<IReadOnlyList<BotButton>> NotLinked(string linkUrl)
        =>
        [
            [new BotButton { Text = "🔗 ربط الحساب", Url = linkUrl }]
        ];

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
        rows.Add(
        [
            new BotButton { Text = "كشف زبون", CallbackData = CallbackData.PromptCustomer },
            new BotButton { Text = "كشف مورد", CallbackData = CallbackData.PromptSupplier }
        ]);
        rows.Add(
        [
            new BotButton { Text = "لوحة النظام", CallbackData = CallbackData.SystemDashboard },
            new BotButton { Text = "تغيير الفرع", CallbackData = CallbackData.BranchList }
        ]);
        rows.Add([new BotButton { Text = "إلغاء الربط", CallbackData = CallbackData.Unlink }]);
        return rows;
    }

    public static IReadOnlyList<IReadOnlyList<BotButton>> LookupResults(
        IReadOnlyList<LookupItemDto> items, bool customers)
    {
        var rows = new List<IReadOnlyList<BotButton>>();
        foreach (var item in items.Take(8))
        {
            var label = string.IsNullOrWhiteSpace(item.FileNumber)
                ? item.Name
                : $"{item.Name} ({item.FileNumber})";
            if (label.Length > 40)
                label = label[..40];
            rows.Add(
            [
                new BotButton
                {
                    Text = label,
                    CallbackData = customers
                        ? CallbackData.PickCustomer(item.SyncId)
                        : CallbackData.PickSupplier(item.SyncId)
                }
            ]);
        }
        rows.Add([new BotButton { Text = "القائمة الرئيسية", CallbackData = CallbackData.Main }]);
        return rows;
    }

    public static IReadOnlyList<IReadOnlyList<BotButton>> BranchPicker(IReadOnlyList<BranchInfoDto> branches)
    {
        var rows = new List<IReadOnlyList<BotButton>>();
        foreach (var b in branches)
        {
            var label = string.IsNullOrWhiteSpace(b.Code) ? b.Name : $"{b.Name} ({b.Code})";
            rows.Add([new BotButton { Text = label, CallbackData = CallbackData.SelectBranch(b.BranchId) }]);
        }
        rows.Add([new BotButton { Text = "القائمة الرئيسية", CallbackData = CallbackData.Main }]);
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
