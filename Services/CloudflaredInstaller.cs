using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace CloudflaredMonitor.Services
{
    /// <summary>
    /// Handles downloading, uninstalling, installing and registering cloudflared.
    /// The uninstall step is critical before reinstalling to avoid msiexec exit 1603
    /// (which occurs when a previous installation record is still present in the
    /// Windows Installer database).
    /// </summary>
    internal sealed class CloudflaredInstaller
    {
        // ── Download ──────────────────────────────────────────────────────────

        public async Task<string> DownloadMsiAsync(CancellationToken ct)
        {
            string temp = Path.Combine(Path.GetTempPath(), "cloudflared.msi");
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
            var bytes = await http.GetByteArrayAsync(AppConfig.CloudflaredMsiUrl, ct);
            await File.WriteAllBytesAsync(temp, bytes, ct);
            return temp;
        }

        // ── Uninstall existing cloudflared MSI ───────────────────────────────

        /// <summary>
        /// Attempts to uninstall any existing cloudflared MSI via msiexec /x.
        /// Looks up the product code from the registry. If no entry is found the
        /// method is a no-op - it never throws.
        /// </summary>
        public void UninstallExistingMsi()
        {
            string? productCode = FindCloudflaredProductCode();
            if (productCode == null) return;

            RunProcess("msiexec.exe", $"/x {productCode} /quiet /norestart", waitMs: 60000);
        }

        private static string? FindCloudflaredProductCode()
        {
            // Search both 32-bit and 64-bit uninstall keys
            string[] roots =
            {
                @"SOFTWAREMicrosoftWindowsCurrentVersionUninstall",
                @"SOFTWAREWOW6432NodeMicrosoftWindowsCurrentVersionUninstall"
            };

            foreach (var root in roots)
            {
                using var key = Registry.LocalMachine.OpenSubKey(root);
                if (key == null) continue;
                foreach (var subName in key.GetSubKeyNames())
                {
                    using var sub = key.OpenSubKey(subName);
                    if (sub == null) continue;
                    var displayName = sub.GetValue("DisplayName") as string ?? "";
                    if (displayName.IndexOf("cloudflared", StringComparison.OrdinalIgnoreCase) >= 0)
                        return subName; // subName IS the product code for MSI entries
                }
            }
            return null;
        }

        // ── Install MSI ───────────────────────────────────────────────────────

        public void InstallMsi(string msiPath)
        {
            int exit = RunProcess("msiexec.exe",
                $"/i "{msiPath}" /quiet /norestart",
                waitMs: 120000);

            if (exit != 0)
                throw new InvalidOperationException(
                    $"msiexec failed with exit code {exit}. " +
                    (exit == 1603
                        ? "Exit 1603 usually means a pending Windows reboot is required, " +
                          "or another installer is currently running. Please reboot and retry."
                        : "Check Windows Event Viewer > Application for MSI errors."));
        }

        // ── Find cloudflared.exe ──────────────────────────────────────────────

        public string FindCloudflaredExeOrThrow()
        {
            // 1. Check PATH
            var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in pathEnv.Split(';'))
            {
                try
                {
                    var candidate = Path.Combine(dir.Trim(), "cloudflared.exe");
                    if (File.Exists(candidate)) return candidate;
                }
                catch { }
            }

            // 2. Check common install directories
            var pf  = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var candidates = new[]
            {
                Path.Combine(pf,   "Cloudflare",  "Cloudflared", "cloudflared.exe"),
                Path.Combine(pf,   "cloudflared", "cloudflared.exe"),
                Path.Combine(pf86, "cloudflared", "cloudflared.exe"),
                @"C:\cloudflared\cloudflared.exe"
            };

            foreach (var c in candidates)
                if (File.Exists(c)) return c;

            throw new FileNotFoundException(
                "cloudflared.exe not found after MSI installation. " +
                "Try opening a new command prompt (to refresh PATH) and check " +
                "C:\\Program Files\\Cloudflare\\Cloudflared\\.");
        }

        // ── Install Windows service ───────────────────────────────────────────

        public void InstallServiceWithToken(string exePath, string token)
        {
            int exit = RunProcess(exePath,
                $"service install {token}",
                waitMs: 30000);

            if (exit != 0)
                throw new InvalidOperationException(
                    $"cloudflared service install failed with exit code {exit}.");
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private static int RunProcess(string fileName, string arguments, int waitMs = 30000)
        {
            var psi = new ProcessStartInfo
            {
                FileName               = fileName,
                Arguments              = arguments,
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            };
            using var p = Process.Start(psi);
            if (p == null) throw new InvalidOperationException($"Failed to start {fileName}");
            p.WaitForExit(waitMs);
            if (!p.HasExited) { try { p.Kill(entireProcessTree: true); } catch { } }
            return p.ExitCode;
        }
    }
}
