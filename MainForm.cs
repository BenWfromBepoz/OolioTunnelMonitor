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
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            const int r = 10;
            using var bgPath  = RRP(new Rectangle(0, 0, Width - 1, Height - 1), r);
            using var bgBrush = new SolidBrush(Color.FromArgb(241, 245, 249));
            g.FillPath(bgBrush, bgPath);
            using var subFont = new Font("Segoe UI", 9f);
            int subH = (int)g.MeasureString(Subtitle, subFont).Height + 6;
            const int padX = 10, padTop = 8;
            float logoH = Height - padTop - subH - 4, logoW = Width - padX * 2;
            float scale = Math.Min(logoW / 926f, logoH / 242f);
            g.TranslateTransform(padX, padTop + (logoH - 242f * scale) / 2f);
            g.ScaleTransform(scale, scale);
            using var brush = new SolidBrush(Color.FromArgb(103, 58, 182));
            DrawDonut(g, brush, 684, 0, 242, 242, 80, 81, 80);
            DrawRect(g, brush, 594, 0, 80, 242);
            DrawRect(g, brush, 414, 0, 70, 242);
            DrawRect(g, brush, 494, 172, 90, 70);
            DrawDonut(g, brush, 160, 0, 244, 242, 80, 81, 80);
            DrawDonut(g, brush, 0, 0, 242, 242, 80, 81, 80);
            g.ResetTransform();
            using var sb2 = new SolidBrush(Color.FromArgb(80, 95, 115));
            g.DrawString(Subtitle, subFont, sb2, new RectangleF(0, Height - subH - 2, Width, subH),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        }
        private static void DrawDonut(Graphics g, Brush b, float ox, float oy, float ow, float oh, float ins, float hx, float hy)
        { using var p = new GraphicsPath(FillMode.Alternate); p.AddEllipse(ox, oy, ow, oh); p.AddEllipse(ox + hx, oy + hy, ow - ins * 2, oh - ins * 2); g.FillPath(b, p); }
        private static void DrawRect(Graphics g, Brush b, float x, float y, float w, float h) => g.FillRectangle(b, x, y, w, h);
        private static GraphicsPath RRP(Rectangle r, int rad)
        { int d = rad * 2; var p = new GraphicsPath(); p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p; }
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
            FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0;
            BackColor = _normal; ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f); Cursor = Cursors.Hand;
            TextAlign = ContentAlignment.MiddleLeft; Padding = new Padding(14, 0, 0, 0);
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }
        protected override void OnMouseEnter(EventArgs e) { BackColor = _hover;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { BackColor = _normal; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(39, 46, 63));
            using var path = RR(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
            using var brush = new SolidBrush(BackColor); g.FillPath(brush, path);
            using var ab = new SolidBrush(_accent); g.FillRectangle(ab, new Rectangle(0, Radius, 3, Height - Radius * 2));
            using var fg = new SolidBrush(ForeColor);
            g.DrawString(Text, Font, fg, new RectangleF(Padding.Left + 4, 0, Width - Padding.Left - 8, Height),
                new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap });
        }
        private static GraphicsPath RR(Rectangle r, int rad)
        { int d = rad * 2; var p = new GraphicsPath(); p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p; }
    }

    // ── Rounded card panel ────────────────────────────────────────────────

    internal sealed class RoundedPanel : Panel
    {
        private const int Radius = 10;
        public RoundedPanel() { DoubleBuffered = true; ResizeRedraw = true; SetStyle(ControlStyles.SupportsTransparentBackColor, true); BackColor = Color.Transparent; }
        protected override void OnResize(EventArgs e) { base.OnResize(e); if (Width > 0 && Height > 0) { using var p = RRP(new Rectangle(0, 0, Width, Height), Radius); Region = new Region(p); } }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            for (int i = 3; i >= 1; i--) { using var sb = new SolidBrush(Color.FromArgb(18, 0, 0, 0)); using var sp = RRP(new Rectangle(i, i, Width - i * 2, Height - i * 2), Radius); g.FillPath(sb, sp); }
            using var wb = new SolidBrush(Color.White); using var wp = RRP(new Rectangle(0, 0, Width - 1, Height - 1), Radius); g.FillPath(wb, wp);
        }
        private static GraphicsPath RRP(Rectangle r, int rad)
        { int d = rad * 2; var p = new GraphicsPath(); p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p; }
    }

    internal static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect, int radius)
        { using var p = new GraphicsPath(); int d = radius * 2; p.AddArc(rect.X, rect.Y, d, d, 180, 90); p.AddArc(rect.Right - d, rect.Y, d, d, 270, 90); p.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90); p.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90); p.CloseFigure(); g.FillPath(brush, p); }
    }

    internal static class TrayIconGenerator
    {
        public static System.Drawing.Icon CreateOolioIcon()
        {
            using var bmp = new System.Drawing.Bitmap(32, 32); using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Color.Transparent);
            using var brush = new SolidBrush(Color.FromArgb(103, 58, 182));
            DrawD(g, brush, 1, 4, 13, 13, 4, 4); DrawD(g, brush, 15, 4, 13, 13, 4, 4);
            return System.Drawing.Icon.FromHandle(bmp.GetHicon());
        }
        private static void DrawD(Graphics g, Brush b, float ox, float oy, float ow, float oh, float ins, float hIns)
        { using var p = new GraphicsPath(FillMode.Alternate); p.AddEllipse(ox, oy, ow, oh); p.AddEllipse(ox + ins, oy + hIns, ow - ins * 2, oh - hIns * 2); g.FillPath(b, p); }
    }

    // ── Ingress item model ─────────────────────────────────────────────────────────

    internal sealed class IngressItem
    {
        public string CloudEndpoint { get; }
        public string LocalEndpoint { get; }
        public IngressItem(string cloud, string local) { CloudEndpoint = cloud; LocalEndpoint = local; }

        /// <summary>
        /// Returns true if this is the Cloudflare catch-all rule (http_status:NNN or
        /// a wildcard hostname with no real service).  These are not useful to
        /// show technicians so we filter them out.
        /// </summary>
        public bool IsCatchAll =>
            LocalEndpoint.StartsWith("http_status:", StringComparison.OrdinalIgnoreCase) ||
            (CloudEndpoint == "*" && string.IsNullOrWhiteSpace(LocalEndpoint));
    }

    // ── Ingress helper ───────────────────────────────────────────────────────────────

    internal static class IngressHelper
    {
        /// <summary>
        /// Build a display-ready IngressItem from raw hostname/path/service strings.
        /// - Appends /{path} to the hostname when a non-wildcard path is configured.
        /// - Returns null for catch-all rules (http_status:NNN or bare *).
        /// </summary>
        public static IngressItem? Build(string? hostname, string? path, string? service)
        {
            // Skip the Cloudflare catch-all rule entirely
            string svc = service ?? "";
            if (svc.StartsWith("http_status:", StringComparison.OrdinalIgnoreCase))
                return null;

            string host = hostname ?? "";
            // Skip bare wildcard rows with no real service
            if (host == "*" && string.IsNullOrWhiteSpace(svc))
                return null;

            // Append path to hostname if it's a real path (not * and not empty)
            string cloudEndpoint = host;
            if (!string.IsNullOrWhiteSpace(path) && path != "*")
                cloudEndpoint = host.TrimEnd('/') + "/" + path.TrimStart('/');

            return new IngressItem(cloudEndpoint, svc);
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

        private const string AppVersion     = "1.0.0";
        private const string VersionJsonUrl = "https://raw.githubusercontent.com/BenWfromBepoz/CloudflaredMonitor/main/version.json";

        private static string TunnelDetailsPath(string tunnelId) =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Bepoz", "CloudflaredMonitor", "tunnel-details", $"{tunnelId}.json");

        public MainForm() { InitializeComponent(); _exporter = new DiagnosticsExporter(_logger); }

        private static string Ts() => DateTime.Now.ToString("yy-MM-dd:HH-mm-ss");

        private void LogInfo(string message)  { AppendLog($"{Ts()} {message}");          _logger.Info(message); }
        private void LogWarn(string message)  { AppendLog($"{Ts()} WARN: {message}");    _logger.Warn(message); }
        private void LogError(string message, Exception? ex = null)
        {
            string detail = ex == null ? message : $"{message} - {ex.Message}";
            AppendLog($"{Ts()} ERROR: {detail}");
            if (ex == null) _logger.Error(message); else _logger.Error(message, ex);
        }
        private void AppendLog(string line)
        {
            _uiLogs.Add(line);
            if (IsDisposed) return;
            if (txtLog.InvokeRequired) txtLog.BeginInvoke(() => { if (!IsDisposed) txtLog.AppendText(line + Environment.NewLine); });
            else txtLog.AppendText(line + Environment.NewLine);
        }

        private static Color BadgeColour(string? value, bool isService = false)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "-") return _slate;
            var v = value.ToLowerInvariant();
            if (isService) return v == "running" ? _green : v == "stopped" ? _red : _amber;
            return v is "healthy" or "active" or "connected" ? _green : v is "inactive" or "degraded" ? _amber : _slate;
        }
        private void ApplyBadge(Label lbl, string text, bool isService = false) { lbl.Text = text; lbl.ForeColor = BadgeColour(text, isService); }

        private string GetToken() => txtApiToken.Text.Trim();
        private bool   HasToken() => !string.IsNullOrWhiteSpace(txtApiToken.Text);

        // Populate the ListView with ingress items, filtering out catch-all rules
        private void PopulateIngress(IEnumerable<IngressItem> items)
        {
            lstIngress.Items.Clear();
            foreach (var item in items)
            {
                if (item.IsCatchAll) continue;
                var lvi = new ListViewItem(item.CloudEndpoint);
                lvi.SubItems.Add(item.LocalEndpoint);
                lstIngress.Items.Add(lvi);
            }
        }

        public void OpenLogFolder()
        {
            try { Process.Start("explorer.exe", _logger.LogDirectory); }
            catch (Exception ex) { LogError("Could not open log folder", ex); }
        }

        // ── CHECK FOR UPDATES
        public async Task CheckForUpdatesAsync(bool silent = false)
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var json    = await http.GetStringAsync(VersionJsonUrl);
                using var doc = JsonDocument.Parse(json);
                var root    = doc.RootElement;
                string latest      = root.TryGetProperty("version",     out var v) ? v.GetString() ?? AppVersion : AppVersion;
                string downloadUrl = root.TryGetProperty("downloadUrl", out var d) ? d.GetString() ?? "" : "";
                if (latest != AppVersion)
                {
                    LogInfo($"Update available: v{latest} (current: v{AppVersion})");
                    var result = MessageBox.Show(this,
                        $"A new version is available: v{latest}\nCurrent: v{AppVersion}\n\nOpen download page?",
                        "Update Available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (result == DialogResult.Yes && !string.IsNullOrWhiteSpace(downloadUrl))
                        Process.Start(new ProcessStartInfo(downloadUrl) { UseShellExecute = true });
                }
                else if (!silent) { LogInfo($"Up to date (v{AppVersion})"); }
            }
            catch (Exception ex) { if (!silent) LogWarn($"Update check failed: {ex.Message}"); }
        }

        // ── TEST TOKEN
        public async Task TestTokenAsync()
        {
            if (!HasToken()) { LogWarn("No API token entered."); return; }
            LogInfo("Testing API token...");
            btnTestToken.Enabled = false;
            try
            {
                var api = new CloudflareApi(GetToken());
                var tunnelId = _currentStatus?.TunnelId;
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                if (tunnelId != null) { var t = await api.GetTunnelAsync(tunnelId, cts.Token); LogInfo($"Token OK (read) – {t?.Name ?? tunnelId}"); }
                else { LogInfo("Token appears valid. Check Service Status first."); }
                try { if (tunnelId != null) { using var c2 = new CancellationTokenSource(TimeSpan.FromSeconds(10)); await api.GetTunnelTokenAsync(tunnelId, c2.Token); LogInfo("Token scope: READ + WRITE"); } }
                catch { LogInfo("Token scope: READ ONLY"); }
            }
            catch (Exception ex) { LogError("Token test failed", ex); }
            finally { btnTestToken.Enabled = true; }
        }

        // ── CHECK SERVICE STATUS
        public async Task RefreshStatusAsync()
        {
            btnRefresh.Enabled      = false;
            btnRepair.Enabled       = false;
            btnRetrieve.Enabled     = false;
            btnTunnelStatus.Enabled = false;
            lstIngress.Items.Clear();
            try
            {
                LogInfo("Checking service status...");
                var status = await GetLocalStatusAsync();
                _currentStatus = status;
                ApplyBadge(lblService, status.ServiceState, isService: true);
                lblTunnelId.Text = status.TunnelId ?? "-";

                if (status.TunnelId != null)
                {
                    var jsonPath = TunnelDetailsPath(status.TunnelId);
                    if (File.Exists(jsonPath)) await LoadTunnelDetailsFromJsonAsync(jsonPath);
                }

                if (!string.IsNullOrWhiteSpace(status.DiagnosticsNote)) LogInfo(status.DiagnosticsNote!);
                btnRepair.Enabled       = status.TunnelId != null;
                btnRetrieve.Enabled     = status.TunnelId != null;
                btnTunnelStatus.Enabled = status.TunnelId != null;
                LogInfo("Service check complete.");
            }
            catch (Exception ex) { LogError("Service check failed", ex); }
            finally { btnRefresh.Enabled = true; }
        }

        private async Task LoadTunnelDetailsFromJsonAsync(string jsonPath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(jsonPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("TunnelName", out var tn)) lblTunnelName.Text = tn.GetString() ?? "-";
                if (root.TryGetProperty("Status",     out var st)) ApplyBadge(lblRemoteStatus, st.GetString() ?? "-");
                if (root.TryGetProperty("Routes",     out var routes))
                {
                    var items = new List<IngressItem>();
                    foreach (var route in routes.EnumerateArray())
                    {
                        string host = route.TryGetProperty("Hostname", out var h) ? h.GetString() ?? "" : "";
                        string path = route.TryGetProperty("Path",     out var p) ? p.GetString() ?? "" : "";
                        string svc  = route.TryGetProperty("Service",  out var s) ? s.GetString() ?? "" : "";
                        var item = IngressHelper.Build(host, path, svc);
                        if (item != null) items.Add(item);
                    }
                    PopulateIngress(items);
                }
            }
            catch { /* stale/corrupt JSON */ }
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

        // ── CHECK TUNNEL STATUS
        public async Task CheckTunnelStatusAsync()
        {
            if (_currentStatus?.TunnelId == null) { LogWarn("Check Service Status first."); return; }
            var tunnelId = _currentStatus.TunnelId;
            btnTunnelStatus.Enabled = false;
            try
            {
                var jsonPath = TunnelDetailsPath(tunnelId);
                if (!File.Exists(jsonPath)) { LogWarn("No cached details. Use Retrieve Tunnel Details first."); return; }
                await LoadTunnelDetailsFromJsonAsync(jsonPath);
                LogInfo("Loaded cached tunnel details.");
                if (HasToken())
                {
                    LogInfo("Refreshing from Cloudflare API...");
                    var api = new CloudflareApi(GetToken());
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                    var tunnel = await api.GetTunnelAsync(tunnelId, cts.Token);
                    if (tunnel != null)
                    {
                        lblTunnelName.Text = tunnel.Name ?? "-";
                        ApplyBadge(lblRemoteStatus, tunnel.Status ?? "-");
                        LogInfo($"Tunnel: {tunnel.Name}  Remote status: {tunnel.Status}");
                        var existingJson = await File.ReadAllTextAsync(jsonPath);
                        using var existingDoc = JsonDocument.Parse(existingJson);
                        var routesList = new List<object>();
                        if (existingDoc.RootElement.TryGetProperty("Routes", out var er))
                            foreach (var route in er.EnumerateArray())
                            {
                                string h = route.TryGetProperty("Hostname", out var hp) ? hp.GetString() ?? "*" : "*";
                                string p = route.TryGetProperty("Path",     out var pp) ? pp.GetString() ?? "*" : "*";
                                string s = route.TryGetProperty("Service",  out var sp) ? sp.GetString() ?? "-" : "-";
                                routesList.Add(new { Hostname = h, Path = p, Service = s });
                            }
                        var updated = new { TunnelId = tunnelId, TunnelName = tunnel.Name, Status = tunnel.Status, Retrieved = DateTime.UtcNow.ToString("o"), Routes = routesList };
                        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(updated, new JsonSerializerOptions { WriteIndented = true }));
                    }
                }
                else { LogInfo("No token – showing cached data only."); }
            }
            catch (Exception ex) { LogError("Check tunnel status failed", ex); }
            finally { btnTunnelStatus.Enabled = true; }
        }

        // ── RETRIEVE TUNNEL DETAILS
        public async Task RetrieveTunnelDetailsAsync()
        {
            if (!HasToken())                      { LogWarn("Enter an API token first."); return; }
            if (_currentStatus?.TunnelId == null) { LogWarn("No tunnel ID – check service status first."); return; }
            btnRetrieve.Enabled = false;
            try
            {
                var api = new CloudflareApi(GetToken()); var tunnelId = _currentStatus.TunnelId;
                LogInfo($"Retrieving tunnel details for {tunnelId}...");
                using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var tunnel = await api.GetTunnelAsync(tunnelId, cts1.Token);
                LogInfo($"Tunnel: {tunnel?.Name ?? "-"}  Status: {tunnel?.Status ?? "-"}");
                using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var config  = await api.GetTunnelConfigAsync(tunnelId, cts2.Token);
                var ingress = config?.Config?.Ingress ?? new List<CfIngressRule>();

                LogInfo($"Published routes ({ingress.Count}):");
                var items = new List<IngressItem>();
                foreach (var rule in ingress)
                {
                    var item = IngressHelper.Build(rule.Hostname, rule.Path, rule.Service);
                    if (item == null) continue;
                    LogInfo($"  {item.CloudEndpoint} → {item.LocalEndpoint}");
                    items.Add(item);
                }
                PopulateIngress(items);
                lblTunnelName.Text = tunnel?.Name ?? "-";
                ApplyBadge(lblRemoteStatus, tunnel?.Status ?? "-");

                var outDir  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Bepoz", "CloudflaredMonitor", "tunnel-details");
                Directory.CreateDirectory(outDir);
                var outPath = Path.Combine(outDir, $"{tunnelId}.json");
                // Save all routes including catch-all so they can be preserved in later status updates
                var export  = new { TunnelId = tunnelId, TunnelName = tunnel?.Name, Status = tunnel?.Status, Retrieved = DateTime.UtcNow.ToString("o"), Routes = ingress.ConvertAll(r => new { r.Hostname, Path = r.Path ?? "*", r.Service }) };
                await File.WriteAllTextAsync(outPath, JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true }));
                LogInfo($"Saved to: {outPath}");
            }
            catch (Exception ex) { LogError("Retrieve tunnel details failed", ex); }
            finally { btnRetrieve.Enabled = true; }
        }

        // ── REPAIR
        public async Task RepairAsync()
        {
            if (_currentStatus?.TunnelId == null) { LogError("No tunnel ID. Check service status first."); return; }
            if (!HasToken()) { LogWarn("Enter an API token first."); return; }
            if (MessageBox.Show(this, "This will stop the cloudflared service and reinstall it. Continue?",
                "Confirm Repair", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) { LogInfo("Repair cancelled."); return; }

            var api = new CloudflareApi(GetToken());
            btnRepair.Enabled = false; btnRefresh.Enabled = false; btnRetrieve.Enabled = false; btnTunnelStatus.Enabled = false;
            try
            {
                var tunnelId = _currentStatus.TunnelId;
                LogInfo($"Repairing tunnel {tunnelId}...");
                LogInfo("Stopping service..."); _serviceManager.StopServiceBestEffort();
                LogInfo("Killing processes..."); _serviceManager.KillCloudflaredProcess();
                LogInfo("Deleting service..."); _serviceManager.DeleteService();

                if (chkReinstall.Checked)
                {
                    LogInfo("Downloading cloudflared MSI...");
                    using var dlCts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
                    var msiPath = await _installer.DownloadMsiAsync(dlCts.Token);
                    LogInfo("Installing MSI (this may take up to 60 seconds)...");
                    try
                    {
                        _installer.InstallMsi(msiPath);
                    }
                    catch (InvalidOperationException msiEx) when (msiEx.Message.Contains("1603"))
                    {
                        // Exit code 1603 = fatal error during installation.
                        // Often caused by a pending reboot or another installer running.
                        throw new InvalidOperationException(
                            "MSI install failed (exit code 1603). This usually means Windows has a " +
                            "pending reboot or another installation is in progress. " +
                            "Please reboot the machine and try again.", msiEx);
                    }
                    finally
                    {
                        try { File.Delete(msiPath); } catch { /* ignore cleanup failure */ }
                    }
                }

                LogInfo("Locating cloudflared executable...");
                var exe = _installer.FindCloudflaredExeOrThrow();
                LogInfo("Requesting new tunnel token from Cloudflare API...");
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    var newToken = await api.GetTunnelTokenAsync(tunnelId, cts.Token);
                    if (string.IsNullOrWhiteSpace(newToken)) throw new InvalidOperationException("API returned empty token.");
                    LogInfo("Installing Windows service...");
                    _installer.InstallServiceWithToken(exe, newToken);
                }
                LogInfo("Starting service..."); _serviceManager.StartService();
                LogInfo("Repair complete."); await RefreshStatusAsync();
            }
            catch (Exception ex) { LogError("Repair failed", ex); }
            finally
            {
                // Always re-enable buttons so the UI is never left frozen
                if (!IsDisposed)
                {
                    btnRefresh.Enabled      = true;
                    btnRepair.Enabled       = true;
                    btnRetrieve.Enabled     = _currentStatus?.TunnelId != null;
                    btnTunnelStatus.Enabled = _currentStatus?.TunnelId != null;
                }
            }
        }

        public void ExportDiagnostics()
        {
            try
            {
                if (_currentStatus == null) { MessageBox.Show(this, "Check service status first.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var lines = new List<string>(); foreach (ListViewItem item in lstIngress.Items) lines.Add($"{item.Text}  →  {item.SubItems[1].Text}");
                var zipPath = _exporter.Export(_currentStatus, _uiLogs, lines);
                MessageBox.Show(this, "Exported to:" + Environment.NewLine + zipPath, "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(this, $"Export failed: {ex.Message}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private async void btnRefresh_Click(object? sender, EventArgs e)      => await RefreshStatusAsync();
        private async void btnRepair_Click(object? sender, EventArgs e)        => await RepairAsync();
        private async void btnRetrieve_Click(object? sender, EventArgs e)      => await RetrieveTunnelDetailsAsync();
        private async void btnTunnelStatus_Click(object? sender, EventArgs e)  => await CheckTunnelStatusAsync();
        private async void btnTestToken_Click(object? sender, EventArgs e)     => await TestTokenAsync();
        private void btnOpenLogs_Click(object? sender, EventArgs e)             => OpenLogFolder();
    }
}
