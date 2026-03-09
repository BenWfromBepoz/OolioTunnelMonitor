namespace CloudflaredMonitor.Services
{
    /// <summary>
    ///  Centralized configuration for the application.  These values define the
    ///  Cloudflare account details, token encryption and MSI sources.  If you
    ///  need to change the Cloudflare account, API token or service name,
    ///  update this file only.
    /// </summary>
    internal static class AppConfig
    {
        /// <summary>
        ///  Cloudflare Account ID.  This must match the account under which
        ///  your tunnels are created.
        /// </summary>
        public const string AccountId = "2a5f7d34c1a1e18d2dfffe551c418915";

        /// <summary>
        ///  The symmetric key used to encrypt and decrypt the Cloudflare API
        ///  token.  This is a 32‑byte array (AES‑256) extracted from the
        ///  PowerShell SecureString.  You should generate your own key when
        ///  producing a new encrypted token.  The key must remain secret and
        ///  must match the one used when encrypting the token.
        /// </summary>
        public static readonly byte[] Key = new byte[]
        {
            11, 92, 33, 54, 76, 21, 44, 87,
            91, 62, 17, 203, 44, 56, 78, 19,
            22, 89, 120, 45, 65, 11, 98, 74,
            31, 44, 58, 73, 92, 10, 44, 61
        };

        /// <summary>
        ///  The encrypted API token string produced by ConvertFrom‑SecureString -Key in
        ///  PowerShell.  Only the token value should be encrypted, without the
        ///  "Bearer" prefix.  To encrypt a new token, run the following in
        ///  PowerShell (with $token containing your API token text):
        ///  
        ///  $secure = ConvertTo-SecureString $token -AsPlainText -Force
        ///  ConvertFrom-SecureString $secure -Key $key
        /// </summary>
        public const string EncryptedApiToken =
            "76492d1116743f0423413b16050a5345MgB8AHIARQBnADcAcQBoAE4AeABpAEkAeABTAE4AdAA5AGIAagA5AEsAagBDAFEAPQA9AHwAYgAyADEAZQAzADAANQA1AGMAMwAwADUAMgBiADAAYgA3ADUANABmADIAOABjADIAYgBhADEANgA4ADAANQAzAGEAOAAzADYAMgA5AGUAZABmADIAMgBhADMAZgBmADkANQA5AGQAYgAwADMAYwBiADkANwA2AGYAZgBmADYAOAA1ADgAMQBkADcAMgAzADQANwA0ADYAZAA4ADUANQA0AGEAYQA0ADYANQAxADkAMAAxADgANwA4ADEANABlADAAOQBlADcAZgBiADgAYwA4ADAAZQA3ADcAZAA2ADYAYwA0ADAAYwA2ADEAYQA1ADIAYgA5AGMAZgAxAGMAMQA3AGUAMgA0ADMAMwBkADcANAAxAGEAMQBmADEAYgA5AGYAMwBiAGMAYgA3ADUAOABiADgAYwBiADAAZQBjADQAYgA3AGYAMQBhADgAMgAyADMAMgBkADQANwA2AGUAMQA0ADMANQA3ADIAMQBiAGMAYgAzAGYAOAAxADcAMQAzADYA";

        /// <summary>
        ///  URL pointing to the latest cloudflared MSI.  The repair routine
        ///  downloads this installer when reinstalling the service.  If you
        ///  deploy your own private build of cloudflared, update this URL.
        /// </summary>
        public const string CloudflaredMsiUrl =
            "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.msi";

        /// <summary>
        ///  Name of the Windows Service used by cloudflared.  You should not
        ///  change this unless you have customised the service name during
        ///  installation.
        /// </summary>
        public const string ServiceName = "Cloudflared";

        /// <summary>
        ///  How long to wait while stopping the service before killing the
        ///  process.  Tune this value if you find your service takes longer
        ///  than 15 seconds to stop gracefully.
        /// </summary>
        public static readonly System.TimeSpan StopTimeout = System.TimeSpan.FromSeconds(15);
    }
}