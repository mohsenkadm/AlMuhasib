using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AlMuhasib.Core.Entities;
using AlMuhasib.Sync.Requests;
using AlMuhasib.Sync.Responses;

namespace AlMuhasib.Infrastructure.Services;

public sealed class SyncApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SyncApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<TenantLoginResponse> LoginAsync(CloudSyncSettings settings, CancellationToken ct)
    {
        var client = CreateClient(settings.ApiBaseUrl);
        var response = await client.PostAsJsonAsync("/api/auth/login", new TenantLoginRequest
        {
            Username = settings.Username,
            Password = settings.Password
        }, ct);

        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<TenantLoginResponse>(JsonOptions, ct))!;
    }

    public async Task<LicenseStatusResponse> GetLicenseStatusAsync(CloudSyncSettings settings, CancellationToken ct)
    {
        var client = CreateAuthorizedClient(settings);
        var response = await client.GetAsync("/api/auth/license-status", ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<LicenseStatusResponse>(JsonOptions, ct))!;
    }

    public async Task<SyncPushResponse> PushAsync(CloudSyncSettings settings, SyncPushRequest request, CancellationToken ct)
    {
        var client = CreateAuthorizedClient(settings);
        var response = await client.PostAsJsonAsync("/api/sync/push", request, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<SyncPushResponse>(JsonOptions, ct))!;
    }

    public async Task<SyncPullResponse> PullAsync(CloudSyncSettings settings, SyncPullRequest request, CancellationToken ct)
    {
        var client = CreateAuthorizedClient(settings);
        var response = await client.PostAsJsonAsync("/api/sync/pull", request, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<SyncPullResponse>(JsonOptions, ct))!;
    }

    private HttpClient CreateClient(string baseUrl)
    {
        var client = _httpClientFactory.CreateClient("CloudSync");
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        return client;
    }

    private HttpClient CreateAuthorizedClient(CloudSyncSettings settings)
    {
        var client = CreateClient(settings.ApiBaseUrl);
        if (!string.IsNullOrWhiteSpace(settings.AccessToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.AccessToken);
        return client;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(ct);
        ApiErrorResponse? error = null;
        try { error = JsonSerializer.Deserialize<ApiErrorResponse>(body, JsonOptions); } catch { }
        throw new HttpRequestException(error?.Message ?? $"API error {(int)response.StatusCode}: {body}");
    }
}
