using Moq;
using Qaid.TelegramBot.Application.Abstractions;
using Qaid.TelegramBot.Application.Bot;
using Qaid.TelegramBot.Application.Models;
using RichardSzalay.MockHttp;

namespace Qaid.TelegramBot.Tests;

public class BotSecurityFlowTests
{
    [Fact]
    public async Task Unlinked_user_cannot_view_reports()
    {
        var (db, _, links) = TestHelpers.CreateLinkStack();
        await using var dbScope = db;
        var sent = new List<string>();
        var bot = TestHelpers.CreateBotMock(sent);
        var api = new Mock<IQaidApiClient>(MockBehavior.Strict);
        var processor = TestHelpers.CreateProcessor(links, api.Object, bot.Object);

        await processor.ProcessCallbackAsync(
            telegramUserId: 9,
            chatId: 9,
            messageId: 1,
            callbackQueryId: "cq1",
            data: CallbackData.Report(ReportKind.DailySales),
            CancellationToken.None);

        Assert.Contains(sent, t => t.Contains("غير مربوط"));
        api.VerifyNoOtherCalls();
    }

    [Fact]
    public void Callback_data_cannot_inject_tenant_or_change_company()
    {
        Assert.False(CallbackData.TryParse("r:0:tenant=99", out _));
        Assert.False(CallbackData.TryParse("tenantId=5", out _));
        Assert.True(CallbackData.TryParse(CallbackData.Period(ReportKind.Expenses, ReportPeriod.Today), out var action));
        Assert.Equal(CallbackActionType.RunReport, action.Type);
        Assert.Equal(ReportKind.Expenses, action.Kind);
        Assert.Equal(ReportPeriod.Today, action.Period);
    }

    [Fact]
    public async Task Expired_session_does_not_bypass_auth()
    {
        var (db, _, links) = TestHelpers.CreateLinkStack();
        await using var dbScope = db;
        var created = await links.CreateLinkCodeAsync(TestHelpers.SampleCodeRequest(), CancellationToken.None);
        await links.ConsumeLinkCodeAsync(created.PlainCode, 77, CancellationToken.None);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "https://api.test/api/auth/refresh")
            .Respond(System.Net.HttpStatusCode.Unauthorized);
        var api = TestHelpers.CreateApiClient(mockHttp);

        // Force expired access token
        var link = await links.GetActiveLinkAsync(77, CancellationToken.None);
        Assert.NotNull(link);
        await links.UpdateTokensAsync(77, new StoredSessionTokens
        {
            AccessToken = "old",
            RefreshToken = "bad-refresh",
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(-5)
        }, CancellationToken.None);

        var sent = new List<string>();
        var bot = TestHelpers.CreateBotMock(sent);
        var processor = TestHelpers.CreateProcessor(links, api, bot.Object);

        await processor.ProcessCallbackAsync(
            77, 77, 3, "cq", CallbackData.Period(ReportKind.DailySales, ReportPeriod.Today), CancellationToken.None);

        Assert.Contains(sent, t => t.Contains("انتهت") || t.Contains("أعد الربط") || t.Contains("جلسة"));
        Assert.Null(await links.GetActiveLinkAsync(77, CancellationToken.None));
    }

    [Fact]
    public async Task Unlink_prevents_new_queries()
    {
        var (db, _, links) = TestHelpers.CreateLinkStack();
        await using var dbScope = db;
        var created = await links.CreateLinkCodeAsync(TestHelpers.SampleCodeRequest(), CancellationToken.None);
        await links.ConsumeLinkCodeAsync(created.PlainCode, 88, CancellationToken.None);

        var sent = new List<string>();
        var bot = TestHelpers.CreateBotMock(sent);
        var api = new Mock<IQaidApiClient>(MockBehavior.Strict);
        var processor = TestHelpers.CreateProcessor(links, api.Object, bot.Object);

        await processor.ProcessCallbackAsync(88, 88, 1, "cq", CallbackData.UnlinkConfirm, CancellationToken.None);
        sent.Clear();

        await processor.ProcessCallbackAsync(
            88, 88, 2, "cq2", CallbackData.Report(ReportKind.CashBalances), CancellationToken.None);

        Assert.Contains(sent, t => t.Contains("غير مربوط"));
        api.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Concurrent_users_do_not_mix_tokens_or_results()
    {
        // Separate link stores avoid EF DbContext thread-affinity; isolation under test is token/result mixing.
        var (dbA, _, linksA) = TestHelpers.CreateLinkStack();
        var (dbB, _, linksB) = TestHelpers.CreateLinkStack();
        await using var scopeA = dbA;
        await using var scopeB = dbB;

        var codeA = await linksA.CreateLinkCodeAsync(TestHelpers.SampleCodeRequest(tenantId: 1), CancellationToken.None);
        var codeB = await linksB.CreateLinkCodeAsync(TestHelpers.SampleCodeRequest(tenantId: 2), CancellationToken.None);
        await linksA.ConsumeLinkCodeAsync(codeA.PlainCode, 100, CancellationToken.None);
        await linksB.ConsumeLinkCodeAsync(codeB.PlainCode, 200, CancellationToken.None);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, "https://api.test/api/reports/cash-balances-summary")
            .Respond(req =>
            {
                var token = req.Headers.Authorization!.Parameter!;
                var total = token.EndsWith("-1", StringComparison.Ordinal) ? 111 : 222;
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent($"{{\"cashBoxesTotal\":{total},\"banksTotal\":0,\"totalLiquid\":{total},\"accountCount\":1,\"rows\":[]}}")
                };
            });
        mockHttp.When(HttpMethod.Get, "https://api.test/api/sync/status")
            .Respond("application/json", """{"lastSyncAt":null,"pendingPushCount":0,"isLicensed":true}""");

        var api = TestHelpers.CreateApiClient(mockHttp);
        var sentA = new List<string>();
        var sentB = new List<string>();
        var pA = TestHelpers.CreateProcessor(linksA, api, TestHelpers.CreateBotMock(sentA).Object);
        var pB = TestHelpers.CreateProcessor(linksB, api, TestHelpers.CreateBotMock(sentB).Object);

        await Task.WhenAll(
            pA.ProcessCallbackAsync(100, 100, 1, "a", CallbackData.Report(ReportKind.CashBalances), CancellationToken.None),
            pB.ProcessCallbackAsync(200, 200, 1, "b", CallbackData.Report(ReportKind.CashBalances), CancellationToken.None));

        Assert.Contains(sentA, t => t.Contains("111"));
        Assert.Contains(sentB, t => t.Contains("222"));
        Assert.DoesNotContain(sentA, t => t.Contains("222"));
        Assert.DoesNotContain(sentB, t => t.Contains("111"));
    }
}
