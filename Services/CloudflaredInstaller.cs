using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace CloudflaredMonitor.Services
{
    internal sealed class CloudflaredInstaller
    {
        public async Task<string> DownloadMsiAsync(CancellationToken ct)
        {
            string temp = Path.Combine(Path.GetTempPath(), "cloudflared.msi");
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            var bytes = await http.GetByteArrayAsync(AppConfig.CloudflaredMsiUrl, ct);
            await File.WriteAllBytesAsync(temp, bytes, ct);
            return temp;
        }

        // Uninstall any existing cloudflared MSI to clear Windows Installer record.
        // This prevents msiexec exit code 1603 on the subsequent fresh install.
        public void UninstallExistingMsi()
        {
            string? productCode = FindCloudflaredProductCode();
            if (productCode == null) return;
            RunProcess("msiexec.exe", "/x " + productCode + " /quiet /norestart", waitMs: 90000);
        }

        private static string? FindCloudflaredProductCode()
        {
            // Build registry paths at runtime - avoids escape issues in source
            string sep = System.IO.Path.DirectorySeparatorChar.ToString();
            string[] roots = {
                string.Join(sep, "SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Uninstall"),
                string.Join(sep, "SOFTWARE", "WOW6432Node", "Microsoft", "Windows", "CurrentVersion", "Uninstall")
            };
            foreach (var root in roots)
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(root);
                    if (key == null) continue;
                    foreach (var subName in key.GetSubKeyNames())
                    {
                        try
                        {
                            using var sub = key.OpenSubKey(subName);
                            if (sub == null) continue;
                            var dn = sub.GetValue("DisplayName") as string ?? "";
                            if (dn.IndexOf("cloudflared", StringComparison.OrdinalIgnoreCase) >= 0)
                                return subName;
                        }
                        catch { }
                    }
                }
                catch { }
            }
            return null;
        }

        public void InstallMsi(string msiPath)
        {
            string args = "/i " + '"' + msiPath + '"' + " /quiet /norestart";
            int exit = RunProcess("msiexec.exe", args, waitMs: 120000);
            if (exit != 0)
            {
                string msg = "msiexec failed with exit code " + exit + ". ";
                msg += exit == 1603
                    ? "Exit 1603: pending reboot or another installer running. Reboot and retry."
                    : "Check Windows Event Viewer > Application for MSI errors.";
                throw new InvalidOperationException(msg);
            }
        }

        public string FindCloudflaredExeOrThrow()
        {
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
            var pf   = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string[] candidates = {
                Path.Combine(pf,   "Cloudflare",  "Cloudflared", "cloudflared.exe"),
                Path.Combine(pf,   "cloudflared", "cloudflared.exe"),
                Path.Combine(pf86, "cloudflared", "cloudflared.exe"),
                Path.Combine("C:", "cloudflared", "cloudflared.exe")
            };
            foreach (var c in candidates)
                if (File.Exists(c)) return c;
            throw new FileNotFoundException(
                "cloudflared.exe not found after MSI installation. " +
                "Open a new command prompt to refresh PATH and verify the install.");
        }

        public void InstallServiceWithToken(string exePath, string token)
        {
            int exit = RunProcess(exePath, "service install " + token, waitMs: 30000);
            if (exit != 0)
                throw new InvalidOperationException(
                    "cloudflared service install failed with exit code " + exit + ".");
        }

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
                ?? throw new InvalidOperationException("Failed to start " + fileName);
            p.WaitForExit(waitMs);
            if (!p.HasExited) { try { p.Kill(entireProcessTree: true); } catch { } }
            return p.ExitCode;
        }
    }
}
