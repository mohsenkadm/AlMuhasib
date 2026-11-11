using System.Diagnostics;
using AlMuhasib.Core.Intelligence;
using Xunit;

namespace AlMuhasib.Core.Tests.Intelligence;

public class ArabicUnderstandingTests
{
    public static TheoryData<string> SalesTodayPhrases { get; } = new()
    {
        "كم مبيعات اليوم؟",
        "شكد بعنا اليوم؟",
        "أريد إجمالي مبيعات اليوم.",
        "أعطني مجموع البيع اليوم.",
        "ما قيمة المبيعات لهذا اليوم؟",
        "مبيعاات اليوم"
    };

    [Theory]
    [MemberData(nameof(SalesTodayPhrases))]
    public void Different_phrasing_maps_to_today_sales(string question)
    {
        var plan = QueryPlanningService.Plan(question);
        var today = DateTime.Today;

        Assert.Equal(AccountingIntent.SalesSummary, plan.Intent);
        Assert.Equal(today, plan.From);
        Assert.Equal(today, plan.To);
        Assert.Equal(AssistantDataDomain.Sales, plan.Domain);
        Assert.Equal("تقرير المبيعات", plan.ReportName);
    }

    [Fact]
    public void Iraqi_low_stock_phrase_is_minimum_quantity()
    {
        var plan = QueryPlanningService.Plan("شنو المنتجات اللي قربت تخلص؟");
        Assert.Equal(AccountingIntent.LowStock, plan.Intent);
        Assert.Equal(AssistantDataDomain.Inventory, plan.Domain);
    }

    [Fact]
    public void Customer_debts_are_not_treated_as_sales()
    {
        var plan = QueryPlanningService.Plan("كم ديوننا على الزبائن؟");
        Assert.Equal(AccountingIntent.CustomerDebt, plan.Intent);
        Assert.Equal(AssistantDataDomain.CustomerBalances, plan.Domain);
    }

    [Fact]
    public void Expenses_this_month_use_month_to_date()
    {
        var plan = QueryPlanningService.Plan("كم صرفنا هذا الشهر؟");
        var today = DateTime.Today;
        Assert.Equal(AccountingIntent.ExpensesSummary, plan.Intent);
        Assert.Equal(new DateTime(today.Year, today.Month, 1), plan.From);
        Assert.Equal(today, plan.To);
    }

    [Fact]
    public void Top_customers_and_products_stay_distinct()
    {
        Assert.Equal(AccountingIntent.TopCustomers, QueryPlanningService.Plan("من أكثر عميل اشترى من عندنا؟").Intent);
        Assert.Equal(AccountingIntent.TopProducts, QueryPlanningService.Plan("ما المنتجات الأكثر مبيعاً؟").Intent);
    }

    [Fact]
    public void Comparison_extracts_current_and_previous_month()
    {
        var plan = QueryPlanningService.Plan("قارن مبيعات هذا الشهر بالشهر الماضي");
        var today = DateTime.Today;
        var start = new DateTime(today.Year, today.Month, 1);
        var lastEnd = start.AddDays(-1);

        Assert.Equal(AccountingIntent.SalesComparison, plan.Intent);
        Assert.Equal(start, plan.From);
        Assert.Equal(today, plan.To);
        Assert.Equal(new DateTime(lastEnd.Year, lastEnd.Month, 1), plan.CompareFrom);
        Assert.Equal(lastEnd, plan.CompareTo);
    }

    [Fact]
    public void Follow_up_changes_only_the_period()
    {
        var context = new ConversationContext();
        var first = QueryPlanningService.Plan("كم مبيعات اليوم؟", context);
        context.LastIntent = first.Intent;
        context.From = first.From;
        context.To = first.To;
        context.PeriodLabel = first.PeriodLabel;

        var second = QueryPlanningService.Plan("وماذا عن الشهر الماضي؟", context);
        var today = DateTime.Today;
        var start = new DateTime(today.Year, today.Month, 1);
        var lastEnd = start.AddDays(-1);

        Assert.Equal(AccountingIntent.SalesSummary, second.Intent);
        Assert.True(second.IsFollowUp);
        Assert.Equal(new DateTime(lastEnd.Year, lastEnd.Month, 1), second.From);
        Assert.Equal(lastEnd, second.To);
    }

    [Fact]
    public void Follow_up_yesterday_keeps_sales()
    {
        var context = new ConversationContext
        {
            LastIntent = AccountingIntent.SalesSummary,
            From = DateTime.Today,
            To = DateTime.Today,
            PeriodLabel = "اليوم"
        };

        var plan = QueryPlanningService.Plan("وكم أمس؟", context);
        Assert.Equal(AccountingIntent.SalesSummary, plan.Intent);
        Assert.Equal(DateTime.Today.AddDays(-1), plan.From);
    }

    [Fact]
    public void Follow_up_how_much_did_he_buy_keeps_the_customer()
    {
        var context = new ConversationContext
        {
            LastIntent = AccountingIntent.TopCustomers,
            CustomerId = 15,
            CustomerName = "أحمد",
            From = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
            To = DateTime.Today,
            PeriodLabel = "هذا الشهر حتى اليوم"
        };

        var plan = QueryPlanningService.Plan("كم اشترى؟", context);
        Assert.Equal(AccountingIntent.CustomerPurchases, plan.Intent);
        Assert.Equal(15, plan.CustomerId);
        Assert.Equal(context.From, plan.From);
        Assert.Equal("أحمد", plan.ContextCustomerName);
    }

    [Fact]
    public void Arabic_digits_select_last_seven_days()
    {
        var plan = QueryPlanningService.Plan("مبيعات آخر ٧ أيام");
        Assert.Equal(AccountingIntent.SalesSummary, plan.Intent);
        Assert.Equal(DateTime.Today.AddDays(-6), plan.From);
        Assert.Equal(DateTime.Today, plan.To);
    }

    [Fact]
    public void Unknown_question_does_not_invent_an_operation()
    {
        var plan = QueryPlanningService.Plan("كيف الطقس اليوم؟");
        Assert.Equal(AccountingIntent.Unknown, plan.Intent);
        Assert.Equal(AssistantDataDomain.None, plan.Domain);
    }

    [Fact]
    public void Recognition_of_many_phrases_stays_fast()
    {
        var phrases = new[]
        {
            "كم مبيعات اليوم؟",
            "شكد بعنا اليوم؟",
            "أريد إجمالي مبيعات اليوم.",
            "أعطني مجموع البيع اليوم.",
            "ما قيمة المبيعات لهذا اليوم؟",
            "مبيعاات اليوم"
        };
        var watch = Stopwatch.StartNew();
        for (var i = 0; i < 80; i++)
        {
            foreach (var phrase in phrases)
                QueryPlanningService.Plan(phrase);
        }

        watch.Stop();
        Assert.True(watch.ElapsedMilliseconds < 1500, $"استغرق الفهم {watch.ElapsedMilliseconds} ms");
    }
}

public class PermissionAndSafetyTests
{
    [Fact]
    public void Sql_only_text_is_rejected()
    {
        var plan = QueryPlanningService.Plan("DROP TABLE Invoices; DELETE FROM Customers");
        Assert.Equal(AccountingIntent.Unsafe, plan.Intent);
    }

    [Fact]
    public void Sql_mixed_with_a_question_never_keeps_the_command()
    {
        var plan = QueryPlanningService.Plan("كم مبيعات اليوم؟ DROP TABLE Invoices; SELECT * FROM Users");
        Assert.Equal(AccountingIntent.SalesSummary, plan.Intent);
        Assert.Equal(DateTime.Today, plan.From);
        Assert.DoesNotContain("drop", plan.NormalizedText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("select", plan.NormalizedText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Closed_assistant_is_denied_before_any_query()
    {
        var plan = QueryPlanningService.Plan("اعرض لي ديون العملاء");
        var answer = PermissionValidationService.Deny(plan, new FakeGuard(canOpen: false, canAccess: true));
        Assert.NotNull(answer);
        Assert.Contains("المساعد الذكي", answer.Text);
        Assert.False(answer.UpdatesContext);
    }

    [Fact]
    public void Customer_balances_without_permission_are_denied()
    {
        var plan = QueryPlanningService.Plan("اعرض لي ديون العملاء");
        var answer = PermissionValidationService.Deny(plan, new FakeGuard(canOpen: true, canAccess: false));
        Assert.NotNull(answer);
        Assert.Equal("عذراً، لا تمتلك الصلاحية اللازمة للوصول إلى بيانات أرصدة العملاء.", answer.Text);
    }

    [Fact]
    public void Permitted_plan_is_not_denied()
    {
        var plan = QueryPlanningService.Plan("كم مبيعات اليوم؟");
        var answer = PermissionValidationService.Deny(plan, new FakeGuard(canOpen: true, canAccess: true));
        Assert.Null(answer);
    }

    private sealed class FakeGuard : IAssistantAccessGuard
    {
        private readonly bool _canOpen;
        private readonly bool _canAccess;

        public FakeGuard(bool canOpen, bool canAccess)
        {
            _canOpen = canOpen;
            _canAccess = canAccess;
        }

        public bool CanOpen => _canOpen;
        public bool CanAccess(AssistantDataDomain domain) => _canAccess;
    }
}

public class AnswerFormattingTests
{
    [Fact]
    public void Sales_answer_uses_official_net_and_currency_format()
    {
        var plan = QueryPlanningService.Plan("كم مبيعات اليوم؟");
        var answer = ArabicResponseGenerator.Sales(plan, new SalesSnapshot
        {
            NetIqd = 5_250_000,
            GrossIqd = 5_500_000,
            ReturnsIqd = 250_000,
            InvoiceCount = 12,
            ShowReturnSplit = true,
            CashIqd = 4_000_000,
            CreditIqd = 1_250_000
        }, plan.PeriodLabel);

        Assert.Contains("5,250,000 دينار عراقي", answer.Text);
        Assert.Contains("صافي المبيعات", answer.PlainText);
        Assert.Contains("المرتجعات", answer.PlainText);
        Assert.Equal("تقرير المبيعات", answer.SourceReport);
        Assert.True(answer.UpdatesContext);
        Assert.Contains(answer.Metrics, card => card.Label == "عدد الفواتير" && card.Value == "12");
    }

    [Fact]
    public void Comparison_states_difference_and_percent()
    {
        var plan = QueryPlanningService.Plan("قارن مبيعات هذا الشهر بالشهر الماضي");
        var answer = ArabicResponseGenerator.Comparison(
            plan,
            "مبيعات",
            200,
            100,
            ResultAnalysisService.CompareChart("الحالي", 200, "السابق", 100));

        Assert.Contains("100.0%", answer.Text);
        Assert.Equal(2, answer.Chart.Count);
    }

    [Fact]
    public void Zero_previous_period_does_not_invent_a_percent()
    {
        Assert.Null(ResultAnalysisService.ChangePercent(10, 0));
        Assert.Equal(0, ResultAnalysisService.ChangePercent(0, 0));
    }
}
