namespace CloudflaredMonitor.Services
{
    /// <summary>
    ///  Centralised configuration for the application.
    ///  The API token is NO LONGER stored here.  It is provisioned once per
    ///  machine by running the bundled Provision-Token.ps1 script (or the
    ///  ProvisionToken helper in this assembly) which encrypts the token with
    ///  Windows DPAPI (LocalMachine scope) and writes it to a protected file
    ///  under ProgramData.  Only accounts with local-machine-level access can
    ///  decrypt it, so the plaintext never appears in source control.
    /// </summary>
    internal static class AppConfig
    {
        /// <summary>Cloudflare Account ID.</summary>
        public const string AccountId = "2a5f7d34c1a1e18d2dfffe551c418915";

        /// <summary>
        ///  Path to the DPAPI-protected token file written by the provisioning
        ///  step.  An administrator must run Provision-Token.ps1 (or call
        ///  TokenStore.Provision) on each target machine before the app can
        ///  reach the Cloudflare API.
        /// </summary>
        public static readonly string TokenFilePath = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData),
            "Bepoz", "CloudflaredMonitor", "token.dat");

        /// <summary>URL for the latest cloudflared MSI.</summary>
        public const string CloudflaredMsiUrl =
            "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.msi";

        /// <summary>Windows Service name for cloudflared.</summary>
        public const string ServiceName = "Cloudflared";

        /// <summary>How long to wait for the service to stop before killing it.</summary>
        public static readonly System.TimeSpan StopTimeout = System.TimeSpan.FromSeconds(15);
    }
}