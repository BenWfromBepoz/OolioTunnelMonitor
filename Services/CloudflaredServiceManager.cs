using System;
using System.Diagnostics;
using System.ServiceProcess;

namespace CloudflaredMonitor.Services
{
    /// <summary>
    ///  Provides methods for interacting with the Windows service that hosts
    ///  cloudflared.  This includes checking if the service exists,
    ///  stopping/starting it, killing any lingering processes and deleting the
    ///  service entry.  All operations catch exceptions internally to
    ///  minimise side effects when the service is not in the expected state.
    /// </summary>
    internal sealed class CloudflaredServiceManager
    {
        public bool IsInstalled()
        {
            try
            {
                using var sc = new ServiceController(AppConfig.ServiceName);
                // Accessing Status will throw if the service doesn't exist
                _ = sc.Status;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GetStatusText()
        {
            if (!IsInstalled()) return "NotInstalled";
            using var sc = new ServiceController(AppConfig.ServiceName);
            return sc.Status.ToString();
        }

        /// <summary>
        ///  Stop the service, waiting up to StopTimeout for it to reach the
        ///  stopped state.  If the service is not installed or already
        ///  stopped, this is a no‑op.
        /// </summary>
        public void StopServiceBestEffort()
        {
            if (!IsInstalled()) return;
            using var sc = new ServiceController(AppConfig.ServiceName);
            try
            {
                if (sc.Status == ServiceControllerStatus.Running || sc.Status == ServiceControllerStatus.Paused)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, AppConfig.StopTimeout);
                }
            }
            catch
            {
                // ignore errors; killing process will handle stuck states
            }
        }

        /// <summary>
        ///  Start the service.  Waits up to 15 seconds for the service to
        ///  report the running state.  If the service is not installed or
        ///  already running, this is a no‑op.
        /// </summary>
        public void StartService()
        {
            if (!IsInstalled()) return;
            using var sc = new ServiceController(AppConfig.ServiceName);
            try
            {
                if (sc.Status != ServiceControllerStatus.Running)
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
                }
            }
            catch
            {
                // ignore; start failures will be surfaced through status refresh
            }
        }

        /// <summary>
        ///  Kill any running cloudflared.exe processes.  This is used when
        ///  the service fails to stop gracefully or when stray processes
        ///  remain after uninstalling the service.
        /// </summary>
        public void KillCloudflaredProcess()
        {
            foreach (var p in Process.GetProcessesByName("cloudflared"))
            {
                try { p.Kill(entireProcessTree: true); }
                catch { /* ignore */ }
            }
        }

        /// <summary>
        ///  Delete the service entry from the SCM.  This uses the sc.exe
        ///  command rather than ServiceController.Delete because the latter
        ///  requires .NET Framework on Windows.  The command is idempotent.
        /// </summary>
        public void DeleteService()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"delete {AppConfig.ServiceName}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(15000);
            }
            catch
            {
                // ignore; service may not exist
            }
        }
    }
}