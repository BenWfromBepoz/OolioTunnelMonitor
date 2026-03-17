using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CloudflaredMonitor.Services
{
    /// <summary>
    ///  Stores and retrieves the Cloudflare API bearer token using Windows
    ///  DPAPI with LocalMachine scope.
    ///  
    ///  LocalMachine scope means:
    ///    - Any process running on the same machine can decrypt the token.
    ///    - The encrypted bytes are useless on any other machine.
    ///    - The plaintext token never appears in source control or config files.
    ///  
    ///  PROVISIONING (run once per machine as Administrator):
    ///  
    ///    Option A - PowerShell one-liner:
    ///      $token = "fDh15F3jC23mCbZWkY1WWMHIxKA7_-UfXdTPcH5H"
    ///      $bytes = [System.Text.Encoding]::UTF8.GetBytes($token)
    ///      $enc   = [System.Security.Cryptography.ProtectedData]::Protect(
    ///                   $bytes, $null,
    ///                   [System.Security.Cryptography.DataProtectionScope]::LocalMachine)
    ///      $dir   = "C:\ProgramData\Bepoz\CloudflaredMonitor"
    ///      New-Item -ItemType Directory -Force $dir | Out-Null
    ///      [IO.File]::WriteAllBytes("$dir\token.dat", $enc)
    ///  
    ///    Option B - call TokenStore.Provision(plainTextToken) from a
    ///      one-time admin setup tool or the repair wizard.
    /// </summary>
    internal static class TokenStore
    {
        /// <summary>
        ///  Encrypt <paramref name="plainToken"/> with DPAPI and write it to
        ///  the token file.  Must be called from an elevated process.
        /// </summary>
        public static void Provision(string plainToken)
        {
            byte[] plainBytes     = Encoding.UTF8.GetBytes(plainToken);
            byte[] encryptedBytes = ProtectedData.Protect(
                plainBytes, null, DataProtectionScope.LocalMachine);
            string dir = Path.GetDirectoryName(AppConfig.TokenFilePath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(AppConfig.TokenFilePath, encryptedBytes);
        }

        /// <summary>
        ///  Read and decrypt the token from disk.
        ///  Throws <see cref="InvalidOperationException"/> with a clear message
        ///  if the token file is missing (i.e. the machine has not been
        ///  provisioned yet).
        /// </summary>
        public static string Load()
        {
            if (!File.Exists(AppConfig.TokenFilePath))
                throw new InvalidOperationException(
                    $"API token not provisioned on this machine.  " +
                    $"Run Provision-Token.ps1 as Administrator to set it up.  " +
                    $"Expected file: {AppConfig.TokenFilePath}");

            byte[] encryptedBytes = File.ReadAllBytes(AppConfig.TokenFilePath);
            byte[] plainBytes     = ProtectedData.Unprotect(
                encryptedBytes, null, DataProtectionScope.LocalMachine);
            return Encoding.UTF8.GetString(plainBytes);
        }

        /// <summary>Returns true if the token file exists on this machine.</summary>
        public static bool IsProvisioned() => File.Exists(AppConfig.TokenFilePath);
    }
}