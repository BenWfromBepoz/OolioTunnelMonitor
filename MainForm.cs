using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CloudflaredMonitor.Services;

namespace CloudflaredMonitor
{
    // ── Custom controls ────────────────────────────────────────────────────────

    /// <summary>Pill-shaped button styled for the dark sidebar.</summary>
    internal sealed class ModernButton : Button
    {
        private bool _hovered;
        private static readonly Color _normal = Color.FromArgb(30, 41, 59);
        private static readonly Color _hover  = Color.FromArgb(51, 65, 85);
        private static readonly Color _accent = Color.FromArgb(99, 102, 241);

        public ModernButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = _normal;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);
            Cursor = Cursors.Hand;
            TextAlign = ContentAlignment.MiddleLeft;
            Padding = new Padding(12, 0, 0, 0);
        }

        protected override void OnMouseEnter(EventArgs e) { _hovered = true;  BackColor = _hover;   Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; BackColor = _normal;  Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(BackColor);
            using var path  = RoundRect(ClientRectangle, 8);
            e.Graphics.FillPath(brush, path);

            // Accent left bar
            using var accent = new SolidBrush(_accent);
            e.Graphics.FillRectangle(accent, 0, 10, 3, Height - 20);

            var tf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
            using var fg = new SolidBrush(ForeColor);
            e.Graphics.DrawString(Text, Font, fg, new RectangleF(Padding.Left + 6, 0, Width - Padding.Left - 6, Height), tf);
        }

        private static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(r.Right - radius * 2, r.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(r.Right - radius * 2, r.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(r.X, r.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    /// <summary>White card with rounded corners and subtle shadow.</summary>
    internal sealed class RoundedPanel : Panel
    {
        public RoundedPanel()
        {
            BackColor = Color.White;
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(2, 2, Width - 5, Height - 5);
            // Shadow
            using var shadow = new SolidBrush(Color.FromArgb(18, 0, 0, 0));
            e.Graphics.FillRoundedRectangle(shadow, new Rectangle(4, 4, Width - 6, Height - 6), 10);
            // Card
            using var fill = new SolidBrush(Color.White);
            e.Graphics.FillRoundedRectangle(fill, rect, 10);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Transparent so parent background shows (for shadow)
        }

        protected override CreateParams CreateParams
        {
            get { var cp = base.CreateParams; cp.ExStyle |= 0x20; return cp; }
        }
    }

    // ── Extension helpers ──────────────────────────────────────────────────────
    internal static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect, int radius)
        {
            using var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            g.FillPath(brush, path);
        }
    }

    // ── Main form ──────────────────────────────────────────────────────────────

    public partial class MainForm : Form
    {
        private readonly CloudflaredServiceManager _serviceManager = new();
        private readonly CloudflaredInstaller      _installer      = new();
        private readonly CloudflareApi             _api            = new();
        private readonly FileLogger                _logger         = new();
        private readonly DiagnosticsExporter       _exporter;

        private TunnelServiceStatus? _currentStatus;
        private readonly List<string> _uiLogs = new();

        // Status badge colours
        private static readonly Color _green  = Color.FromArgb(22, 163, 74);
        private static readonly Color _red    = Color.FromArgb(220, 38, 38);
        private static readonly Color _amber  = Color.FromArgb(217, 119, 6);
        private static readonly Color _slate  = Color.FromArgb(100, 116, 139);

        public MainForm()
        {
            InitializeComponent();
            _exporter = new DiagnosticsExporter(_logger);
        }

        // ── Logging ────────────────────────────────────────────────────────────

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
            if (ex == null) _logger.Error(message); else _logger.Error(message, ex);
        }

        // ── Status badge helper ────────────────────────────────────────────────

        private static Color BadgeColour(string? value, bool isService = false)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "-") return _slate;
            var v = value.ToLowerInvariant();
            if (isService)
                return v == "running" ? _green : v == "stopped" ? _red : _amber;
            // remote status
            return v == "healthy" || v == "active" || v == "connected" ? _green
                 : v == "inactive" || v == "degraded"                  ? _amber
                 : _slate;
        }

        private void ApplyBadge(Label lbl, string text, bool isService = false)
        {
            lbl.Text      = text;
            lbl.ForeColor = BadgeColour(text, isService);
        }

        // ── Refresh ────────────────────────────────────────────────────────────

        public async Task RefreshStatusAsync()
        {
            btnRefresh.Enabled = false;
            btnRepair.Enabled  = false;
            btnExport.Enabled  = false;
            lstIngress.Items.Clear();

            try
            {
                LogInfo("Refreshing status...");
                var status = await GetStatusAsync();
                _currentStatus = status;

                ApplyBadge(lblService,      status.ServiceState,    isService: true);
                ApplyBadge(lblRemoteStatus, status.RemoteStatus ?? "-");

                lblTunnelName.Text = status.TunnelName   ?? "-";
                lblTunnelId.Text   = status.TunnelId     ?? "-";

                if (!string.IsNullOrWhiteSpace(status.DiagnosticsNote))
                    LogInfo(status.DiagnosticsNote!);

                foreach (var rule in status.Ingress)
                    lstIngress.Items.Add(rule.Display);

                btnRepair.Enabled = status.TunnelId != null;
                btnExport.Enabled = status.TunnelId != null;
                LogInfo("Refresh complete.");
            }
            catch (Exception ex) { LogError("Refresh failed", ex); }
            finally { btnRefresh.Enabled = true; }
        }

        private async Task<TunnelServiceStatus> GetStatusAsync()
        {
            var status = new TunnelServiceStatus();
            if (!_serviceManager.IsInstalled())
            {
                status.ServiceState     = "NotInstalled";
                status.DiagnosticsNote  = "Cloudflared service is not installed.";
                return status;
            }
            status.ServiceState = _serviceManager.GetStatusText();

            var imagePath = TunnelDiscovery.TryGetServiceImagePath();
            var token     = TunnelDiscovery.TryExtractTokenFromImagePath(imagePath);
            var tunnelId  = TunnelDiscovery.TryDecodeTunnelIdFromToken(token);
            status.TunnelId = tunnelId;
            if (tunnelId == null)
            {
                status.DiagnosticsNote = "Could not decode tunnel ID from service command line.";
                return status;
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            try
            {
                var tunnel = await _api.GetTunnelAsync(tunnelId, cts.Token);
                status.TunnelName   = tunnel?.Name;
                status.RemoteStatus = tunnel?.Status;

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
            catch (Exception ex) { status.DiagnosticsNote = $"Failed to query Cloudflare: {ex.Message}"; }

            return status;
        }

        // ── Repair ─────────────────────────────────────────────────────────────

        public async Task RepairAsync()
        {
            btnRepair.Enabled  = false;
            btnRefresh.Enabled = false;
            btnExport.Enabled  = false;

            try
            {
                if (_currentStatus?.TunnelId == null) { LogError("Cannot repair: no tunnel ID detected."); return; }
                var tunnelId = _currentStatus.TunnelId;
                LogInfo($"Repairing tunnel {tunnelId}...");

                LogInfo("Stopping service...");
                _serviceManager.StopServiceBestEffort();
                LogInfo("Killing residual cloudflared processes...");
                _serviceManager.KillCloudflaredProcess();
                LogInfo("Deleting service...");
                _serviceManager.DeleteService();

                if (chkReinstall.Checked)
                {
                    LogInfo("Reinstalling cloudflared MSI...");
                    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
                    var msiPath = await _installer.DownloadMsiAsync(cts.Token);
                    _installer.InstallMsi(msiPath);
                }

                LogInfo("Locating cloudflared executable...");
                var exe = _installer.FindCloudflaredExeOrThrow();

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
            catch (Exception ex) { LogError("Repair failed", ex); }
            finally { btnRefresh.Enabled = true; btnRepair.Enabled = true; btnExport.Enabled = true; }
        }

        // ── Export ─────────────────────────────────────────────────────────────

        public void ExportDiagnostics()
        {
            try
            {
                if (_currentStatus == null)
                {
                    MessageBox.Show(this, "Status unknown. Please refresh first.", "Export Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var ingressLines = new List<string>();
                foreach (var item in lstIngress.Items) ingressLines.Add(item?.ToString() ?? string.Empty);
                var zipPath = _exporter.Export(_currentStatus, _uiLogs, ingressLines);
                MessageBox.Show(this, $"Diagnostics exported to:\n{zipPath}", "Export Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to export diagnostics: {ex.Message}", "Export Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Handlers ───────────────────────────────────────────────────────────

        private async void btnRefresh_Click(object? sender, EventArgs e) => await RefreshStatusAsync();
        private async void btnRepair_Click(object? sender, EventArgs e)  => await RepairAsync();
        private void btnExport_Click(object? sender, EventArgs e)         => ExportDiagnostics();
    }
}