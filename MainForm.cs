using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CloudflaredMonitor.Services;

namespace CloudflaredMonitor
{
    /// <summary>
    ///  Main window for the monitoring tool.  Exposes diagnostic status and
    ///  repair actions and writes both to a log text area and a log file.
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly CloudflaredServiceManager _serviceManager = new();
        private readonly CloudflaredInstaller _installer = new();
        private readonly CloudflareApi _api = new();
        private readonly FileLogger _logger = new();
        private readonly DiagnosticsExporter _exporter;

        // Keep a copy of the most recent status for export
        private TunnelServiceStatus? _currentStatus;
        // Maintain a live list of log lines for diagnostics export
        private readonly List<string> _uiLogs = new();

        public MainForm()
        {
            InitializeComponent();
            // Instantiate exporter with logger dependency
            _exporter = new DiagnosticsExporter(_logger);
        }

        /// <summary>
        /// Append a message to the UI log and the file log.
        /// </summary>
        private void LogInfo(string message)
        {
            string line = $"INFO: {message}";
            txtLog.AppendText(line + Environment.NewLine);
            _uiLogs.Add(line);
            _logger.Info(message);
        }

        private void LogError(string message, Exception? ex = null)
        {
            string line = ex == null ? $"ERROR: {message}" : $"ERROR: {message} - {ex.Message}";
            txtLog.AppendText(line + Environment.NewLine);
            _uiLogs.Add(line);
            if (ex == null)
            {
                _logger.Error(message);
            }
            else
            {
                _logger.Error(message, ex);
            }
        }

        /// <summary>
        /// Refresh the service and tunnel status and update the UI.  Called on
        /// startup and when the user clicks the refresh button.
        /// </summary>
        public async Task RefreshStatusAsync()
        {
            btnRefresh.Enabled = false;
            btnRepair.Enabled = false;
            btnExport.Enabled = false;

            lstIngress.Items.Clear();

            try
            {
                LogInfo("Refreshing status...");
                var status = await GetStatusAsync();
                _currentStatus = status;

                lblService.Text = status.ServiceState;
                lblTunnelId.Text = status.TunnelId ?? "-";
                lblTunnelName.Text = status.TunnelName ?? "-";
                lblRemoteStatus.Text = status.RemoteStatus ?? "-";
                if (!string.IsNullOrWhiteSpace(status.DiagnosticsNote))
                {
                    LogInfo(status.DiagnosticsNote!);
                }
                foreach (var rule in status.Ingress)
                {
                    lstIngress.Items.Add(rule.Display);
                }
                // enable buttons if we have a tunnel id
                btnRepair.Enabled = status.TunnelId != null;
                btnExport.Enabled = status.TunnelId != null;
                LogInfo("Refresh complete.");
            }
            catch (Exception ex)
            {
                LogError("Refresh failed", ex);
            }
            finally
            {
                btnRefresh.Enabled = true;
            }
        }

        /// <summary>
        /// Gather current status from local service, decode the tunnel ID and
        /// query Cloudflare for remote state and ingress configuration.
        /// </summary>
        private async Task<TunnelServiceStatus> GetStatusAsync()
        {
            var status = new TunnelServiceStatus();

            // Check if the Windows service is present
            if (!_serviceManager.IsInstalled())
            {
                status.ServiceState = "NotInstalled";
                status.DiagnosticsNote = "Cloudflared service is not installed.";
                return status;
            }

            status.ServiceState = _serviceManager.GetStatusText();

            // Try to extract the token and decode the tunnel ID
            var imagePath = TunnelDiscovery.TryGetServiceImagePath();
            var token = TunnelDiscovery.TryExtractTokenFromImagePath(imagePath);
            var tunnelId = TunnelDiscovery.TryDecodeTunnelIdFromToken(token);
            status.TunnelId = tunnelId;
            if (tunnelId == null)
            {
                status.DiagnosticsNote = "Could not decode tunnel ID from service command line.";
                return status;
            }

            // Fetch remote tunnel information
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            try
            {
                var tunnel = await _api.GetTunnelAsync(tunnelId, cts.Token);
                status.TunnelName = tunnel?.Name;
                status.RemoteStatus = tunnel?.Status;

                // Fetch ingress configuration
                var cfg = await _api.GetTunnelConfigAsync(tunnelId, cts.Token);
                if (cfg?.Config?.Ingress != null)
                {
                    foreach (var rule in cfg.Config.Ingress)
                    {
                        if (!string.IsNullOrWhiteSpace(rule.Hostname))
                        {
                            var path = string.IsNullOrWhiteSpace(rule.Path) ? "/" : rule.Path;
                            status.Ingress.Add(new IngressRuleView
                            {
                                Display = $"{rule.Hostname,-45} {path,-10} -> {rule.Service}"
                            });
                        }
                        else if (!string.IsNullOrWhiteSpace(rule.Service))
                        {
                            status.Ingress.Add(new IngressRuleView { Display = $"(catch-all) -> {rule.Service}" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                status.DiagnosticsNote = $"Failed to query Cloudflare: {ex.Message}";
            }

            return status;
        }

        /// <summary>
        /// Repair the existing tunnel by stopping and removing the service,
        /// reinstalling the cloudflared MSI if requested, fetching a new token
        /// and installing a fresh service.
        /// </summary>
        public async Task RepairAsync()
        {
            btnRepair.Enabled = false;
            btnRefresh.Enabled = false;
            btnExport.Enabled = false;

            try
            {
                if (_currentStatus?.TunnelId == null)
                {
                    LogError("Cannot repair: no tunnel ID detected.");
                    return;
                }
                var tunnelId = _currentStatus.TunnelId;
                LogInfo($"Repairing tunnel {tunnelId}...");

                // Stop and remove service
                LogInfo("Stopping service...");
                _serviceManager.StopServiceBestEffort();
                LogInfo("Killing residual cloudflared processes...");
                _serviceManager.KillCloudflaredProcess();
                LogInfo("Deleting service...");
                _serviceManager.DeleteService();

                // Optionally reinstall the MSI
                if (chkReinstall.Checked)
                {
                    LogInfo("Reinstalling cloudflared MSI...");
                    using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3)))
                    {
                        var msiPath = await _installer.DownloadMsiAsync(cts.Token);
                        _installer.InstallMsi(msiPath);
                    }
                }

                LogInfo("Locating cloudflared executable...");
                var exe = _installer.FindCloudflaredExeOrThrow();

                // Fetch new token
                LogInfo("Requesting new tunnel token via Cloudflare API...");
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    var newToken = await _api.GetTunnelTokenAsync(tunnelId, cts.Token);
                    if (string.IsNullOrWhiteSpace(newToken))
                        throw new InvalidOperationException("API returned empty token");
                    LogInfo("Installing Windows service with new token...");
                    _installer.InstallServiceWithToken(exe, newToken);
                }

                LogInfo("Starting service...");
                _serviceManager.StartService();

                LogInfo("Repair finished. Performing final refresh...");
                await RefreshStatusAsync();
            }
            catch (Exception ex)
            {
                LogError("Repair failed", ex);
            }
            finally
            {
                btnRefresh.Enabled = true;
                btnRepair.Enabled = true;
                btnExport.Enabled = true;
            }
        }

        /// <summary>
        /// Export a diagnostic bundle containing the current status, the log file
        /// and the UI log lines.  The bundle is zipped to a file and stored in
        /// ProgramData.
        /// </summary>
        public void ExportDiagnostics()
        {
            try
            {
                if (_currentStatus == null)
                {
                    MessageBox.Show(this, "Status unknown. Please refresh first.", "Export Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // Build list of ingress lines for export
                var ingressLines = new List<string>();
                foreach (var item in lstIngress.Items)
                {
                    ingressLines.Add(item?.ToString() ?? string.Empty);
                }
                var zipPath = _exporter.Export(_currentStatus, _uiLogs, ingressLines);
                MessageBox.Show(this, $"Diagnostics exported to:\n{zipPath}", "Export Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to export diagnostics: {ex.Message}", "Export Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnRefresh_Click(object? sender, EventArgs e)
        {
            await RefreshStatusAsync();
        }

        private async void btnRepair_Click(object? sender, EventArgs e)
        {
            await RepairAsync();
        }

        private void btnExport_Click(object? sender, EventArgs e)
        {
            ExportDiagnostics();
        }
    }
}