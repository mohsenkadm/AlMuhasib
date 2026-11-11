namespace AlMuhasib.Core.Intelligence;

/// <summary>يحوّل الفهم إلى خطة قراءة من العمليات المعتمدة فقط.</summary>
public static class QueryPlanningService
{
    public static QueryPlan Plan(string? question, ConversationContext? context = null)
    {
        context ??= new ConversationContext();
        var understanding = IntentRecognitionService.Recognize(question, context);
        var entities = EntityExtractionService.Extract(understanding.NormalizedText, context, understanding);
        return Build(understanding, entities, context);
    }

    public static QueryPlan Build(Understanding understanding, QueryEntities entities, ConversationContext context)
    {
        var intent = understanding.Intent;
        var domain = AccountingKnowledgeBase.DomainOf(intent);
        DateTime? from = entities.From;
        DateTime? to = entities.To;
        var label = entities.PeriodLabel;
        DateTime? compareFrom = entities.CompareFrom;
        DateTime? compareTo = entities.CompareTo;
        var compareLabel = entities.CompareLabel;

        if (!entities.HasPeriod
            && (understanding.IsFollowUp || entities.UseContextCustomer)
            && context.From is not null && context.To is not null
            && AccountingKnowledgeBase.UsesDateRange(intent) && !AccountingKnowledgeBase.IsPointInTime(intent))
        {
            from = context.From;
            to = context.To;
            label = context.PeriodLabel;
        }

        if (!entities.HasPeriod && label is null)
            ApplyDefault(intent, ref from, ref to, ref label, ref compareFrom, ref compareTo, ref compareLabel);

        int? customerId = null;
        string? contextName = null;
        if (entities.UseContextCustomer)
        {
            customerId = context.CustomerId;
            contextName = context.CustomerName;
        }

        var asOf = AccountingKnowledgeBase.IsPointInTime(intent)
            ? (entities.HasPeriod ? to : DateTime.Today)
            : to;

        return new QueryPlan
        {
            Intent = intent,
            Domain = domain,
            NormalizedText = understanding.NormalizedText,
            IsFollowUp = understanding.IsFollowUp,
            ReportName = AccountingKnowledgeBase.ReportName(intent),
            PeriodLabel = label ?? "الفترة الحالية",
            CompareLabel = compareLabel,
            From = from,
            To = to,
            CompareFrom = compareFrom,
            CompareTo = compareTo,
            AsOf = asOf,
            CustomerId = customerId,
            CustomerNameQuery = entities.CustomerNameQuery,
            ContextCustomerName = contextName,
            TopCount = entities.TopCount
        };
    }

    private static void ApplyDefault(
        AccountingIntent intent,
        ref DateTime? from,
        ref DateTime? to,
        ref string? label,
        ref DateTime? compareFrom,
        ref DateTime? compareTo,
        ref string? compareLabel)
    {
        var today = DateTime.Today;
        if (intent is AccountingIntent.SalesComparison or AccountingIntent.ExpenseComparison or AccountingIntent.ProfitComparison)
        {
            var start = new DateTime(today.Year, today.Month, 1);
            var lastEnd = start.AddDays(-1);
            from = start;
            to = today;
            label = "هذا الشهر حتى اليوم";
            compareFrom = new DateTime(lastEnd.Year, lastEnd.Month, 1);
            compareTo = lastEnd;
            compareLabel = "الشهر الماضي";
            return;
        }

        if (AccountingKnowledgeBase.IsPointInTime(intent))
        {
            to = today;
            label = "حتى اليوم";
            return;
        }

        if (!AccountingKnowledgeBase.UsesDateRange(intent))
        {
            label = "—";
            return;
        }

        from = new DateTime(today.Year, today.Month, 1);
        to = today;
        label = "هذا الشهر حتى اليوم";
    }
}
