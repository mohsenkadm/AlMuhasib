using Moq;
using Qaid.TelegramBot.Application.Abstractions;
using Qaid.TelegramBot.Application.Bot;
using Qaid.TelegramBot.Application.Models;
using Qaid.TelegramBot.Application.Reports;
using RichardSzalay.MockHttp;

namespace Qaid.TelegramBot.Tests;

public class FormatterAndErrorTests
{
    [Fact]
    public void Formatter_uses_api_numbers_only_no_invention()
    {
        var result = new ProfitAndLossReportResult
        {
            TotalSales = 1000,
            CostOfGoodsSold = 400,
            GrossProfit = 600,
            TotalExpenses = 150,
            OperatingProfit = 450,
            NetProfit = 450,
            GrossMarginPercent = 60,
            NetMarginPercent = 45
        };

        var text = ArabicReportFormatter.FormatProfitAndLoss(result, "اليوم");
        Assert.Contains(ArabicReportFormatter.FormatMoney(1000), text);
        Assert.Contains(ArabicReportFormatter.FormatMoney(450), text);
        Assert.DoesNotContain("9999", text);
        Assert.DoesNotContain("اختلاق", text);
    }

    [Fact]
    public void Empty_rows_still_show_totals_from_api()
    {
        var result = new DailySalesReportResult
        {
            TotalSales = 0,
            InvoiceCount = 0,
            DayCount = 0,
            AverageDaily = 0,
            Rows = []
        };
        var text = ArabicReportFormatter.FormatDailySales(result, "اليوم");
        Assert.Contains("إجمالي المبيعات", text);
        Assert.Contains(ArabicReportFormatter.FormatMoney(0), text);
        Assert.DoesNotContain("أحدث السجلات", text);
    }

    [Fact]
    public async Task Network_errors_return_safe_arabic_message()
    {
        var (db, _, links) = TestHelpers.CreateLinkStack();
        await using var dbScope = db;
        var created = await links.CreateLinkCodeAsync(TestHelpers.SampleCodeRequest(), CancellationToken.None);
        await links.ConsumeLinkCodeAsync(created.PlainCode, 33, CancellationToken.None);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, "https://api.test/api/reports/expenses*")
            .Throw(new HttpRequestException("connection reset"));
        var api = TestHelpers.CreateApiClient(mockHttp);

        var sent = new List<string>();
        var bot = TestHelpers.CreateBotMock(sent);
        var processor = TestHelpers.CreateProcessor(links, api, bot.Object);

        await processor.ProcessCallbackAsync(
            33, 33, 1, "cq", CallbackData.Period(ReportKind.Expenses, ReportPeriod.Today), CancellationToken.None);

        Assert.Contains(sent, t => t.Contains("تعذّر"));
        Assert.DoesNotContain(sent, t => t.Contains("connection reset"));
        Assert.DoesNotContain(sent, t => t.Contains("StackTrace"));
    }

    [Fact]
    public async Task Api_empty_body_handled_without_crash()
    {
        var (db, _, links) = TestHelpers.CreateLinkStack();
        await using var dbScope = db;
        var created = await links.CreateLinkCodeAsync(TestHelpers.SampleCodeRequest(), CancellationToken.None);
        await links.ConsumeLinkCodeAsync(created.PlainCode, 44, CancellationToken.None);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, "https://api.test/api/reports/cash-flow*")
            .Respond(System.Net.HttpStatusCode.OK, "application/json", "null");
        var api = TestHelpers.CreateApiClient(mockHttp);

        var sent = new List<string>();
        var processor = TestHelpers.CreateProcessor(links, api, TestHelpers.CreateBotMock(sent).Object);

        await processor.ProcessCallbackAsync(
            44, 44, 1, "cq", CallbackData.Period(ReportKind.CashFlow, ReportPeriod.ThisMonth), CancellationToken.None);

        Assert.Contains(sent, t => t.Contains("تعذّر"));
    }

    [Fact]
    public void Period_helper_resolves_today_bounds()
    {
        var today = new DateTime(2026, 10, 9);
        var range = ReportPeriodHelper.Resolve(ReportPeriod.Today, today);
        Assert.Equal(today, range.From);
        Assert.Equal("اليوم", range.Label);
    }
}
