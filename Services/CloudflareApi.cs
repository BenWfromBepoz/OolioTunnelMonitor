using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CloudflaredMonitor.Services
{
    /// <summary>
    ///  Encapsulates calls to the Cloudflare Zero Trust API for retrieving
    ///  tunnel details, tunnel configuration and refreshing tunnel tokens.
    ///  This class decrypts the encrypted API token at runtime via
    ///  PowerShell.  See README.md for details on how to generate the
    ///  encrypted token.
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

        /// <summary>
        ///  Retrieve details about a specific tunnel by its ID.
        /// </summary>
        public async Task<CfTunnel?> GetTunnelAsync(string tunnelId, CancellationToken ct)
        {
            return await SendAsync<CfTunnel>($"accounts/{AppConfig.AccountId}/cfd_tunnel/{tunnelId}", ct);
        }

        /// <summary>
        ///  Retrieve the remote configuration for a tunnel.  The config
        ///  includes ingress rules and other settings.  Note that this will
        ///  fail if the tunnel is locally managed via config.yml.
        /// </summary>
        public async Task<CfTunnelConfigWrapper?> GetTunnelConfigAsync(string tunnelId, CancellationToken ct)
        {
            return await SendAsync<CfTunnelConfigWrapper>($"accounts/{AppConfig.AccountId}/cfd_tunnel/{tunnelId}/configurations", ct);
        }

        /// <summary>
        ///  Request a fresh tunnel token for reinstalling a connector.  The
        ///  token should be passed to `cloudflared service install`.
        /// </summary>
        public async Task<string?> GetTunnelTokenAsync(string tunnelId, CancellationToken ct)
        {
            var result = await SendAsync<CfTunnelTokenResult>($"accounts/{AppConfig.AccountId}/cfd_tunnel/{tunnelId}/token", ct);
            return result?.Token;
        }

        private async Task<T?> SendAsync<T>(string relativeUrl, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
            string token = DecryptApiToken(AppConfig.EncryptedApiToken, AppConfig.Key);
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

        /// <summary>
        ///  Decrypts a PowerShell SecureString export using the same key used
        ///  during encryption.  See README.md for details on generating the
        ///  encrypted token.  This method shells out to powershell.exe to
        ///  leverage .NET's own implementation of SecureString encryption.
        ///  Because of this, powershell must be available on the host.
        /// </summary>
        private static string DecryptApiToken(string encrypted, byte[] key)
        {
            // Construct a comma separated list of bytes for the PowerShell key
            var keyList = string.Join(",", key);
            // Escape double quotes in the encrypted string for PowerShell
            var escapedEnc = encrypted.Replace("\"", "\"\"");
            // Build the inline PowerShell script.  Use single quoted strings where
            // possible and avoid newlines because the script will be passed via
            // the -Command argument.
            string script = "$key = [byte[]]({keyList}); " +
                            "$enc = \"{escapedEnc}\"; " +
                            "$secure = ConvertTo-SecureString $enc -Key $key; " +
                            "$ptr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure); " +
                            "try { [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($ptr) } finally { [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{script}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null)
                throw new InvalidOperationException("Failed to start PowerShell.");
            string output = process.StandardOutput.ReadToEnd().Trim();
            string error = process.StandardError.ReadToEnd().Trim();
            process.WaitForExit();
            if (process.ExitCode != 0 || string.IsNullOrEmpty(output))
                throw new InvalidOperationException($"PowerShell decryption failed: {error}");
            return output;
        }
    }
}