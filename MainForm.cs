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
        // Gradient purple matching Oolio POS button style
        private static readonly Color _gradStart = Color.FromArgb(103, 58, 182);   // #673ab6
        private static readonly Color _gradEnd   = Color.FromArgb(81,  45, 168);   // slightly deeper
        private static readonly Color _gradHovS  = Color.FromArgb(126, 87, 194);
        private static readonly Color _gradHovE  = Color.FromArgb(103, 58, 182);

        private bool _hovered;

        public ModernButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);
            Cursor = Cursors.Hand;
            TextAlign = ContentAlignment.MiddleLeft;
            Padding = new Padding(12, 0, 0, 0);
        }

        protected override void OnMouseEnter(EventArgs e) { _hovered = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var path = RoundRect(ClientRectangle, 8);
            var c1 = _hovered ? _gradHovS : _gradStart;
            var c2 = _hovered ? _gradHovE : _gradEnd;
            using var brush = new LinearGradientBrush(ClientRectangle, c1, c2, LinearGradientMode.Vertical);
            g.FillPath(brush, path);

            // subtle left accent bar (lighter)
            using var accentPath = RoundRect(new Rectangle(0, 8, 3, Height - 16), 1);
            using var accentBrush = new SolidBrush(Color.FromArgb(180, 255, 255, 255));
            g.FillPath(accentBrush, accentPath);

            var tf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
            using var fg = new SolidBrush(Color.White);
            g.DrawString(Text, Font, fg, new RectangleF(Padding.Left + 6, 0, Width - Padding.Left - 6, Height), tf);
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

        public RoundedPanel()
        {
            BackColor      = Color.FromArgb(241, 245, 249);
            DoubleBuffered = true;
            ResizeRedraw   = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? Color.FromArgb(241, 245, 249));

            // Shadow layers
            for (int i = 3; i >= 1; i--)
            {
                var sr = new Rectangle(i + 1, i + 1, Width - i * 2 - 2, Height - i * 2 - 2);
                using var sb = new SolidBrush(Color.FromArgb(12, 0, 0, 0));
                g.FillRoundedRectangle(sb, sr, Radius);
            }
            // White card — fully bounded (no edge bleed)
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

    // ── Sidebar header panel with gradient + Oolio logo ───────────────────────

    internal sealed class SidebarHeaderPanel : Panel
    {
        public SidebarHeaderPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw   = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode   = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Gradient background: #673ab6 → deeper purple
            using var grad = new LinearGradientBrush(ClientRectangle,
                Color.FromArgb(103, 58, 182), Color.FromArgb(69, 39, 160), LinearGradientMode.Vertical);
            g.FillRectangle(grad, ClientRectangle);

            // ── Oolio logo ────────────────────────────────────────────────────
            // Drawn using GDI+ to match the oolio.com logo style:
            //   Two filled circles side-by-side (the "OO") + "LIO" text
            //   "Oolio" in #673ab6 on white bg — here inverted: white circles on purple bg

            int logoY  = 18;
            int circleR = 10; // radius of each circle in the "OO"
            int gapX    = 4;  // gap between circles

            // Left position for logo group (centred in sidebar)
            int totalLogoW = circleR * 4 + gapX + 2 + 50; // two circles + "LIO" text estimate
            int startX = (Width - totalLogoW) / 2;

            // Left circle (filled white)
            using var whiteBrush = new SolidBrush(Color.White);
            g.FillEllipse(whiteBrush, startX, logoY, circleR * 2, circleR * 2);

            // Right circle (filled white, with purple inner hole to make it a ring like the logo)
            int c2x = startX + circleR * 2 + gapX;
            g.FillEllipse(whiteBrush, c2x, logoY, circleR * 2, circleR * 2);
            // Inner cutout to make donut
            using var purpleFill = new SolidBrush(Color.FromArgb(103, 58, 182));
            g.FillEllipse(purpleFill, c2x + 4, logoY + 4, (circleR - 4) * 2, (circleR - 4) * 2);

            // "LIO" text immediately after circles
            int textX = c2x + circleR * 2 + 5;
            using var logoFont = new Font("Segoe UI", 13f, FontStyle.Bold, GraphicsUnit.Point);
            g.DrawString("LIO", logoFont, whiteBrush, new PointF(textX, logoY - 1));

            // ── "Oolio" label in #673ab6 colour ──────────────────────────────
            // Since we're on a purple bg, render "Oolio" in white (branding text)
            // Per spec: "Oolio" in #673ab6, but on the dark sidebar the readable equivalent
            // is white — we render it white here with the purple shown through the logo shape.
            // Below the logo, render the app name lines.

            int nameY = logoY + circleR * 2 + 10;

            // "Oolio" in white (on purple bg — matches brand intent; purple text on white bg)
            using var oolioFont = new Font("Segoe UI", 14f, FontStyle.Bold, GraphicsUnit.Point);
            // Render a light purple tint for "Oolio" so the #673ab6 intent reads clearly
            using var oolioBrush = new SolidBrush(Color.FromArgb(220, 195, 255));
            g.DrawString("Oolio", oolioFont, oolioBrush, new PointF(16, nameY));

            // Measure "Oolio" width to position "Cloudflared" right after on same line
            var oolioSize = g.MeasureString("Oolio", oolioFont);
            using var cfFont = new Font("Segoe UI", 14f, FontStyle.Bold, GraphicsUnit.Point);
            using var cfBrush = new SolidBrush(Color.White);
            g.DrawString(" ZeroTrust", cfFont, cfBrush, new PointF(16 + oolioSize.Width - 4, nameY));

            // Subtitle line
            using var subFont = new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);
            using var subBrush = new SolidBrush(Color.FromArgb(180, 220, 200, 255));
            g.DrawString("Tunnel Monitor", subFont, subBrush, new PointF(16, nameY + 24));
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
        private readonly ToolTip _toolTip = new ToolTip();

        public string Token     => _txtToken.Text.Trim();
        public string? TunnelNameFound { get; private set; }

        public TokenDialog(string tunnelId)
        {
            _tunnelId = tunnelId;

            Text            = "Cloudflare API Token Required";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            StartPosition   = FormStartPosition.CenterParent;
            ClientSize      = new Size(520, 230);
            BackColor       = Color.FromArgb(241, 245, 249);
            Font            = new Font("Segoe UI", 9.5f);

            // Title: "Paste the Cloudflare" then coloured "API token" with tooltip
            var lblTitle = new Label
            {
                Text      = "Paste the Cloudflare ",
                Location  = new Point(20, 20),
                AutoSize  = true,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font      = new Font("Segoe UI Semibold", 10f, FontStyle.Bold)
            };

            var lblApiToken = new Label
            {
                Text      = "API token",
                Location  = new Point(20 + TextRenderer.MeasureText("Paste the Cloudflare ", lblTitle.Font).Width - 6, 20),
                AutoSize  = true,
                ForeColor = Color.FromArgb(103, 58, 182),   // #673ab6
                Font      = new Font("Segoe UI Semibold", 10f, FontStyle.Bold | FontStyle.Underline),
                Cursor    = Cursors.Help
            };

            // Tooltip explaining where to find the token
            _toolTip.SetToolTip(lblApiToken,
                "The Cloudflare API token can be found in:\r\n" +
                "  • LastPass (search 'Cloudflare API')\r\n" +
                "  • HubSpot → Company Record →\r\n" +
                "    Network & Environment section");
            _toolTip.AutoPopDelay = 8000;
            _toolTip.InitialDelay = 300;
            _toolTip.ReshowDelay  = 300;
            _toolTip.ToolTipTitle = "Where to find the API token";
            _toolTip.ToolTipIcon  = ToolTipIcon.Info;

            var lblNote = new Label
            {
                Text      = "The token is used once and never saved to disk.",
                Location  = new Point(20, 46),
                Size      = new Size(480, 18),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            _txtToken = new TextBox
            {
                Location      = new Point(20, 74),
                Size          = new Size(480, 26),
                UseSystemPasswordChar = true,
                Font          = new Font("Cascadia Mono", 9.5f)
            };
            _txtToken.TextChanged += (_, _) => { _lblStatus.Text = string.Empty; };

            var chkShow = new CheckBox
            {
                Text     = "Show token",
                Location = new Point(20, 108),
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

            // ── Test Token button (dark, rounded) ─────────────────────────────
            _btnTest = new RoundedDialogButton
            {
                Text      = "Test Token",
                Location  = new Point(20, 176),
                Size      = new Size(110, 34),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White
            };
            _btnTest.Click += BtnTest_Click;

            // ── Repair button (#673ab6, rounded) ──────────────────────────────
            _btnOk = new RoundedDialogButton
            {
                Text         = "Repair",
                Location     = new Point(290, 176),
                Size         = new Size(100, 34),
                DialogResult = DialogResult.OK,
                BackColor    = Color.FromArgb(103, 58, 182),   // #673ab6
                ForeColor    = Color.White
            };

            // ── Cancel button (outlined, rounded) ────────────────────────────
            _btnCancel = new RoundedDialogButton
            {
                Text         = "Cancel",
                Location     = new Point(400, 176),
                Size         = new Size(100, 34),
                DialogResult = DialogResult.Cancel,
                BackColor    = Color.FromArgb(241, 245, 249),
                ForeColor    = Color.FromArgb(30, 41, 59),
                DrawBorder   = true
            };

            Controls.AddRange(new Control[] { lblTitle, lblApiToken, lblNote, _txtToken, chkShow, _lblStatus, _btnTest, _btnOk, _btnCancel });
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
                TunnelNameFound = tunnel?.Name;
                _lblStatus.Text      = $"OK – tunnel: {tunnel?.Name ?? _tunnelId}";
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

    // ── Rounded dialog button (used inside TokenDialog) ───────────────────────

    internal sealed class RoundedDialogButton : Button
    {
        public bool DrawBorder { get; set; }

        public RoundedDialogButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor    = Cursors.Hand;
            Font      = new Font("Segoe UI", 9.5f);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var path = RoundRect(ClientRectangle, 7);
            using var bg   = new SolidBrush(BackColor);
            g.FillPath(bg, path);

            if (DrawBorder)
            {
                using var pen = new Pen(Color.FromArgb(203, 213, 225), 1.5f);
                g.DrawPath(pen, path);
            }

            var tf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var fg = new SolidBrush(ForeColor);
            g.DrawString(Text, Font, fg, new RectangleF(0, 0, Width, Height), tf);
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
                lblTunnelName.Text = status.TunnelName ?? "-";
                lblTunnelId.Text   = status.TunnelId   ?? "-";

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

        public async Task RepairAsync()
        {
            if (_currentStatus?.TunnelId == null)
            {
                LogError("Cannot repair: no tunnel ID detected. Please refresh first.");
                return;
            }

            using var dlg = new TokenDialog(_currentStatus.TunnelId);
            if (dlg.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.Token))
            {
                LogInfo("Repair cancelled.");
                return;
            }

            // If the user tested the token, we may already have the tunnel name
            if (!string.IsNullOrWhiteSpace(dlg.TunnelNameFound))
            {
                lblTunnelName.Text = dlg.TunnelNameFound;
                if (_currentStatus != null)
                    _currentStatus.TunnelName = dlg.TunnelNameFound;
            }

            var api = new CloudflareApi(dlg.Token);

            btnRepair.Enabled  = false;
            btnRefresh.Enabled = false;
            btnExport.Enabled  = false;

            try
            {
                var tunnelId = _currentStatus.TunnelId;
                LogInfo($"Repairing tunnel {tunnelId}...");

                // Fetch tunnel name if not already known
                if (string.IsNullOrWhiteSpace(lblTunnelName.Text) || lblTunnelName.Text == "-")
                {
                    try
                    {
                        using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                        var t = await api.GetTunnelAsync(tunnelId, cts2.Token);
                        if (t?.Name != null)
                        {
                            lblTunnelName.Text = t.Name;
                            if (_currentStatus != null) _currentStatus.TunnelName = t.Name;
                        }
                    }
                    catch { /* best effort */ }
                }

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
