using System.Text;

namespace AlMuhasib.Core.Intelligence;

/// <summary>يصوغ الإجابة العربية من أرقام التقارير الرسمية فقط.</summary>
public static class ArabicResponseGenerator
{
    public static AssistantAnswer Denied(string message) =>
        TextOnly(message, AccountingIntent.Unknown, updatesContext: false);

    public static AssistantAnswer Help() =>
        TextOnly(
            "أقدر أقرأ بيانات نظام قيد المتصل حالياً، ضمن صلاحياتك، وأجيب من التقارير الرسمية. جرّب مثلاً:\n" +
            "• كم إجمالي المبيعات اليوم؟\n" +
            "• كم المبالغ المستحقة على العملاء؟\n" +
            "• ما إجمالي المصروفات هذا الشهر؟\n" +
            "• من هم أكثر العملاء شراءً؟\n" +
            "• ما المنتجات الأكثر مبيعاً؟\n" +
            "• كم رصيد الصندوق الحالي؟\n" +
            "• اعرض لي ملخصاً مالياً لهذا الشهر.\n" +
            "يمكنك بعدها تغيير الفترة، مثل: وماذا عن الشهر الماضي؟",
            AccountingIntent.Help,
            false);

    public static AssistantAnswer Unknown() =>
        TextOnly(
            "لم أفهم السؤال بشكل كافٍ لأربطه بتقرير محدد. أعد صياغته، مثلاً: «كم مبيعات اليوم؟» أو «كم المستحق على العملاء؟».",
            AccountingIntent.Unknown,
            false);

    public static AssistantAnswer Unsafe() =>
        TextOnly(
            "لا أستطيع تنفيذ أوامر على قاعدة البيانات. اسأل سؤالاً محاسبياً مثل: كم مبيعات اليوم؟",
            AccountingIntent.Unsafe,
            false);

    public static AssistantAnswer Unsupported() =>
        TextOnly(
            "هذه الجزئية غير موجودة كتقرير مستقل في النظام الحالي. قيد يعتمد على الفواتير والسندات والتقارير المالية، ولا يحتفظ بدفتر ضريبة أو قيد يومي مزدوج منفصل. اسأل عن المبيعات أو المصروفات أو الصندوق أو الذمم.",
            AccountingIntent.Unsupported,
            false);

    public static AssistantAnswer WrongModule() =>
        TextOnly(
            "المساعد يقرأ بيانات نظام قيد المحاسبي. النظام المفتوح الآن ليس نظام المحاسبة، لذلك لا أستخرج أرقاماً من قاعدة هذا النظام.",
            AccountingIntent.Unsupported,
            false);

    public static AssistantAnswer Timeout(bool shared) =>
        Failed(shared
            ? "استغرق الاتصال بقاعدة البيانات المشتركة وقتاً طويلاً. تحقق من الشبكة ثم أعد المحاولة."
            : "استغرق الاتصال بقاعدة البيانات المحلية وقتاً طويلاً. أعد المحاولة.");

    public static AssistantAnswer ConnectionFailed(bool shared) =>
        Failed(shared
            ? "تعذّر الاتصال بقاعدة البيانات المشتركة. تحقق من الشبكة ثم أعد المحاولة."
            : "تعذّر الاتصال بقاعدة البيانات المحلية. أعد المحاولة.");

    public static AssistantAnswer ReadFailed() =>
        Failed("تعذّر قراءة البيانات الآن. أعد المحاولة بعد قليل.");

    public static AssistantAnswer Sales(QueryPlan plan, SalesSnapshot snapshot, string headlinePeriod)
    {
        var net = AssistantMoney.Iqd(snapshot.NetIqd);
        var text = new StringBuilder($"إجمالي مبيعات {headlinePeriod}: {net}.");
        if (snapshot.InvoiceCount == 0 && snapshot.NetIqd == 0 && snapshot.NetUsd == 0)
            text.Append(" لا توجد فواتير ضمن هذه الفترة.");

        var metrics = new List<AssistantMetricCard>
        {
            new() { Label = "صافي المبيعات", Value = net, Hint = "مطابق لتقرير المبيعات" },
            new() { Label = "عدد الفواتير", Value = AssistantMoney.Count(snapshot.InvoiceCount) }
        };
        if (snapshot.ShowReturnSplit)
        {
            metrics.Add(new() { Label = "إجمالي قبل المرتجعات", Value = AssistantMoney.Iqd(snapshot.GrossIqd) });
            metrics.Add(new() { Label = "المرتجعات", Value = AssistantMoney.Iqd(snapshot.ReturnsIqd) });
        }

        metrics.Add(new() { Label = "نقدي", Value = AssistantMoney.Iqd(snapshot.CashIqd) });
        metrics.Add(new() { Label = "آجل", Value = AssistantMoney.Iqd(snapshot.CreditIqd) });
        metrics.Add(new() { Label = "أقساط", Value = AssistantMoney.Iqd(snapshot.InstallmentIqd) });
        if (snapshot.NetUsd != 0)
            metrics.Add(new() { Label = "صافي الدولار", Value = AssistantMoney.Usd(snapshot.NetUsd), Hint = "منفصل عن الدينار" });

        return Done(text.ToString(), plan, metrics, chart: snapshot.Daily);
    }

    public static AssistantAnswer Comparison(
        QueryPlan plan,
        string title,
        decimal current,
        decimal previous,
        IReadOnlyList<AssistantChartPoint> chart)
    {
        var change = ResultAnalysisService.ChangePercent(current, previous);
        var diff = current - previous;
        var text = $"{title} {plan.PeriodLabel}: {AssistantMoney.Iqd(current)}. {plan.CompareLabel}: {AssistantMoney.Iqd(previous)}. الفرق: {AssistantMoney.Iqd(diff)}. نسبة التغير: {AssistantMoney.Percent(change)}.";
        var metrics = new List<AssistantMetricCard>
        {
            new() { Label = plan.PeriodLabel, Value = AssistantMoney.Iqd(current) },
            new() { Label = plan.CompareLabel ?? "الفترة السابقة", Value = AssistantMoney.Iqd(previous) },
            new() { Label = "الفرق", Value = AssistantMoney.Iqd(diff) },
            new() { Label = "نسبة التغير", Value = AssistantMoney.Percent(change) }
        };
        return Done(text, plan, metrics, chart: chart);
    }

    public static AssistantAnswer Ranked(
        QueryPlan plan,
        string text,
        IReadOnlyList<AssistantMetricCard> metrics,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyList<AssistantChartPoint>? chart = null,
        int? focusCustomerId = null,
        string? focusCustomerName = null)
    {
        var answer = Done(text, plan, metrics, headers, rows, chart);
        answer.FocusCustomerId = focusCustomerId;
        answer.FocusCustomerName = focusCustomerName;
        return answer;
    }

    public static AssistantAnswer NeedCustomer() =>
        TextOnly("من تقصد؟ اكتب اسم العميل، مثلاً: كم اشترى العميل أحمد؟", AccountingIntent.CustomerPurchases, false);

    public static AssistantAnswer CustomerNotFound(string name) =>
        TextOnly($"لم أجد عميلاً باسم «{name}» ضمن الفرع الحالي.", AccountingIntent.CustomerPurchases, false);

    public static AssistantAnswer ChooseCustomer(IReadOnlyList<string> names)
    {
        var lines = string.Join("\n", names.Select(name => "• " + name));
        return TextOnly("وجدت أكثر من عميل. اكتب الاسم الكامل:\n" + lines, AccountingIntent.CustomerPurchases, false);
    }

    private static AssistantAnswer Failed(string message) =>
        TextOnly(message, AccountingIntent.Unknown, false, isError: true);

    private static AssistantAnswer TextOnly(string text, AccountingIntent intent, bool updatesContext, bool isError = false) =>
        new()
        {
            Text = text,
            PlainText = text,
            Intent = intent,
            IsError = isError,
            UpdatesContext = updatesContext
        };

    private static AssistantAnswer Done(
        string text,
        QueryPlan plan,
        IReadOnlyList<AssistantMetricCard> metrics,
        IReadOnlyList<string>? headers = null,
        IReadOnlyList<IReadOnlyList<string>>? rows = null,
        IReadOnlyList<AssistantChartPoint>? chart = null)
    {
        var plain = new StringBuilder(text);
        foreach (var metric in metrics)
        {
            plain.AppendLine();
            plain.Append(metric.Label).Append(": ").Append(metric.Value);
        }

        if (headers is { Count: > 0 } && rows is { Count: > 0 })
        {
            plain.AppendLine();
            plain.Append(string.Join(" | ", headers));
            foreach (var row in rows)
            {
                plain.AppendLine();
                plain.Append(string.Join(" | ", row));
            }
        }

        plain.AppendLine();
        plain.Append("المصدر: ").Append(plan.ReportName).Append(" | الفترة: ").Append(plan.PeriodLabel);

        return new AssistantAnswer
        {
            Text = text,
            PlainText = plain.ToString().Trim(),
            Intent = plan.Intent,
            UpdatesContext = true,
            SourceReport = plan.ReportName,
            PeriodLabel = plan.PeriodLabel,
            Metrics = metrics.ToList(),
            TableHeaders = headers?.ToList() ?? [],
            TableRows = rows?.Select(row => row.ToList()).ToList() ?? [],
            Chart = chart?.Take(31).ToList() ?? []
        };
    }
}
