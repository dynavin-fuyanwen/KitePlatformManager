using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace KitePlatformManager.Services;

public class KiteClient
{
    private readonly HttpClient _httpClient;

    public KiteClient(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("Kite");
    }

    public async IAsyncEnumerable<JsonElement> GetSubscriptionsAsync(string? lifeCycle = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var url = "subscriptions";
        if (!string.IsNullOrWhiteSpace(lifeCycle))
        {
            url += $"?lifeCycleStatus={Uri.EscapeDataString(lifeCycle)}";
        }

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
        if (doc != null && doc.RootElement.TryGetProperty("subscriptions", out var subs))
        {
            foreach (var sub in subs.EnumerateArray())
            {
                yield return sub;
            }
        }
    }

    public async Task ModifyLifecycleAsync(string icc, string targetStatus, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(new { lifeCycleStatus = targetStatus });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PutAsync($"subscriptions/{icc}/lifecycle", content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string?> SendSmsAsync(string icc, string message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(new { message });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync($"subscriptions/{icc}/sms", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
        return doc?.RootElement.GetProperty("watcherId").GetString();
    }

    public async Task<IReadOnlyList<JsonElement>> ListCommercialGroupsAsync(CancellationToken cancellationToken = default)
    {
        var url = "/services/REST/GlobalM2M/CommercialGroup/v5/r12/commercialGroup";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
        if (doc != null && doc.RootElement.TryGetProperty("groups", out var groups))
        {
            return groups.EnumerateArray().ToList();
        }
        return Array.Empty<JsonElement>();
    }

    public async Task<JsonElement?> GetSimDetailAsync(string icc, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"subscriptions/{icc}", cancellationToken);
        response.EnsureSuccessStatusCode();
        var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
        return doc?.RootElement;
    }

    public async IAsyncEnumerable<JsonElement> ListSimsAsync(int pageIndex, int pageSize, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var url = $"subscriptions?pageIndex={pageIndex}&pageSize={pageSize}";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
        if (doc != null && doc.RootElement.TryGetProperty("subscriptions", out var subs))
        {
            foreach (var sub in subs.EnumerateArray())
            {
                yield return sub;
            }
        }
    }
}
