using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CloudflaredMonitor.Services
{
    internal sealed class CloudflareApi
    {
        private readonly HttpClient _client;
        private readonly string _bearerToken;

        public CloudflareApi(string bearerToken)
        {
            _bearerToken = bearerToken;
            _client = new HttpClient
            {
                BaseAddress = new Uri("https://api.cloudflare.com/client/v4/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        // ── Existing read methods ─────────────────────────────────────────────────────

        public Task<CfTunnel?> GetTunnelAsync(string tunnelId, CancellationToken ct)
            => SendAsync<CfTunnel>($"accounts/{AppConfig.AccountId}/cfd_tunnel/{tunnelId}", ct);

        public Task<CfTunnelConfigWrapper?> GetTunnelConfigAsync(string tunnelId, CancellationToken ct)
            => SendAsync<CfTunnelConfigWrapper>($"accounts/{AppConfig.AccountId}/cfd_tunnel/{tunnelId}/configurations", ct);

        public async Task<string?> GetTunnelTokenAsync(string tunnelId, CancellationToken ct)
        {
            var result = await SendAsync<CfTunnelTokenResult>($"accounts/{AppConfig.AccountId}/cfd_tunnel/{tunnelId}/token", ct);
            return result?.Token;
        }

        public async Task<string?> TestTokenAsync(string tunnelId, CancellationToken ct)
        {
            try { await GetTunnelAsync(tunnelId, ct); return null; }
            catch (Exception ex) { return ex.Message; }
        }

        // ── Create tunnel ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new Cloudflare Tunnel and returns the created tunnel object.
        /// Requires Cloudflare Zero Trust Edit permission.
        /// </summary>
        public async Task<CfTunnel?> CreateTunnelAsync(string name, CancellationToken ct)
        {
            var body = JsonSerializer.Serialize(new { name, config_src = "cloudflare" });
            var result = await PostAsync<CfTunnel>(
                $"accounts/{AppConfig.AccountId}/cfd_tunnel",
                body, ct);
            return result;
        }

        /// <summary>
        /// Puts the full ingress configuration for a tunnel.
        /// Replaces any existing config with the provided ingress rules.
        /// </summary>
        public async Task PutTunnelConfigAsync(string tunnelId, System.Collections.Generic.List<CfIngressRule> ingressRules, CancellationToken ct)
        {
            // Always append the required catch-all rule at the end
            var rules = new System.Collections.Generic.List<CfIngressRule>(ingressRules)
            {
                new CfIngressRule { Service = "http_status:404" }
            };
            var payload = new { config = new { ingress = rules } };
            var body    = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            await PutAsync($"accounts/{AppConfig.AccountId}/cfd_tunnel/{tunnelId}/configurations", body, ct);
        }

        // ── HTTP helpers ─────────────────────────────────────────────────────────────────

        private async Task<T?> SendAsync<T>(string relativeUrl, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
            var response = await _client.SendAsync(request, ct);
            string json  = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Cloudflare API {(int)response.StatusCode}: {json}");
            var parsed = JsonSerializer.Deserialize<CfApiResponse<T>>(json);
            if (parsed is null || !parsed.Success || parsed.Result is null)
                throw new InvalidOperationException($"Cloudflare API returned an error: {json}");
            return parsed.Result;
        }

        private async Task<T?> PostAsync<T>(string relativeUrl, string jsonBody, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, relativeUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var response = await _client.SendAsync(request, ct);
            string json  = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Cloudflare API {(int)response.StatusCode}: {json}");
            var parsed = JsonSerializer.Deserialize<CfApiResponse<T>>(json);
            if (parsed is null || !parsed.Success || parsed.Result is null)
                throw new InvalidOperationException($"Cloudflare API error: {json}");
            return parsed.Result;
        }

        private async Task PutAsync(string relativeUrl, string jsonBody, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, relativeUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var response = await _client.SendAsync(request, ct);
            string json  = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Cloudflare API {(int)response.StatusCode}: {json}");
        }
    }
}
