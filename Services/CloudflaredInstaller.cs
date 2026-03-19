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
        // Strategy:
        //   1. Remove any existing service (MSI auto-creates one)
        //   2. Delete the EventLog registry key the MSI creates (causes exit 1 on service install)
        //   3. Run cloudflared service install
        //   4. Treat the result as success if the service now exists, even if exit != 0
        public void InstallServiceWithToken(string exePath, string token)
        {
            // Step A: Remove any service the MSI may have auto-created
            EnsureServiceDeleted(exePath);

            // Step B: Delete the EventLog registry key that the MSI creates.
            // cloudflared service install tries to create this key and fails with exit 1
            // if it already exists, even though the service itself installs successfully.
            DeleteEventLogRegistryKey();

            // Step C: Run service install and capture output
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
            // Read output before WaitForExit to avoid deadlock on full buffers
            string stdout = p.StandardOutput.ReadToEnd();
            string stderr = p.StandardError.ReadToEnd();
            p.WaitForExit(30000);
            if (!p.HasExited) { try { p.Kill(entireProcessTree: true); } catch { } }

            // Step D: Check if the service actually got installed regardless of exit code.
            // cloudflared exits 1 when the EventLog key exists but still installs the service.
            bool serviceExists = IsServiceInstalled();
            if (serviceExists)
                return; // Service is installed - success regardless of exit code

            // Service is genuinely not installed - throw with detail
            string detail = !string.IsNullOrEmpty(stderr) ? stderr.Trim() : stdout.Trim();
            throw new InvalidOperationException(
                "cloudflared service install failed (exit " + p.ExitCode + ")" +
                (string.IsNullOrEmpty(detail) ? "." : ": " + detail));
        }

        // Check whether the Cloudflared service exists in the SCM
        private static bool IsServiceInstalled()
        {
            try
            {
                using var sc = new System.ServiceProcess.ServiceController(AppConfig.ServiceName);
                _ = sc.Status; // throws if not installed
                return true;
            }
            catch { return false; }
        }

        // Delete HKLM\SYSTEM\CurrentControlSet\Services\EventLog\Application\Cloudflared
        // The MSI creates this key; cloudflared service install tries to recreate it and exits 1.
        private static void DeleteEventLogRegistryKey()
        {
            try
            {
                string sep = System.IO.Path.DirectorySeparatorChar.ToString();
                string keyPath = string.Join(sep,
                    "SYSTEM", "CurrentControlSet", "Services",
                    "EventLog", "Application", AppConfig.ServiceName);
                Registry.LocalMachine.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);
            }
            catch { }
        }

        // Remove any existing cloudflared service cleanly before reinstalling
        private static void EnsureServiceDeleted(string exePath)
        {
            try { RunProcess(exePath, "service uninstall", waitMs: 15000); } catch { }
            try { RunProcess("sc.exe", "delete " + AppConfig.ServiceName, waitMs: 15000); } catch { }
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
