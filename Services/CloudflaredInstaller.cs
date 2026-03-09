using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CloudflaredMonitor.Services
{
    /// <summary>
    ///  Downloads and installs the cloudflared Windows MSI, locates the
    ///  cloudflared executable and installs the Windows service with a given
    ///  token.  Use this class during the repair flow when reinstalling
    ///  cloudflared.
    /// </summary>
    internal sealed class CloudflaredInstaller
    {
        /// <summary>
        ///  Download the MSI to a temporary file.  Returns the full path to
        ///  the downloaded file.  The caller is responsible for deleting the
        ///  file after installation.
        /// </summary>
        public async Task<string> DownloadMsiAsync(CancellationToken ct)
        {
            string temp = Path.Combine(Path.GetTempPath(), "cloudflared.msi");
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
            var bytes = await http.GetByteArrayAsync(AppConfig.CloudflaredMsiUrl, ct);
            await File.WriteAllBytesAsync(temp, bytes, ct);
            return temp;
        }

        /// <summary>
        ///  Run msiexec to install the MSI.  This method blocks until the
        ///  installation completes.  It is up to the caller to ensure
        ///  administrative privileges are available.
        /// </summary>
        public void InstallMsi(string msiPath)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "msiexec.exe",
                Arguments = $"/i \"{msiPath}\" /quiet /norestart",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var p = Process.Start(psi);
            if (p == null) throw new InvalidOperationException("Failed to start msiexec");
            p.WaitForExit();
            if (p.ExitCode != 0)
                throw new InvalidOperationException($"msiexec failed with exit code {p.ExitCode}: {p.StandardError.ReadToEnd()}");
        }

        /// <summary>
        ///  Find the cloudflared executable in common installation locations or
        ///  by using the PATH environment variable.  Throws if the exe cannot
        ///  be found after a successful MSI install.
        /// </summary>
        public string FindCloudflaredExeOrThrow()
        {
            // Check PATH first
            var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (var dir in pathEnv.Split(';'))
            {
                try
                {
                    var candidate = Path.Combine(dir.Trim(), "cloudflared.exe");
                    if (File.Exists(candidate)) return candidate;
                }
                catch { /* ignore invalid paths */ }
            }
            // Check common install directories
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var candidates = new[]
            {
                Path.Combine(programFiles, "Cloudflare", "Cloudflared", "cloudflared.exe"),
                Path.Combine(programFiles, "cloudflared", "cloudflared.exe"),
                Path.Combine(programFilesX86, "cloudflared", "cloudflared.exe"),
                Path.Combine("C:\\cloudflared", "cloudflared.exe")
            };
            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate)) return candidate;
            }
            throw new FileNotFoundException("cloudflared.exe not found after MSI installation.");
        }

        /// <summary>
        ///  Install the cloudflared Windows service using the provided token.
        ///  This calls `cloudflared service install` with the token.
        /// </summary>
        public void InstallServiceWithToken(string exePath, string token)
        {
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"service install {token}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var p = Process.Start(psi);
            if (p == null) throw new InvalidOperationException("Failed to start cloudflared service install");
            p.WaitForExit();
            if (p.ExitCode != 0)
                throw new InvalidOperationException($"cloudflared service install failed: {p.StandardError.ReadToEnd()}");
        }
    }
}