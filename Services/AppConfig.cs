namespace OolioTunnelMonitor.Services
{
    /// <summary>
    ///  Centralised configuration.  The Cloudflare API token is NOT stored
    ///  here or anywhere on the machine.  Technicians paste it from LastPass
    ///  into the Repair dialog when needed; it lives only in memory for the
    ///  duration of that single repair operation.
    /// </summary>
    internal static class AppConfig
    {
        /// <summary>Cloudflare Account ID.</summary>
        public const string AccountId = "2a5f7d34c1a1e18d2dfffe551c418915";

        /// <summary>URL for the latest cloudflared MSI.</summary>
        public const string CloudflaredMsiUrl =
            "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.msi";

        /// <summary>Windows Service name for cloudflared.</summary>
        public const string ServiceName = "Cloudflared";

        /// <summary>How long to wait for the service to stop gracefully.</summary>
        public static readonly System.TimeSpan StopTimeout = System.TimeSpan.FromSeconds(15);
    }
}
