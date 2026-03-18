using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Http;
using System.Text.Json;
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
            const int r = 10;
            using var bgPath  = RoundRectPath(new Rectangle(0, 0, Width - 1, Height - 1), r);
            using var bgBrush = new SolidBrush(Color.FromArgb(241, 245, 249));
            g.FillPath(bgBrush, bgPath);
            using var subFont = new Font("Segoe UI", 9f);
            var subSize = g.MeasureString(Subtitle, subFont);
            int subH    = (int)subSize.Height + 6;
            const int padX = 10, padTop = 8;
            float logoH = Height - padTop - subH - 4;
            float logoW = Width  - padX * 2;
            const float svgW = 926f, svgH = 242f;
            float scale   = Math.Min(logoW / svgW, logoH / svgH);
            g.TranslateTransform(padX, padTop + (logoH - svgH * scale) / 2f);
            g.ScaleTransform(scale, scale);
            using var brush = new SolidBrush(Color.FromArgb(103, 58, 182));
            DrawDonut(g, brush, 684,  0, 242, 242, 80, 81, 80);
            DrawRect (g, brush, 594,  0,  80, 242);
            DrawRect (g, brush, 414,  0,  70, 242);
            DrawRect (g, brush, 494, 172,  90,  70);
            DrawDonut(g, brush, 160,  0, 244, 242, 80, 81, 80);
            DrawDonut(g, brush,   0,  0, 242, 242, 80, 81, 80);
            g.ResetTransform();
            var subRect = new RectangleF(0, Height - subH - 2, Width, subH);
            using var subBrush = new SolidBrush(Color.FromArgb(80, 95, 115));
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(Subtitle, subFont, subBrush, subRect, sf);
        }

        private static void DrawDonut(Graphics g, Brush brush, float ox, float oy, float ow, float oh, float inset, float hx, float hy)
        {
            using var path = new GraphicsPath(FillMode.Alternate);
            path.AddEllipse(ox, oy, ow, oh);
            path.AddEllipse(ox + hx, oy + hy, ow - inset * 2, oh - inset * 2);
            g.FillPath(brush, path);
        }

        private static void DrawRect(Graphics g, Brush brush, float x, float y, float w, float h)
            => g.FillRectangle(brush, x, y, w, h);

        private static GraphicsPath RoundRectPath(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X,         r.Y,          d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
            path.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // ── Custom button ─────────────────────────────────────────────────────────

    internal sealed class ModernButton : Button
    {
        private static readonly Color _normal = Color.FromArgb(45, 52, 68);
        private static readonly Color _hover  = Color.FromArgb(60, 68, 88);
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
            g.Clear(Color.FromArgb(39, 46, 63));
            using var path  = RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
            using var brush = new SolidBrush(BackColor);
            g.FillPath(brush, path);
            using var ab = new SolidBrush(_accent);
            g.FillRectangle(ab, new Rectangle(0, Radius, 3, Height - Radius * 2));
            var tf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
            using var fg = new SolidBrush(ForeColor);
            g.DrawString(Text, Font, fg, new RectangleF(Padding.Left + 4, 0, Width - Padding.Left - 8, Height), tf);
        }

        private static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X,         r.Y,          d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
            path.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // ── Rounded card panel ────────────────────────────────────────────────

    internal sealed class RoundedPanel : Panel
    {
        private const int Radius = 10;

        public RoundedPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw   = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width > 0 && Height > 0)
            {
                using var path = RRP(new Rectangle(0, 0, Width, Height), Radius);
                Region = new Region(path);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            for (int i = 3; i >= 1; i--)
            {
                var sr = new Rectangle(i, i, Width - i * 2, Height - i * 2);
                using var sb = new SolidBrush(Color.FromArgb(18, 0, 0, 0));
                using var sp = RRP(sr, Radius);
                g.FillPath(sb, sp);
            }
            using var wb = new SolidBrush(Color.White);
            using var wp = RRP(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
            g.FillPath(wb, wp);
        }

        private static GraphicsPath RRP(Rectangle r, int radius)
        {
            int d = radius * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X,         r.Y,          d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
            p.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
            p.CloseFigure();
            return p;
        }
    }

    internal static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect, int radius)
        {
            using var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X,         rect.Y,          d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y,          d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d,   0, 90);
            path.AddArc(rect.X,         rect.Bottom - d, d, d,  90, 90);
            path.CloseFigure();
            g.FillPath(brush, path);
        }
    }

    // ── Tray icon

    internal static class TrayIconGenerator
    {
        public static System.Drawing.Icon CreateOolioIcon()
        {
            using var bmp = new System.Drawing.Bitmap(32, 32);
            using var g   = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var brush = new SolidBrush(Color.FromArgb(103, 58, 182));
            DrawD(g, brush,  1, 4, 13, 13, 4, 4);
            DrawD(g, brush, 15, 4, 13, 13, 4, 4);
            return System.Drawing.Icon.FromHandle(bmp.GetHicon());
        }

        private static void DrawD(Graphics g, Brush b, float ox, float oy, float ow, float oh, float ins, float hIns)
        {
            using var p = new GraphicsPath(FillMode.Alternate);
            p.AddEllipse(ox, oy, ow, oh);
            p.AddEllipse(ox + ins, oy + hIns, ow - ins * 2, oh - hIns * 2);
            g.FillPath(b, p);
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

        // Timestamp: yy-MM-dd:HH-mm-ss 24hr local
        private static string Ts() => DateTime.Now.ToString("yy-MM-dd:HH-mm-ss");

        private void LogInfo(string message)
        {
            AppendLog($"{Ts()} {message}");
            _logger.Info(message);
        }

        private void LogWarn(string message)
        {
            AppendLog($"{Ts()} WARN: {message}");
            _logger.Warn(message);
        }

        private void LogError(string message, Exception? ex = null)
        {
            string detail = ex == null ? message : $"{message} - {ex.Message}";
            AppendLog($"{Ts()} ERROR: {detail}");
            if (ex == null) _logger.Error(message); else _logger.Error(message, ex);
        }

        private void AppendLog(string line)
        {
            _uiLogs.Add(line);
            if (txtLog.InvokeRequired) txtLog.Invoke(() => txtLog.AppendText(line + Environment.NewLine));
            else txtLog.AppendText(line + Environment.NewLine);
        }

        private static Color BadgeColour(string? value, bool isService = false)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "-") return _slate;
            var v = value.ToLowerInvariant();
            if (isService) return v == "running" ? _green : v == "stopped" ? _red : _amber;
            return v is "healthy" or "active" or "connected" ? _green
                 : v is "inactive" or "degraded"             ? _amber : _slate;
        }

        private void ApplyBadge(Label lbl, string text, bool isService = false)
        {
            lbl.Text      = text;
            lbl.ForeColor = BadgeColour(text, isService);
        }

        private string GetToken() => txtApiToken.Text.Trim();
        private bool   HasToken() => !string.IsNullOrWhiteSpace(txtApiToken.Text);

        // ── OPEN LOG FOLDER
        public void OpenLogFolder()
        {
            try { Process.Start("explorer.exe", _logger.LogDirectory); }
            catch (Exception ex) { LogError("Could not open log folder", ex); }
        }

        // ── TEST TOKEN
        public async Task TestTokenAsync()
        {
            if (!HasToken()) { LogWarn("No API token entered."); return; }
            LogInfo("Testing API token...");
            btnTestToken.Enabled = false;
            try
            {
                var api      = new CloudflareApi(GetToken());
                var tunnelId = _currentStatus?.TunnelId;
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                if (tunnelId != null)
                {
                    var tunnel = await api.GetTunnelAsync(tunnelId, cts.Token);
                    LogInfo($"Token OK (read) – tunnel: {tunnel?.Name ?? tunnelId}");
                }
                else
                {
                    LogInfo("Token appears valid. Refresh first to verify against a tunnel.");
                }
                try
                {
                    if (tunnelId != null)
                    {
                        using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                        await api.GetTunnelTokenAsync(tunnelId, cts2.Token);
                        LogInfo("Token scope: READ + WRITE");
                    }
                }
                catch { LogInfo("Token scope: READ ONLY"); }
            }
            catch (Exception ex) { LogError("Token test failed", ex); }
            finally { btnTestToken.Enabled = true; }
        }

        // ── REFRESH
        public async Task RefreshStatusAsync()
        {
            btnRefresh.Enabled  = false;
            btnRepair.Enabled   = false;
            btnRetrieve.Enabled = false;
            lstIngress.Items.Clear();
            try
            {
                LogInfo("Refreshing service status...");
                var status = await GetLocalStatusAsync();
                _currentStatus = status;
                ApplyBadge(lblService,      status.ServiceState,      isService: true);
                ApplyBadge(lblRemoteStatus, status.RemoteStatus ?? "-");
                lblTunnelName.Text = status.TunnelName ?? "-";
                lblTunnelId.Text   = status.TunnelId   ?? "-";
                if (!string.IsNullOrWhiteSpace(status.DiagnosticsNote)) LogInfo(status.DiagnosticsNote!);
                foreach (var rule in status.Ingress) lstIngress.Items.Add(rule.Display);
                btnRepair.Enabled   = status.TunnelId != null;
                btnRetrieve.Enabled = status.TunnelId != null;
                LogInfo("Refresh complete.");
                await CheckIngressHealthAsync(status.Ingress);
            }
            catch (Exception ex) { LogError("Refresh failed", ex); }
            finally { btnRefresh.Enabled = true; }
        }

        private async Task CheckIngressHealthAsync(List<IngressRuleView> rules)
        {
            var urls = new List<string>();
            foreach (var rule in rules)
            {
                var display = rule.Display;
                if (string.IsNullOrWhiteSpace(display) || display.StartsWith("*")) continue;
                var parts = display.Split(new[] { " -> ", " → " }, StringSplitOptions.RemoveEmptyEntries);
                var host  = parts[0].Trim();
                if (!host.StartsWith("http")) host = "https://" + host;
                urls.Add(host);
            }
            if (urls.Count == 0) return;
            LogInfo($"Health checking {urls.Count} endpoint(s)...");
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            foreach (var url in urls)
            {
                try
                {
                    var resp = await http.GetAsync(url);
                    int code = (int)resp.StatusCode;
                    LogInfo($"  {(code >= 200 && code < 300 ? "✓" : "✗")} {url} → HTTP {code}");
                }
                catch (Exception ex) { LogWarn($"  ✗ {url} → {ex.Message}"); }
            }
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
            if (tunnelId == null) status.DiagnosticsNote = "Could not decode tunnel ID from service command line.";
            return Task.FromResult(status);
        }

        // ── RETRIEVE TUNNEL DETAILS
        public async Task RetrieveTunnelDetailsAsync()
        {
            if (!HasToken())                       { LogWarn("Enter an API token first.");     return; }
            if (_currentStatus?.TunnelId == null)  { LogWarn("No tunnel ID – refresh first."); return; }
            btnRetrieve.Enabled = false;
            try
            {
                var api      = new CloudflareApi(GetToken());
                var tunnelId = _currentStatus.TunnelId;
                LogInfo($"Retrieving tunnel details for {tunnelId}...");
                using var cts1  = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var tunnel = await api.GetTunnelAsync(tunnelId, cts1.Token);
                LogInfo($"Tunnel: {tunnel?.Name ?? "-"}  Status: {tunnel?.Status ?? "-"}");
                using var cts2  = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var config = await api.GetTunnelConfigAsync(tunnelId, cts2.Token);
                var ingress = config?.Config?.Ingress ?? new List<CfIngressRule>();
                LogInfo($"Published routes ({ingress.Count}):");
                foreach (var rule in ingress)
                {
                    string host = rule.Hostname ?? "*";
                    string path = string.IsNullOrWhiteSpace(rule.Path) ? "*" : rule.Path;
                    string svc  = rule.Service ?? "-";
                    LogInfo($"  {host}/{path} → {svc}");
                }
                var outDir  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Bepoz", "CloudflaredMonitor", "tunnel-details");
                Directory.CreateDirectory(outDir);
                var outPath = Path.Combine(outDir, $"{tunnelId}.json");
                var export  = new { TunnelId = tunnelId, TunnelName = tunnel?.Name, Status = tunnel?.Status, Retrieved = DateTime.UtcNow.ToString("o"), Routes = ingress.ConvertAll(r => new { r.Hostname, Path = r.Path ?? "*", r.Service }) };
                await File.WriteAllTextAsync(outPath, JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true }));
                LogInfo($"Saved to: {outPath}");
                lstIngress.Items.Clear();
                foreach (var rule in ingress)
                {
                    string host = rule.Hostname ?? "*";
                    string path = string.IsNullOrWhiteSpace(rule.Path) ? "" : "/" + rule.Path;
                    lstIngress.Items.Add($"{host}{path}  →  {rule.Service ?? "-"}");
                }
            }
            catch (Exception ex) { LogError("Retrieve tunnel details failed", ex); }
            finally { btnRetrieve.Enabled = true; }
        }

        // ── REPAIR
        public async Task RepairAsync()
        {
            if (_currentStatus?.TunnelId == null) { LogError("Cannot repair: no tunnel ID. Please refresh first."); return; }
            if (!HasToken())                       { LogWarn("Enter an API token first.");                           return; }
            if (MessageBox.Show(this, "This will stop the cloudflared service and reinstall it. Continue?",
                "Confirm Repair", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            { LogInfo("Repair cancelled."); return; }

            var api = new CloudflareApi(GetToken());
            btnRepair.Enabled   = false;
            btnRefresh.Enabled  = false;
            btnRetrieve.Enabled = false;
            try
            {
                var tunnelId = _currentStatus.TunnelId;
                LogInfo($"Repairing tunnel {tunnelId}...");
                LogInfo("Stopping service...");   _serviceManager.StopServiceBestEffort();
                LogInfo("Killing processes...");  _serviceManager.KillCloudflaredProcess();
                LogInfo("Deleting service...");   _serviceManager.DeleteService();
                if (chkReinstall.Checked)
                {
                    LogInfo("Downloading MSI...");
                    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
                    var msiPath   = await _installer.DownloadMsiAsync(cts.Token);
                    _installer.InstallMsi(msiPath);
                }
                LogInfo("Locating cloudflared exe...");
                var exe = _installer.FindCloudflaredExeOrThrow();
                LogInfo("Requesting new tunnel token...");
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    var newToken = await api.GetTunnelTokenAsync(tunnelId, cts.Token);
                    if (string.IsNullOrWhiteSpace(newToken)) throw new InvalidOperationException("API returned empty token.");
                    LogInfo("Installing service...");
                    _installer.InstallServiceWithToken(exe, newToken);
                }
                LogInfo("Starting service..."); _serviceManager.StartService();
                LogInfo("Repair complete.");    await RefreshStatusAsync();
            }
            catch (Exception ex) { LogError("Repair failed", ex); }
            finally
            {
                btnRefresh.Enabled  = true;
                btnRepair.Enabled   = true;
                btnRetrieve.Enabled = true;
            }
        }

        // ── EXPORT DIAGNOSTICS (zip bundle for support tickets)
        public void ExportDiagnostics()
        {
            try
            {
                if (_currentStatus == null) { MessageBox.Show(this, "Refresh first.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var ingressLines = new List<string>();
                foreach (var item in lstIngress.Items) ingressLines.Add(item?.ToString() ?? string.Empty);
                var zipPath = _exporter.Export(_currentStatus, _uiLogs, ingressLines);
                MessageBox.Show(this, "Diagnostics exported to:" + Environment.NewLine + zipPath, "Export Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(this, $"Export failed: {ex.Message}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // ─ Event handlers
        private async void btnRefresh_Click(object? sender, EventArgs e)  => await RefreshStatusAsync();
        private async void btnRepair_Click(object? sender, EventArgs e)   => await RepairAsync();
        private async void btnRetrieve_Click(object? sender, EventArgs e) => await RetrieveTunnelDetailsAsync();
        private async void btnTestToken_Click(object? sender, EventArgs e) => await TestTokenAsync();
        private void btnOpenLogs_Click(object? sender, EventArgs e)        => OpenLogFolder();
    }
}
