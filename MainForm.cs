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
    // ── Custom controls ───────────────────────────────────────────────────────

    internal sealed class ModernButton : Button
    {
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

        protected override void OnMouseEnter(EventArgs e) { BackColor = _hover;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { BackColor = _normal; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(BackColor);
            using var path  = RoundRect(ClientRectangle, 8);
            e.Graphics.FillPath(brush, path);
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

    internal sealed class RoundedPanel : Panel
    {
        private const int Radius = 10;
        private static readonly Color _bg = Color.FromArgb(241, 245, 249);

        public RoundedPanel()
        {
            BackColor    = _bg;
            DoubleBuffered = true;
            ResizeRedraw   = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(_bg);
            for (int i = 3; i >= 1; i--)
            {
                var sr = new Rectangle(i + 1, i + 1, Width - i * 2 - 2, Height - i * 2 - 2);
                using var sb = new SolidBrush(Color.FromArgb(12, 0, 0, 0));
                g.FillRoundedRectangle(sb, sr, Radius);
            }
            var cr = new Rectangle(1, 1, Width - 4, Height - 4);
            using var cb = new SolidBrush(Color.White);
            g.FillRoundedRectangle(cb, cr, Radius);
        }
    }

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

    // ── Token input dialog ────────────────────────────────────────────────────

    internal sealed class TokenDialog : Form
    {
        private readonly TextBox _txtToken;
        private readonly Button  _btnTest;
        private readonly Button  _btnOk;
        private readonly Button  _btnCancel;
        private readonly Label   _lblStatus;
        private readonly string  _tunnelId;

        public string Token => _txtToken.Text.Trim();

        public TokenDialog(string tunnelId)
        {
            _tunnelId = tunnelId;

            Text            = "Cloudflare API Token Required";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            StartPosition   = FormStartPosition.CenterParent;
            ClientSize      = new Size(520, 220);
            BackColor       = Color.FromArgb(241, 245, 249);
            Font            = new Font("Segoe UI", 9.5f);

            var lblTitle = new Label
            {
                Text      = "Paste the Cloudflare API token from LastPass:",
                Location  = new Point(20, 20),
                Size      = new Size(480, 20),
                ForeColor = Color.FromArgb(15, 23, 42),
                Font      = new Font("Segoe UI Semibold", 10f, FontStyle.Bold)
            };

            var lblNote = new Label
            {
                Text      = "The token is used once and never saved to disk.",
                Location  = new Point(20, 44),
                Size      = new Size(480, 18),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            _txtToken = new TextBox
            {
                Location      = new Point(20, 72),
                Size          = new Size(480, 26),
                UseSystemPasswordChar = true,
                Font          = new Font("Cascadia Mono", 9.5f)
            };
            _txtToken.TextChanged += (_, _) => { _lblStatus.Text = string.Empty; };

            var chkShow = new CheckBox
            {
                Text     = "Show token",
                Location = new Point(20, 106),
                AutoSize = true,
                ForeColor = Color.FromArgb(100, 116, 139)
            };
            chkShow.CheckedChanged += (_, _) => { _txtToken.UseSystemPasswordChar = !chkShow.Checked; };

            _lblStatus = new Label
            {
                Location  = new Point(20, 136),
                Size      = new Size(480, 18),
                ForeColor = Color.FromArgb(220, 38, 38)
            };

            _btnTest = new Button
            {
                Text     = "Test Token",
                Location = new Point(20, 172),
                Size     = new Size(100, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White
            };
            _btnTest.FlatAppearance.BorderSize = 0;
            _btnTest.Click += BtnTest_Click;

            _btnOk = new Button
            {
                Text         = "Repair",
                Location     = new Point(300, 172),
                Size         = new Size(100, 32),
                DialogResult = DialogResult.OK,
                FlatStyle    = FlatStyle.Flat,
                BackColor    = Color.FromArgb(99, 102, 241),
                ForeColor    = Color.White
            };
            _btnOk.FlatAppearance.BorderSize = 0;

            _btnCancel = new Button
            {
                Text         = "Cancel",
                Location     = new Point(408, 172),
                Size         = new Size(92, 32),
                DialogResult = DialogResult.Cancel,
                FlatStyle    = FlatStyle.Flat,
                BackColor    = Color.FromArgb(241, 245, 249),
                ForeColor    = Color.FromArgb(30, 41, 59)
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);

            Controls.AddRange(new Control[] { lblTitle, lblNote, _txtToken, chkShow, _lblStatus, _btnTest, _btnOk, _btnCancel });
            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        private async void BtnTest_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtToken.Text))
            {
                _lblStatus.Text      = "Please paste a token first.";
                _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                return;
            }
            _btnTest.Enabled = false;
            _lblStatus.Text      = "Testing...";
            _lblStatus.ForeColor = Color.FromArgb(100, 116, 139);
            try
            {
                var api = new CloudflareApi(_txtToken.Text.Trim());
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var tunnel = await api.GetTunnelAsync(_tunnelId, cts.Token);
                _lblStatus.Text      = $"OK - connected to tunnel: {tunnel?.Name ?? _tunnelId}";
                _lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
            }
            catch (Exception ex)
            {
                _lblStatus.Text      = $"Failed: {ex.Message}";
                _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
            }
            finally { _btnTest.Enabled = true; }
        }
    }

    // ── Main form ─────────────────────────────────────────────────────────────

    public partial class MainForm : Form
    {
        private readonly CloudflaredServiceManager _serviceManager = new();
        private readonly CloudflaredInstaller      _installer      = new();
        private readonly FileLogger                _logger         = new();
        private readonly DiagnosticsExporter       _exporter;

        private TunnelServiceStatus? _currentStatus;
        private readonly List<string> _uiLogs = new();

        private static readonly Color _green = Color.FromArgb(22, 163, 74);
        private static readonly Color _red   = Color.FromArgb(220, 38, 38);
        private static readonly Color _amber = Color.FromArgb(217, 119, 6);
        private static readonly Color _slate = Color.FromArgb(100, 116, 139);

        public MainForm()
        {
            InitializeComponent();
            _exporter = new DiagnosticsExporter(_logger);
        }

        // ── Logging ───────────────────────────────────────────────────────────

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

        // ── Status badges ─────────────────────────────────────────────────────

        private static Color BadgeColour(string? value, bool isService = false)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "-") return _slate;
            var v = value.ToLowerInvariant();
            if (isService)
                return v == "running" ? _green : v == "stopped" ? _red : _amber;
            return v is "healthy" or "active" or "connected" ? _green
                 : v is "inactive" or "degraded"             ? _amber : _slate;
        }

        private void ApplyBadge(Label lbl, string text, bool isService = false)
        {
            lbl.Text      = text;
            lbl.ForeColor = BadgeColour(text, isService);
        }

        // ── Refresh ───────────────────────────────────────────────────────────
        // Refresh uses a read-only API instance with no token - only tunnel
        // discovery from the local service registry is used for status.
        // The Cloudflare API is NOT called during a plain refresh because we
        // do not want to store a token on the machine at all.

        public async Task RefreshStatusAsync()
        {
            btnRefresh.Enabled = false;
            btnRepair.Enabled  = false;
            btnExport.Enabled  = false;
            lstIngress.Items.Clear();

            try
            {
                LogInfo("Refreshing status...");
                var status = await GetLocalStatusAsync();
                _currentStatus = status;

                ApplyBadge(lblService, status.ServiceState, isService: true);
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

        // Local-only status: service state + tunnel ID from registry.
        // Does not call Cloudflare API.
        private Task<TunnelServiceStatus> GetLocalStatusAsync()
        {
            var status = new TunnelServiceStatus();
            if (!_serviceManager.IsInstalled())
            {
                status.ServiceState    = "NotInstalled";
                status.DiagnosticsNote = "Cloudflared service is not installed.";
                return Task.FromResult(status);
            }
            status.ServiceState = _serviceManager.GetStatusText();
            var imagePath = TunnelDiscovery.TryGetServiceImagePath();
            var token     = TunnelDiscovery.TryExtractTokenFromImagePath(imagePath);
            var tunnelId  = TunnelDiscovery.TryDecodeTunnelIdFromToken(token);
            status.TunnelId = tunnelId;
            if (tunnelId == null)
                status.DiagnosticsNote = "Could not decode tunnel ID from service command line.";
            return Task.FromResult(status);
        }

        // ── Repair ────────────────────────────────────────────────────────────
        // Repair prompts for the API token, uses it once, then discards it.

        public async Task RepairAsync()
        {
            if (_currentStatus?.TunnelId == null)
            {
                LogError("Cannot repair: no tunnel ID detected. Please refresh first.");
                return;
            }

            // Show token input dialog - token never stored on disk
            using var dlg = new TokenDialog(_currentStatus.TunnelId);
            if (dlg.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.Token))
            {
                LogInfo("Repair cancelled.");
                return;
            }

            // Create a short-lived API instance with the pasted token
            var api = new CloudflareApi(dlg.Token);

            btnRepair.Enabled  = false;
            btnRefresh.Enabled = false;
            btnExport.Enabled  = false;

            try
            {
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

                LogInfo("Requesting new tunnel token from Cloudflare API...");
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    var newToken = await api.GetTunnelTokenAsync(tunnelId, cts.Token);
                    if (string.IsNullOrWhiteSpace(newToken))
                        throw new InvalidOperationException("API returned an empty tunnel token.");
                    LogInfo("Installing Windows service with new token...");
                    _installer.InstallServiceWithToken(exe, newToken);
                }

                LogInfo("Starting service...");
                _serviceManager.StartService();
                LogInfo("Repair complete. Running final refresh...");
                await RefreshStatusAsync();
            }
            catch (Exception ex) { LogError("Repair failed", ex); }
            finally
            {
                // api instance goes out of scope here - token is gone
                btnRefresh.Enabled = true;
                btnRepair.Enabled  = true;
                btnExport.Enabled  = true;
            }
        }

        // ── Export ────────────────────────────────────────────────────────────

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
                MessageBox.Show(this, "Diagnostics exported to:" + Environment.NewLine + zipPath, "Export Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to export diagnostics: {ex.Message}", "Export Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private async void btnRefresh_Click(object? sender, EventArgs e) => await RefreshStatusAsync();
        private async void btnRepair_Click(object? sender, EventArgs e)  => await RepairAsync();
        private void btnExport_Click(object? sender, EventArgs e)         => ExportDiagnostics();
    }
}