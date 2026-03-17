using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CloudflaredMonitor.Services
{
    /// <summary>
    ///  Calls the Cloudflare Zero Trust API.  The Bearer token is loaded at
    ///  runtime from the DPAPI-protected store written by the provisioning step.
    /// </summary>
    internal sealed class CloudflareApi
    {
        private readonly HttpClient _client;

        public CloudflareApi()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri("https://api.cloudflare.com/client/v4/"),
                Timeout = TimeSpan.FromSeconds(20)
            };
        }

        public async Task<CfTunnel?> GetTunnelAsync(string tunnelId, CancellationToken ct)
            => await SendAsync<CfTunnel>($"accounts/{AppConfig.AccountId}/cfd_tunnel/{tunnelId}", ct);

        public async Task<CfTunnelConfigWrapper?> GetTunnelConfigAsync(string tunnelId, CancellationToken ct)
            => await SendAsync<CfTunnelConfigWrapper>($"accounts/{AppConfig.AccountId}/cfd_tunnel/{tunnelId}/configurations", ct);

        public async Task<string?> GetTunnelTokenAsync(string tunnelId, CancellationToken ct)
        {
            var result = await SendAsync<CfTunnelTokenResult>($"accounts/{AppConfig.AccountId}/cfd_tunnel/{tunnelId}/token", ct);
            return result?.Token;
        }

        private async Task<T?> SendAsync<T>(string relativeUrl, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
            // Load the DPAPI-protected token from disk on every call so that
            // a re-provisioned token is picked up without restarting the app.
            string token = TokenStore.Load();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.SendAsync(request, ct);
            string json = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Cloudflare API {(int)response.StatusCode}: {json}");
            var parsed = JsonSerializer.Deserialize<CfApiResponse<T>>(json);
            if (parsed is null || !parsed.Success || parsed.Result is null)
                throw new InvalidOperationException($"Cloudflare API returned an error: {json}");
            return parsed.Result;
        }
    }
}