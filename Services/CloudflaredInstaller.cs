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

        // Uninstall existing cloudflared MSI - clears Windows Installer record to prevent exit 1603
        public void UninstallExistingMsi()
        {
            string? productCode = FindCloudflaredProductCode();
            if (productCode == null) return;
            RunProcess("msiexec.exe", "/x " + productCode + " /quiet /norestart", waitMs: 90000);
        }

        private static string? FindCloudflaredProductCode()
        {
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
                    ? "Exit 1603: pending Windows reboot or another installer running. Reboot and retry."
                    : "Check Windows Event Viewer > Application for MSI errors.";
                throw new InvalidOperationException(msg);
            }
        }

        public string FindCloudflaredExeOrThrow()
        {
            var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in pathEnv.Split(';'))
            {
                try { var c = Path.Combine(dir.Trim(), "cloudflared.exe"); if (File.Exists(c)) return c; }
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
                "Open a new command prompt to refresh PATH and verify the install completed.");
        }

        // Install the Windows service with the tunnel token.
        // The cloudflared MSI sometimes auto-registers a service during installation.
        // We delete any existing service first so the install is always clean.
        public void InstallServiceWithToken(string exePath, string token)
        {
            // Remove any service the MSI may have created automatically
            EnsureServiceDeleted(exePath);

            var psi = new ProcessStartInfo
            {
                FileName               = exePath,
                Arguments              = "service install " + token,
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            };
            using var p = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start cloudflared.exe");
            p.WaitForExit(30000);
            if (!p.HasExited) { try { p.Kill(entireProcessTree: true); } catch { } }

            if (p.ExitCode != 0)
            {
                // Capture output to give a meaningful error message
                string stderr = p.StandardError.ReadToEnd().Trim();
                string stdout = p.StandardOutput.ReadToEnd().Trim();
                string detail = !string.IsNullOrEmpty(stderr) ? stderr : stdout;
                throw new InvalidOperationException(
                    "cloudflared service install failed (exit " + p.ExitCode + ")" +
                    (string.IsNullOrEmpty(detail) ? "." : ": " + detail));
            }
        }

        // Delete the cloudflared service cleanly before (re)installing
        private static void EnsureServiceDeleted(string exePath)
        {
            // Try cloudflared's own uninstall command first
            try { RunProcess(exePath, "service uninstall", waitMs: 15000); } catch { }
            // Then sc.exe as belt-and-braces
            try { RunProcess("sc.exe", "delete " + AppConfig.ServiceName, waitMs: 15000); } catch { }
            // Give the SCM a moment to complete the delete
            Thread.Sleep(1500);
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
