using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CloudflaredMonitor.Services
{
    /// <summary>
    ///  Calls the Cloudflare Zero Trust API.
    ///  The bearer token is passed in at construction time and held only in
    ///  memory for the lifetime of this instance.  It is never written to
    ///  disk or the registry.
    /// </summary>
    internal sealed class CloudflareApi
    {
        private readonly HttpClient _client;
        private readonly string _bearerToken;

        /// <param name="bearerToken">
        ///  Cloudflare API token without the "Bearer" prefix.
        ///  Obtain from LastPass; do not store on disk.
        /// </param>
        public CloudflareApi(string bearerToken)
        {
            _bearerToken = bearerToken;
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

        /// <summary>
        ///  Verify the token is valid by calling the tunnel endpoint.
        ///  Returns null on success, or an error message on failure.
        /// </summary>
        public async Task<string?> TestTokenAsync(string tunnelId, CancellationToken ct)
        {
            try
            {
                await GetTunnelAsync(tunnelId, ct);
                return null; // success
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private async Task<T?> SendAsync<T>(string relativeUrl, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
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