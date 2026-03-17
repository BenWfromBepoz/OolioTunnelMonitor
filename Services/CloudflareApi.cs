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
        ///  Decrypts a token that was encrypted with PowerShell's
        ///  ConvertFrom-SecureString using an explicit -Key (AES-256 CBC).
        ///
        ///  PowerShell's SecureString wire format is:
        ///    [header 20 bytes][IV 16 bytes][HMAC 32 bytes][ciphertext]
        ///  all encoded as a UTF-16LE hex string.
        ///
        ///  The plaintext is UTF-16LE; we convert it to a regular .NET string
        ///  before returning.
        ///
        ///  To encrypt a new token, run the following PowerShell on a secure
        ///  workstation (substitute your own token and key):
        ///
        ///    $token  = "YOUR_API_TOKEN"
        ///    $key    = [byte[]](11,92,33,54,76,21,44,87,91,62,17,203,44,56,78,19,
        ///                        22,89,120,45,65,11,98,74,31,44,58,73,92,10,44,61)
        ///    $secure = ConvertTo-SecureString $token -AsPlainText -Force
        ///    ConvertFrom-SecureString $secure -Key $key
        ///
        ///  Paste the output into AppConfig.EncryptedApiToken.
        /// </summary>
        private static string DecryptApiToken(string encrypted, byte[] key)
        {
            if (encrypted.Length % 2 != 0)
                throw new InvalidOperationException("Encrypted token has an odd number of hex characters.");

            byte[] blob = new byte[encrypted.Length / 2];
            for (int i = 0; i < blob.Length; i++)
                blob[i] = Convert.ToByte(encrypted.Substring(i * 2, 2), 16);

            // Decode the UTF-16LE blob to get the inner hex payload produced by
            // PowerShell's SecureString serialiser.
            string innerHex = Encoding.Unicode.GetString(blob);

            // The inner hex string encodes:
            //   bytes  0-19  : header (version + flags, ignored)
            //   bytes 20-35  : IV      (16 bytes, AES block size)
            //   bytes 36-67  : HMAC-SHA256 (32 bytes, not verified here)
            //   bytes 68-end : AES-256-CBC ciphertext
            byte[] inner = new byte[innerHex.Length / 2];
            for (int i = 0; i < inner.Length; i++)
                inner[i] = Convert.ToByte(innerHex.Substring(i * 2, 2), 16);

            const int HeaderSize = 20;
            const int IvSize     = 16;
            const int HmacSize   = 32;
            const int DataOffset = HeaderSize + IvSize + HmacSize; // 68

            if (inner.Length < DataOffset)
                throw new InvalidOperationException("Encrypted token blob is too short.");

            byte[] iv         = inner[HeaderSize..(HeaderSize + IvSize)];
            byte[] ciphertext = inner[DataOffset..];

            using var aes = Aes.Create();
            aes.Key     = key;
            aes.IV      = iv;
            aes.Mode    = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            byte[] plainBytes   = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

            // The decrypted bytes are the token encoded as UTF-16LE.
            return Encoding.Unicode.GetString(plainBytes).TrimEnd('\0');
        }
    }
}