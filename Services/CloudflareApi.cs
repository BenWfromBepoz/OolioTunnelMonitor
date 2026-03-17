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
    ///  implementation — no PowerShell dependency required.
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
            return await SendAsync<CfTunnel>(`accounts/${AppConfig.AccountId}/cfd_tunnel/${tunnelId}`, ct);
        }

        public async Task<CfTunnelConfigWrapper?> GetTunnelConfigAsync(string tunnelId, CancellationToken ct)
        {
            return await SendAsync<CfTunnelConfigWrapper>(`accounts/${AppConfig.AccountId}/cfd_tunnel/${tunnelId}/configurations`, ct);
        }

        public async Task<string?> GetTunnelTokenAsync(string tunnelId, CancellationToken ct)
        {
            var result = await SendAsync<CfTunnelTokenResult>(`accounts/${AppConfig.AccountId}/cfd_tunnel/${tunnelId}/token`, ct);
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
        ///
        ///  The format produced by PowerShell is a UTF-16LE string that, when the
        ///  bytes are decoded, yields a string like:
        ///    76492d1116743f04....|<base64-encoded-iv-and-ciphertext>
        ///
        ///  The portion before the pipe is a fixed magic header.
        ///  The portion after the pipe is Base64 containing:
        ///    bytes 0-15  : AES IV (16 bytes)
        ///    bytes 16+   : AES-256-CBC ciphertext
        ///  The decrypted plaintext is UTF-16LE (PowerShell native string encoding).
        /// </summary>
        private static string DecryptApiToken(string encrypted, byte[] key)
        {
            // The encrypted value is the raw string from ConvertFrom-SecureString.
            // Split on the pipe — left side is magic header, right side is Base64 payload.
            int pipeIndex = encrypted.IndexOf('|');
            if (pipeIndex < 0)
                throw new InvalidOperationException("Encrypted token is not in the expected PowerShell SecureString format (missing pipe separator).");

            string base64Payload = encrypted[(pipeIndex + 1)..];
            byte[] payload = Convert.FromBase64String(base64Payload);

            // First 16 bytes are the IV, remainder is the ciphertext.
            const int IvSize = 16;
            if (payload.Length < IvSize + 1)
                throw new InvalidOperationException("Encrypted token payload is too short.");

            byte[] iv         = payload[..IvSize];
            byte[] ciphertext = payload[IvSize..];

            using var aes = Aes.Create();
            aes.Key     = key;
            aes.IV      = iv;
            aes.Mode    = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            byte[] plainBytes   = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

            // Plaintext is UTF-16LE (PowerShell's native string encoding).
            return Encoding.Unicode.GetString(plainBytes).TrimEnd('\0');
        }
    }
}