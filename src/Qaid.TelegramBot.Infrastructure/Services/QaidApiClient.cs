using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Qaid.TelegramBot.Application.Abstractions;
using Qaid.TelegramBot.Application.Models;
using Qaid.TelegramBot.Application.Security;

namespace Qaid.TelegramBot.Infrastructure.Services;

public sealed class QaidApiClient : IQaidApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly ILogger<QaidApiClient> _logger;

    public QaidApiClient(HttpClient http, ILogger<QaidApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public Task<TenantLoginResponse> LoginAsync(string username, string password, CancellationToken ct)
        => PostAsync<TenantLoginResponse>(
            AllowedEndpoints.Login,
            new TenantLoginRequest { Username = username, Password = password },
            accessToken: null,
            ct);

    public Task<SelectBranchResponse> SelectBranchAsync(string accessToken, int branchId, CancellationToken ct)
        => PostAsync<SelectBranchResponse>(
            AllowedEndpoints.SelectBranch,
            new SelectBranchRequest { BranchId = branchId, AllBranches = false },
            accessToken,
            ct);

    public Task<TenantLoginResponse> RefreshAsync(string refreshToken, CancellationToken ct)
        => PostAsync<TenantLoginResponse>(
            AllowedEndpoints.Refresh,
            new RefreshTokenRequest { RefreshToken = refreshToken },
            accessToken: null,
            ct);

    public Task<SyncStatusResponse> GetSyncStatusAsync(string accessToken, CancellationToken ct)
        => GetAsync<SyncStatusResponse>(AllowedEndpoints.SyncStatus, accessToken, query: null, ct);

    public Task<DailySalesReportResult> GetDailySalesAsync(string accessToken, DateTime? from, DateTime? to, CancellationToken ct)
        => GetAsync<DailySalesReportResult>(AllowedEndpoints.DailySales, accessToken, DateQuery(from, to), ct);

    public Task<ExpensesReportResult> GetExpensesAsync(string accessToken, DateTime? from, DateTime? to, CancellationToken ct)
        => GetAsync<ExpensesReportResult>(AllowedEndpoints.Expenses, accessToken, DateQuery(from, to), ct);

    public Task<ProfitAndLossReportResult> GetProfitAndLossAsync(string accessToken, DateTime? from, DateTime? to, CancellationToken ct)
        => GetAsync<ProfitAndLossReportResult>(AllowedEndpoints.ProfitAndLoss, accessToken, DateQuery(from, to), ct);

    public Task<CashBalancesSummaryReportResult> GetCashBalancesAsync(string accessToken, CancellationToken ct)
        => GetAsync<CashBalancesSummaryReportResult>(AllowedEndpoints.CashBalancesSummary, accessToken, query: null, ct);

    public Task<CashFlowResult> GetCashFlowAsync(string accessToken, DateTime? from, DateTime? to, CancellationToken ct)
        => GetAsync<CashFlowResult>(AllowedEndpoints.CashFlow, accessToken, DateQuery(from, to), ct);

    public Task<CustomersOverviewReportResult> GetCustomersOverviewAsync(string accessToken, DateTime? from, DateTime? to, CancellationToken ct)
        => GetAsync<CustomersOverviewReportResult>(AllowedEndpoints.CustomersOverview, accessToken, DateQuery(from, to), ct);

    private async Task<T> GetAsync<T>(string relativePath, string accessToken, string? query, CancellationToken ct)
    {
        AllowedEndpoints.EnsureAllowed(HttpMethod.Get, relativePath);
        var url = string.IsNullOrEmpty(query) ? relativePath : $"{relativePath}?{query}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await SendAsync<T>(request, ct);
    }

    private async Task<T> PostAsync<T>(string relativePath, object body, string? accessToken, CancellationToken ct)
    {
        AllowedEndpoints.EnsureAllowed(HttpMethod.Post, relativePath);
        using var request = new HttpRequestMessage(HttpMethod.Post, relativePath)
        {
            Content = JsonContent.Create(body)
        };
        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await SendAsync<T>(request, ct);
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken ct)
    {
        // Defense in depth: never allow TenantId query injection from callers.
        var requestUriText = request.RequestUri?.OriginalString ?? string.Empty;
        if (requestUriText.Contains("tenantId=", StringComparison.OrdinalIgnoreCase)
            || requestUriText.Contains("TenantId=", StringComparison.Ordinal))
            throw new InvalidOperationException("TenantId query parameters are not permitted.");

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (TaskCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            throw;
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Qaid API unauthorized.");

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Qaid API {Status} for {Path}", (int)response.StatusCode, request.RequestUri?.AbsolutePath);
            // Do not leak raw body to callers.
            _ = errorBody;
            throw new HttpRequestException($"Qaid API returned {(int)response.StatusCode}.");
        }

        var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        if (payload is null)
            throw new InvalidOperationException("Empty API response.");
        return payload;
    }

    private static string DateQuery(DateTime? from, DateTime? to)
    {
        var sb = new StringBuilder();
        if (from is not null)
            sb.Append("from=").Append(Uri.EscapeDataString(from.Value.ToString("o")));
        if (to is not null)
        {
            if (sb.Length > 0) sb.Append('&');
            sb.Append("to=").Append(Uri.EscapeDataString(to.Value.ToString("o")));
        }
        return sb.ToString();
    }
}
