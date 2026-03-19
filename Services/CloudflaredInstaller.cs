using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace CloudflaredMonitor.Services
{
    /// <summary>
    /// Handles downloading, uninstalling, installing and registering cloudflared.
    /// The uninstall step before reinstalling removes the Windows Installer product
    /// record and prevents msiexec exit code 1603.
    /// </summary>
    internal sealed class CloudflaredInstaller
    {
        // ── Download ──────────────────────────────────────────────────────────

        public async Task<string> DownloadMsiAsync(CancellationToken ct)
        {
            string temp = Path.Combine(Path.GetTempPath(), "cloudflared.msi");
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            var bytes = await http.GetByteArrayAsync(AppConfig.CloudflaredMsiUrl, ct);
            await File.WriteAllBytesAsync(temp, bytes, ct);
            return temp;
        }

        // ── Uninstall existing cloudflared MSI ───────────────────────────────

        /// <summary>
        /// Uninstalls any existing cloudflared MSI via msiexec /x using the
        /// product code found in the registry. Never throws - if nothing is found
        /// it is a silent no-op.
        /// </summary>
        public void UninstallExistingMsi()
        {
            string? productCode = FindCloudflaredProductCode();
            if (productCode == null) return;
            RunProcess("msiexec.exe", $"/x {productCode} /quiet /norestart", waitMs: 90000);
        }

        private static string? FindCloudflaredProductCode()
        {
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
                    try
                    {
                        using var sub = key.OpenSubKey(subName);
                        if (sub == null) continue;
                        var displayName = sub.GetValue("DisplayName") as string ?? "";
                        if (displayName.IndexOf("cloudflared", StringComparison.OrdinalIgnoreCase) >= 0)
                            return subName;
                    }
                    catch { }
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
                        ? "Exit 1603: a pending Windows reboot may be required, or another installer is running. Please reboot and retry."
                        : "Check Windows Event Viewer > Application for MSI errors."));
        }

        // ── Locate cloudflared.exe ────────────────────────────────────────────

        public string FindCloudflaredExeOrThrow()
        {
            // 1. Check PATH
            var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in pathEnv.Split(';'))
            {
                try
                {
                    var c = Path.Combine(dir.Trim(), "cloudflared.exe");
                    if (File.Exists(c)) return c;
                }
                catch { }
            }

            // 2. Common install locations
            var pf   = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var candidates = new[]
            {
                Path.Combine(pf,   "Cloudflare",  "Cloudflared", "cloudflared.exe"),
                Path.Combine(pf,   "cloudflared", "cloudflared.exe"),
                Path.Combine(pf86, "cloudflared", "cloudflared.exe"),
                Path.Combine("C:", "cloudflared", "cloudflared.exe")
            };

            foreach (var c in candidates)
                if (File.Exists(c)) return c;

            throw new FileNotFoundException(
                "cloudflared.exe not found after MSI installation. " +
                "Try opening a new command prompt (to refresh PATH) and verify " +
                "the install completed successfully.");
        }

        // ── Install Windows service ───────────────────────────────────────────

        public void InstallServiceWithToken(string exePath, string token)
        {
            int exit = RunProcess(exePath, $"service install {token}", waitMs: 30000);
            if (exit != 0)
                throw new InvalidOperationException(
                    $"cloudflared service install failed with exit code {exit}.");
        }

        // ── Process helper ────────────────────────────────────────────────────

        private static int RunProcess(string fileName, string arguments, int waitMs)
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
            using var p = Process.Start(psi)
                ?? throw new InvalidOperationException($"Failed to start {fileName}");
            p.WaitForExit(waitMs);
            if (!p.HasExited) { try { p.Kill(entireProcessTree: true); } catch { } }
            return p.ExitCode;
        }
    }
}
