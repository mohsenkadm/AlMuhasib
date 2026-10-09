using System.Net;
using System.Reflection;
using Qaid.TelegramBot.Application.Security;
using Qaid.TelegramBot.Infrastructure.Services;
using RichardSzalay.MockHttp;

namespace Qaid.TelegramBot.Tests;

public class AllowedEndpointsAndApiClientTests
{
    [Theory]
    [InlineData("GET", "api/reports/daily-sales", true)]
    [InlineData("GET", "api/reports/expenses", true)]
    [InlineData("GET", "api/sync/status", true)]
    [InlineData("POST", "api/auth/refresh", true)]
    [InlineData("POST", "api/sync/push", false)]
    [InlineData("POST", "api/sync/pull", false)]
    [InlineData("POST", "api/mobile/invoices", false)]
    [InlineData("DELETE", "api/reports/daily-sales", false)]
    public void Allowlist_matches_policy(string method, string path, bool allowed)
    {
        var httpMethod = method switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "DELETE" => HttpMethod.Delete,
            _ => new HttpMethod(method)
        };
        Assert.Equal(allowed, AllowedEndpoints.IsAllowed(httpMethod, path));
    }

    [Fact]
    public void Client_public_methods_only_target_allowlisted_paths()
    {
        // Structural guard: every report/auth path constant used by client is allowlisted.
        foreach (var path in AllowedEndpoints.AllAllowedGetPaths)
            Assert.True(AllowedEndpoints.IsAllowed(HttpMethod.Get, path));

        Assert.True(AllowedEndpoints.IsAllowed(HttpMethod.Post, AllowedEndpoints.Login));
        Assert.True(AllowedEndpoints.IsAllowed(HttpMethod.Post, AllowedEndpoints.Refresh));
        Assert.True(AllowedEndpoints.IsAllowed(HttpMethod.Post, AllowedEndpoints.SelectBranch));

        foreach (var forbidden in AllowedEndpoints.ForbiddenWriteExamples)
            Assert.False(AllowedEndpoints.IsAllowed(HttpMethod.Post, forbidden));
    }

    [Fact]
    public async Task Get_report_does_not_send_tenantId_query()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, "https://api.test/api/reports/daily-sales*")
            .Respond(async req =>
            {
                Assert.DoesNotContain("tenantId", req.RequestUri!.Query, StringComparison.OrdinalIgnoreCase);
                Assert.Equal("Bearer", req.Headers.Authorization?.Scheme);
                Assert.Equal("tok-a", req.Headers.Authorization?.Parameter);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"totalSales":12,"invoiceCount":1,"dayCount":1,"averageDaily":12,"rows":[]}""")
                };
            });

        var client = TestHelpers.CreateApiClient(mock);
        var result = await client.GetDailySalesAsync("tok-a", DateTime.Today, DateTime.Today, CancellationToken.None);
        Assert.Equal(12, result.TotalSales);
    }

    [Fact]
    public void EnsureAllowed_throws_for_write_endpoint()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AllowedEndpoints.EnsureAllowed(HttpMethod.Post, "api/sync/push"));
    }

    [Fact]
    public void QaidApiClient_has_no_write_invoice_methods()
    {
        var names = typeof(QaidApiClient).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToArray();
        Assert.DoesNotContain(names, n => n.Contains("Create", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Contains("Delete", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Contains("Push", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("GetDailySalesAsync", names);
    }

    [Fact]
    public async Task LoginAsync_deserializes_cloud_auth_shape()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, "https://api.test/api/auth/login")
            .Respond("application/json", """
                {
                  "accessToken":"tok",
                  "refreshToken":"ref",
                  "accessTokenExpiresAt":"2026-10-09T22:25:10.0391207Z",
                  "tenantId":11,
                  "companyName":"branch",
                  "tenantName":"branch",
                  "applicationSystemType":0,
                  "isMobileEnabled":true,
                  "allowedBranches":[{"branchId":10,"syncId":"00000000-0000-0000-0000-000000000001","name":"main","code":"M","isMain":true,"isDefault":true}],
                  "defaultBranchId":10,
                  "requiresBranchSelection":false,
                  "currentBranchId":10
                }
                """);

        var client = TestHelpers.CreateApiClient(mock);
        var login = await client.LoginAsync("test5", "test5", CancellationToken.None);
        Assert.Equal(11, login.TenantId);
        Assert.Equal("tok", login.AccessToken);
        Assert.Equal(10, login.CurrentBranchId);
        Assert.Single(login.AllowedBranches);
    }
}
