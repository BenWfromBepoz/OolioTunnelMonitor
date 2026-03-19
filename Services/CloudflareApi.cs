using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
                Timeout     = TimeSpan.FromSeconds(30)
            };
        }

        public Task<CfTunnel?> GetTunnelAsync(string tunnelId, CancellationToken ct)
            => SendAsync<CfTunnel>("accounts/" + AppConfig.AccountId + "/cfd_tunnel/" + tunnelId, ct);

        public Task<CfTunnelConfigWrapper?> GetTunnelConfigAsync(string tunnelId, CancellationToken ct)
            => SendAsync<CfTunnelConfigWrapper>("accounts/" + AppConfig.AccountId + "/cfd_tunnel/" + tunnelId + "/configurations", ct);

        // Cloudflare returns result as a plain string token, not a nested object.
        // We deserialise as string directly to avoid the CfTunnelTokenResult conversion error.
        public async Task<string?> GetTunnelTokenAsync(string tunnelId, CancellationToken ct)
        {
            var url = "accounts/" + AppConfig.AccountId + "/cfd_tunnel/" + tunnelId + "/token";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
            var response = await _client.SendAsync(req, ct);
            string json  = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    "Cloudflare API " + (int)response.StatusCode + ": " + json);
            // Parse as generic document - result may be a string or object
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("success", out var successProp) || !successProp.GetBoolean())
                throw new InvalidOperationException("Cloudflare API error: " + json);
            if (!root.TryGetProperty("result", out var resultProp))
                return null;
            // Handle both formats: result is a plain string OR result is { "token": "..." }
            if (resultProp.ValueKind == JsonValueKind.String)
                return resultProp.GetString();
            if (resultProp.ValueKind == JsonValueKind.Object &&
                resultProp.TryGetProperty("token", out var tokenProp))
                return tokenProp.GetString();
            return null;
        }

        // Token verification via /user/tokens/verify
        public Task<CfTokenVerifyResult?> VerifyTokenAsync(CancellationToken ct)
            => SendAsync<CfTokenVerifyResult>("user/tokens/verify", ct);

        // Create a new named tunnel
        public async Task<CfTunnel?> CreateTunnelAsync(string name, CancellationToken ct)
        {
            var body = JsonSerializer.Serialize(new { name, config_src = "cloudflare" });
            return await PostAsync<CfTunnel>("accounts/" + AppConfig.AccountId + "/cfd_tunnel", body, ct);
        }

        // Push ingress config (replaces existing)
        public async Task PutTunnelConfigAsync(string tunnelId, List<CfIngressRule> ingressRules, CancellationToken ct)
        {
            var rules = new List<CfIngressRule>(ingressRules)
            {
                new CfIngressRule { Service = "http_status:404" }
            };
            var payload = new { config = new { ingress = rules } };
            var opts    = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
            await PutAsync("accounts/" + AppConfig.AccountId + "/cfd_tunnel/" + tunnelId + "/configurations",
                           JsonSerializer.Serialize(payload, opts), ct);
        }

        private async Task<T?> SendAsync<T>(string relativeUrl, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
            var response = await _client.SendAsync(req, ct);
            string json  = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    "Cloudflare API " + (int)response.StatusCode + ": " + json);
            var parsed = JsonSerializer.Deserialize<CfApiResponse<T>>(json);
            if (parsed is null || !parsed.Success || parsed.Result is null)
                throw new InvalidOperationException("Cloudflare API returned an error: " + json);
            return parsed.Result;
        }

        private async Task<T?> PostAsync<T>(string relativeUrl, string jsonBody, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, relativeUrl);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
            req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var response = await _client.SendAsync(req, ct);
            string json  = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    "Cloudflare API " + (int)response.StatusCode + ": " + json);
            var parsed = JsonSerializer.Deserialize<CfApiResponse<T>>(json);
            if (parsed is null || !parsed.Success || parsed.Result is null)
                throw new InvalidOperationException("Cloudflare API error: " + json);
            return parsed.Result;
        }

        private async Task PutAsync(string relativeUrl, string jsonBody, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Put, relativeUrl);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
            req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var response = await _client.SendAsync(req, ct);
            string json  = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    "Cloudflare API " + (int)response.StatusCode + ": " + json);
        }
    }
}
