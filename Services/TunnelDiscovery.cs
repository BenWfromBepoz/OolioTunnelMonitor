using Microsoft.Win32;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace CloudflaredMonitor.Services
{
    /// <summary>
    ///  Responsible for locating the installed cloudflared service, extracting
    ///  the installation token from its command line, and decoding that
    ///  token into the underlying tunnel ID.  This mirrors the discovery
    ///  logic used in the PowerShell script for the CLI version of this tool.
    /// </summary>
    internal static class TunnelDiscovery
    {
        /// <summary>
        ///  Reads the ImagePath of the installed service from the registry.
        /// </summary>
        public static string? TryGetServiceImagePath()
        {
            using var key = Registry.LocalMachine.OpenSubKey($"SYSTEM\\CurrentControlSet\\Services\\{AppConfig.ServiceName}");
            return key?.GetValue("ImagePath") as string;
        }

        /// <summary>
        ///  Extract the token from the service command line.  The token
        ///  typically appears after the '--token' argument.  If the token
        ///  cannot be found, null is returned.
        /// </summary>
        public static string? TryExtractTokenFromImagePath(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return null;
            int idx = imagePath.IndexOf("--token", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            string after = imagePath[(idx + "--token".Length)..].Trim();
            if (after.Length == 0) return null;
            var parts = after.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return null;
            string token = parts[0].Trim().Trim('"');
            return token;
        }

        /// <summary>
        ///  Decode the base64 token to extract the tunnel ID.  The token is a
        ///  JWT-like payload encoded as base64url.  The property "t" in the
        ///  decoded JSON corresponds to the tunnel ID.
        /// </summary>
        public static string? TryDecodeTunnelIdFromToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            try
            {
                string normalized = token.Replace('-', '+').Replace('_', '/');
                switch (normalized.Length % 4)
                {
                    case 2: normalized += "=="; break;
                    case 3: normalized += "="; break;
                }
                var bytes = Convert.FromBase64String(normalized);
                string json = Encoding.UTF8.GetString(bytes);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("t", out var tProp))
                    return tProp.GetString();
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}