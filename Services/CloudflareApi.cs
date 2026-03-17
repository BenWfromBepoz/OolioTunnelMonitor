using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CloudflaredMonitor.Services
{
    /// <summary>
    ///  Encapsulates calls to the Cloudflare Zero Trust API for retrieving
    ///  tunnel details, tunnel configuration and refreshing tunnel tokens.
    ///  The API token is stored as a PowerShell ConvertFrom-SecureString
    ///  export and is decrypted at runtime using a pure C# AES-256 CBC
    ///  implementation - no PowerShell dependency required.
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
        {
            return await SendAsync<CfTunnel>($"accounts/{AppConfig.AccountId}/cfd_tunnel/{tunnelId}", ct);
        }

        public async Task<CfTunnelConfigWrapper?> GetTunnelConfigAsync(string tunnelId, CancellationToken ct)
        {
            return await SendAsync<CfTunnelConfigWrapper>($"accounts/{AppConfig.AccountId}/cfd_tunnel/{tunnelId}/configurations", ct);
        }

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
        ///  Decrypts a token produced by PowerShell ConvertFrom-SecureString -Key.
        ///  Format: &lt;magic header&gt;|&lt;Base64(IV[16] + ciphertext)&gt;
        ///  Plaintext is UTF-16LE (PowerShell native string encoding).
        ///  To re-encrypt a token run in PowerShell on a secure workstation:
        ///    $key    = [byte[]](11,92,33,54,76,21,44,87,91,62,17,203,44,56,78,19,22,89,120,45,65,11,98,74,31,44,58,73,92,10,44,61)
        ///    $secure = ConvertTo-SecureString "YOUR_TOKEN" -AsPlainText -Force
        ///    ConvertFrom-SecureString $secure -Key $key
        /// </summary>
        private static string DecryptApiToken(string encrypted, byte[] key)
        {
            // Format is: <header>|<base64payload>
            int pipeIndex = encrypted.IndexOf('|');
            if (pipeIndex < 0)
                throw new InvalidOperationException("Encrypted token is not in expected PowerShell SecureString format (missing pipe separator).");

            byte[] payload = Convert.FromBase64String(encrypted[(pipeIndex + 1)..]);

            // First 16 bytes = AES IV, remainder = ciphertext
            const int IvSize = 16;
            if (payload.Length < IvSize + 1)
                throw new InvalidOperationException("Encrypted token payload is too short.");

            byte[] iv         = payload[..IvSize];
            byte[] ciphertext = payload[IvSize..];

            using var aes   = Aes.Create();
            aes.Key         = key;
            aes.IV          = iv;
            aes.Mode        = CipherMode.CBC;
            aes.Padding     = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            byte[] plainBytes   = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

            return Encoding.Unicode.GetString(plainBytes).TrimEnd('\0');
        }
    }
}