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
    // ── Oolio logo brand control ────────────────────────────────────────────

    internal sealed class OolioLogoBrand : Control
    {
        private const string Subtitle = "ZeroTrust Tunnel Monitor";

        public OolioLogoBrand()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Background pill
            const int r = 10;
            using var bgPath  = RoundRectPath(new Rectangle(0, 0, Width - 1, Height - 1), r);
            using var bgBrush = new SolidBrush(Color.FromArgb(241, 245, 249));
            g.FillPath(bgBrush, bgPath);

            // Subtitle at bottom - fix #2: larger font
            using var subFont = new Font("Segoe UI", 9f);
            var subSize = g.MeasureString(Subtitle, subFont);
            int subH    = (int)subSize.Height + 6;

            // Logo area above subtitle
            const int padX   = 10;
            const int padTop = 8;
            float logoH = Height - padTop - subH - 4;
            float logoW = Width  - padX * 2;

            const float svgW = 926f, svgH = 242f;
            float scale   = Math.Min(logoW / svgW, logoH / svgH);
            float offsetX = padX;
            float offsetY = padTop + (logoH - svgH * scale) / 2f;

            g.TranslateTransform(offsetX, offsetY);
            g.ScaleTransform(scale, scale);

            using var brush = new SolidBrush(Color.FromArgb(103, 58, 182));
            DrawDonut(g, brush, 684,  0, 242, 242, 80, 81, 80);
            DrawRect (g, brush, 594,  0,  80, 242);
            DrawRect (g, brush, 414,  0,  70, 242);
            DrawRect (g, brush, 494, 172,  90,  70);
            DrawDonut(g, brush, 160,  0, 244, 242, 80, 81, 80);
            DrawDonut(g, brush,   0,  0, 242, 242, 80, 81, 80);

            g.ResetTransform();

            // Subtitle
            var subRect = new RectangleF(padX, Height - subH - 2, Width - padX * 2, subH);
            using var subBrush = new SolidBrush(Color.FromArgb(80, 95, 115));
            var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
            g.DrawString(Subtitle, subFont, subBrush, subRect, sf);
        }

        private static void DrawDonut(Graphics g, Brush brush,
            float ox, float oy, float ow, float oh,
            float inset, float hx, float hy)
        {
            float hw = ow - inset * 2;
            float hh = oh - inset * 2;
            using var path = new GraphicsPath(FillMode.Alternate);
            path.AddEllipse(ox,      oy,      ow, oh);
            path.AddEllipse(ox + hx, oy + hy, hw, hh);
            g.FillPath(brush, path);
        }

        private static void DrawRect(Graphics g, Brush brush, float x, float y, float w, float h)
            => g.FillRectangle(brush, x, y, w, h);

        private static GraphicsPath RoundRectPath(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X,          r.Y,          d, d, 180, 90);
            path.AddArc(r.Right - d,  r.Y,          d, d, 270, 90);
            path.AddArc(r.Right - d,  r.Bottom - d, d, d,   0, 90);
            path.AddArc(r.X,          r.Bottom - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // ── Custom button ─────────────────────────────────────────────────────────

    internal sealed class ModernButton : Button
    {
        private static readonly Color _normal = Color.FromArgb(55, 60, 75);
        private static readonly Color _hover  = Color.FromArgb(72, 78, 95);
        private static readonly Color _accent = Color.FromArgb(103, 58, 182);
        private const int Radius = 8;

        public ModernButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = _normal;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);
            Cursor = Cursors.Hand;
            TextAlign = ContentAlignment.MiddleLeft;
            Padding = new Padding(14, 0, 0, 0);
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
        }

        protected override void OnMouseEnter(EventArgs e) { BackColor = _hover;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { BackColor = _normal; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            // Clear to sidebar colour so rounded corners don't show artefacts
            g.Clear(Color.FromArgb(63, 69, 85));
            using var path = RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
            using var brush = new SolidBrush(BackColor);
            g.FillPath(brush, path);
            using var accentBrush = new SolidBrush(_accent);
            g.FillRectangle(accentBrush, new Rectangle(0, Radius, 3, Height - Radius * 2));
            var tf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
            using var fg = new SolidBrush(ForeColor);
            g.DrawString(Text, Font, fg, new RectangleF(Padding.Left + 4, 0, Width - Padding.Left - 8, Height), tf);
        }

        private static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath(); int d = radius * 2;
            path.AddArc(r.X,         r.Y,          d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
            path.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
            path.CloseFigure(); return path;
        }
    }

    // ── Rounded card panel ────────────────────────────────────────────────
    // Fix #1: The trick for proper rounded corners is to NOT use g.Clear() at all.
    // Instead we set the panel region to a rounded rectangle shape, which clips
    // the control to that shape so the parent background shows through the corners.

    internal sealed class RoundedPanel : Panel
    {
        private const int Radius = 10;

        public RoundedPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw   = true;
            // Transparent background so parent bg shows through rounded corners
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // Clip the control to a rounded rectangle - corners are literally gone
            if (Width > 0 && Height > 0)
            {
                using var path = RoundRectPath(new Rectangle(0, 0, Width, Height), Radius);
                Region = new Region(path);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Subtle drop shadow
            for (int i = 3; i >= 1; i--)
            {
                var sr = new Rectangle(i, i, Width - i * 2, Height - i * 2);
                using var sb = new SolidBrush(Color.FromArgb(18, 0, 0, 0));
                using var sp = RoundRectPath(sr, Radius);
                g.FillPath(sb, sp);
            }

            // White face - fills the entire clipped region
            using var wb = new SolidBrush(Color.White);
            using var wp = RoundRectPath(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
            g.FillPath(wb, wp);
        }

        private static GraphicsPath RoundRectPath(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X,          r.Y,          d, d, 180, 90);
            path.AddArc(r.Right - d,  r.Y,          d, d, 270, 90);
            path.AddArc(r.Right - d,  r.Bottom - d, d, d,   0, 90);
            path.AddArc(r.X,          r.Bottom - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect, int radius)
        {
            using var path = new GraphicsPath(); int d = radius * 2;
            path.AddArc(rect.X,         rect.Y,          d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y,          d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d,   0, 90);
            path.AddArc(rect.X,         rect.Bottom - d, d, d,  90, 90);
            path.CloseFigure(); g.FillPath(brush, path);
        }
    }

    // ── Tray icon generator ──────────────────────────────────────────────────
    // Generates a 32x32 "OO" icon in Oolio purple matching the brand image.

    internal static class TrayIconGenerator
    {
        public static Icon CreateOolioIcon()
        {
            const int size = 32;
            using var bmp = new System.Drawing.Bitmap(size, size);
            using var g   = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var brush = new SolidBrush(Color.FromArgb(103, 58, 182));

            // Two 'o' donuts side by side, scaled to 32x32
            // Left O: outer circle at (0,4) size 13x13, inner (3,7) size 7x7
            DrawDonut(g, brush, 1,  4, 13, 13, 4, 4);
            // Right O: outer circle at (14,4) size 13x13
            DrawDonut(g, brush, 15, 4, 13, 13, 4, 4);

            // Bottom stem marks to echo the lettermark feel
            g.FillRectangle(brush, 12, 19, 3, 8);  // tiny 'i' stem
            g.FillRectangle(brush, 18, 19, 3, 8);  // tiny 'i' stem mirror

            return Icon.FromHandle(bmp.GetHicon());
        }

        private static void DrawDonut(Graphics g, Brush brush,
            float ox, float oy, float ow, float oh, float inset, float hInset)
        {
            using var path = new GraphicsPath(FillMode.Alternate);
            path.AddEllipse(ox,           oy,           ow,           oh);
            path.AddEllipse(ox + inset,   oy + hInset,  ow - inset*2, oh - hInset*2);
            g.FillPath(brush, path);
        }
    }

    // ── Token input dialog ────────────────────────────────────────────────

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
            Text = "Cloudflare API Token Required"; FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 230); BackColor = Color.FromArgb(241, 245, 249);
            Font = new Font("Segoe UI", 9.5f);
            var lblTitle = new Label { Text = "Paste your Cloudflare API token:", Location = new Point(20, 20), Size = new Size(480, 20), ForeColor = Color.FromArgb(15, 23, 42), Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold) };
            var toolTip = new ToolTip(); toolTip.SetToolTip(lblTitle, "Found in LastPass or HubSpot → Company Record → Network & Environment");
            var lblNote = new Label { Text = "ⓘ  Found in LastPass or HubSpot → Company Record → Network & Environment", Location = new Point(20, 44), Size = new Size(480, 18), ForeColor = Color.FromArgb(103, 58, 182), Font = new Font("Segoe UI", 8.5f) };
            _txtToken = new TextBox { Location = new Point(20, 72), Size = new Size(480, 26), UseSystemPasswordChar = true, Font = new Font("Cascadia Mono", 9.5f) };
            _txtToken.TextChanged += (_, _) => { _lblStatus.Text = string.Empty; };
            var chkShow = new CheckBox { Text = "Show token", Location = new Point(20, 106), AutoSize = true, ForeColor = Color.FromArgb(100, 116, 139) };
            chkShow.CheckedChanged += (_, _) => { _txtToken.UseSystemPasswordChar = !chkShow.Checked; };
            _lblStatus = new Label { Location = new Point(20, 136), Size = new Size(480, 18), ForeColor = Color.FromArgb(220, 38, 38) };
            _btnTest = new Button { Text = "Test Token", Location = new Point(20, 182), Size = new Size(100, 32), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White };
            _btnTest.FlatAppearance.BorderSize = 0; _btnTest.Region = RoundedRegion(100, 32, 6); _btnTest.Click += BtnTest_Click;
            _btnOk = new Button { Text = "Repair", Location = new Point(300, 182), Size = new Size(100, 32), DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(103, 58, 182), ForeColor = Color.White };
            _btnOk.FlatAppearance.BorderSize = 0; _btnOk.Region = RoundedRegion(100, 32, 6);
            _btnCancel = new Button { Text = "Cancel", Location = new Point(408, 182), Size = new Size(92, 32), DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(241, 245, 249), ForeColor = Color.FromArgb(30, 41, 59) };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225); _btnCancel.Region = RoundedRegion(92, 32, 6);
            Controls.AddRange(new Control[] { lblTitle, lblNote, _txtToken, chkShow, _lblStatus, _btnTest, _btnOk, _btnCancel });
            AcceptButton = _btnOk; CancelButton = _btnCancel;
        }

        private static Region RoundedRegion(int w, int h, int r)
        {
            var path = new GraphicsPath(); int d = r * 2;
            path.AddArc(0,   0,   d, d, 180, 90); path.AddArc(w-d, 0,   d, d, 270, 90);
            path.AddArc(w-d, h-d, d, d,   0, 90); path.AddArc(0,   h-d, d, d,  90, 90);
            path.CloseFigure(); return new Region(path);
        }

        private async void BtnTest_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtToken.Text)) { _lblStatus.Text = "Please paste a token first."; _lblStatus.ForeColor = Color.FromArgb(220, 38, 38); return; }
            _btnTest.Enabled = false; _lblStatus.Text = "Testing..."; _lblStatus.ForeColor = Color.FromArgb(100, 116, 139);
            try { var api = new CloudflareApi(_txtToken.Text.Trim()); using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)); var tunnel = await api.GetTunnelAsync(_tunnelId, cts.Token); _lblStatus.Text = $"✓ Connected to tunnel: {tunnel?.Name ?? _tunnelId}"; _lblStatus.ForeColor = Color.FromArgb(22, 163, 74); }
            catch (Exception ex) { _lblStatus.Text = $"Failed: {ex.Message}"; _lblStatus.ForeColor = Color.FromArgb(220, 38, 38); }
            finally { _btnTest.Enabled = true; }
        }
    }

    // ── Main form ─────────────────────────────────────────────────────────────────

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

        public MainForm() { InitializeComponent(); _exporter = new DiagnosticsExporter(_logger); }

        private void LogInfo(string message) { string line = $"INFO: {message}"; txtLog.AppendText(line + Environment.NewLine); _uiLogs.Add(line); _logger.Info(message); }
        private void LogError(string message, Exception? ex = null) { string line = ex == null ? $"ERROR: {message}" : $"ERROR: {message} - {ex.Message}"; txtLog.AppendText(line + Environment.NewLine); _uiLogs.Add(line); if (ex == null) _logger.Error(message); else _logger.Error(message, ex); }

        private static Color BadgeColour(string? value, bool isService = false)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "-") return _slate;
            var v = value.ToLowerInvariant();
            if (isService) return v == "running" ? _green : v == "stopped" ? _red : _amber;
            return v is "healthy" or "active" or "connected" ? _green : v is "inactive" or "degraded" ? _amber : _slate;
        }

        private void ApplyBadge(Label lbl, string text, bool isService = false) { lbl.Text = text; lbl.ForeColor = BadgeColour(text, isService); }

        public async Task RefreshStatusAsync()
        {
            btnRefresh.Enabled = false; btnRepair.Enabled = false; btnExport.Enabled = false;
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
                if (!string.IsNullOrWhiteSpace(status.DiagnosticsNote)) LogInfo(status.DiagnosticsNote!);
                foreach (var rule in status.Ingress) lstIngress.Items.Add(rule.Display);
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
            if (!_serviceManager.IsInstalled()) { status.ServiceState = "NotInstalled"; status.DiagnosticsNote = "Cloudflared service is not installed."; return Task.FromResult(status); }
            status.ServiceState = _serviceManager.GetStatusText();
            var imagePath = TunnelDiscovery.TryGetServiceImagePath();
            var token     = TunnelDiscovery.TryExtractTokenFromImagePath(imagePath);
            var tunnelId  = TunnelDiscovery.TryDecodeTunnelIdFromToken(token);
            status.TunnelId = tunnelId;
            if (tunnelId == null) status.DiagnosticsNote = "Could not decode tunnel ID from service command line.";
            return Task.FromResult(status);
        }

        public async Task RepairAsync()
        {
            if (_currentStatus?.TunnelId == null) { LogError("Cannot repair: no tunnel ID detected. Please refresh first."); return; }
            using var dlg = new TokenDialog(_currentStatus.TunnelId);
            if (dlg.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.Token)) { LogInfo("Repair cancelled."); return; }
            var api = new CloudflareApi(dlg.Token);
            btnRepair.Enabled = false; btnRefresh.Enabled = false; btnExport.Enabled = false;
            try
            {
                var tunnelId = _currentStatus.TunnelId;
                LogInfo($"Repairing tunnel {tunnelId}...");
                LogInfo("Stopping service..."); _serviceManager.StopServiceBestEffort();
                LogInfo("Killing residual cloudflared processes..."); _serviceManager.KillCloudflaredProcess();
                LogInfo("Deleting service..."); _serviceManager.DeleteService();
                if (chkReinstall.Checked) { LogInfo("Reinstalling cloudflared MSI..."); using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3)); var msiPath = await _installer.DownloadMsiAsync(cts.Token); _installer.InstallMsi(msiPath); }
                LogInfo("Locating cloudflared executable..."); var exe = _installer.FindCloudflaredExeOrThrow();
                LogInfo("Requesting new tunnel token from Cloudflare API...");
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30))) { var newToken = await api.GetTunnelTokenAsync(tunnelId, cts.Token); if (string.IsNullOrWhiteSpace(newToken)) throw new InvalidOperationException("API returned an empty tunnel token."); LogInfo("Installing Windows service with new token..."); _installer.InstallServiceWithToken(exe, newToken); }
                LogInfo("Starting service..."); _serviceManager.StartService();
                LogInfo("Repair complete. Running final refresh..."); await RefreshStatusAsync();
            }
            catch (Exception ex) { LogError("Repair failed", ex); }
            finally { btnRefresh.Enabled = true; btnRepair.Enabled = true; btnExport.Enabled = true; }
        }

        public void ExportDiagnostics()
        {
            try
            {
                if (_currentStatus == null) { MessageBox.Show(this, "Status unknown. Please refresh first.", "Export Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var ingressLines = new List<string>(); foreach (var item in lstIngress.Items) ingressLines.Add(item?.ToString() ?? string.Empty);
                var zipPath = _exporter.Export(_currentStatus, _uiLogs, ingressLines);
                MessageBox.Show(this, "Diagnostics exported to:" + Environment.NewLine + zipPath, "Export Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(this, $"Failed to export diagnostics: {ex.Message}", "Export Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private async void btnRefresh_Click(object? sender, EventArgs e) => await RefreshStatusAsync();
        private async void btnRepair_Click(object? sender, EventArgs e)  => await RepairAsync();
        private void btnExport_Click(object? sender, EventArgs e)         => ExportDiagnostics();
    }
}
