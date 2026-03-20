using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CloudflaredMonitor.Services;

namespace CloudflaredMonitor
{
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

    // Pill badge label - draws a compact rounded pill, transparent background outside pill
    internal sealed class PillLabel : Label
    {
        private const int PillRadius = 9;
        private Color _pillColour = Color.Transparent;

        public Color PillColour
        {
            get => _pillColour;
            set { _pillColour = value; Invalidate(); }
        }

        public PillLabel()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            ForeColor = Color.FromArgb(100, 116, 139);
            TextAlign = ContentAlignment.MiddleLeft;
            AutoSize  = false;
            Font      = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
        }

        // Suppress all base background painting - we handle everything in OnPaint
        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Clear to transparent
            g.Clear(Color.Transparent);

            bool hasPill = _pillColour != Color.Transparent && Text.Length > 0 && Text != "-";

            if (hasPill)
            {
                // Size pill tightly around text
                var sz  = g.MeasureString(Text, Font);
                int pw  = (int)sz.Width  + 24;
                int ph  = (int)sz.Height + 8;
                int px  = 0; // left-aligned pill
                int py  = (Height - ph) / 2;
                var rect = new Rectangle(px, py, pw, ph);
                using var fill = new SolidBrush(_pillColour);
                using var path = PillPath(rect, PillRadius);
                g.FillPath(fill, path);
                using var fg = new SolidBrush(Color.White);
                g.DrawString(Text, Font, fg,
                    new RectangleF(px, py, pw, ph),
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }
            else
            {
                // No pill - just grey text
                using var fg = new SolidBrush(Color.FromArgb(100, 116, 139));
                g.DrawString(Text, Font, fg,
                    new RectangleF(0, 0, Width, Height),
                    new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });
            }
        }

        private static GraphicsPath PillPath(Rectangle r, int rad)
        { int d = rad * 2; var p = new GraphicsPath(); p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p; }
    }

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

    internal sealed class RoundedPanel : Panel
    {
        private const int Radius = 10;
        public RoundedPanel()
        {
            DoubleBuffered = true; ResizeRedraw = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }
        protected override void OnResize(EventArgs e)
        { base.OnResize(e); if (Width > 0 && Height > 0) { using var p = RRP(new Rectangle(0, 0, Width, Height), Radius); Region = new Region(p); } }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            for (int i = 3; i >= 1; i--)
            { using var sb = new SolidBrush(Color.FromArgb(18, 0, 0, 0)); using var sp = RRP(new Rectangle(i, i, Width - i * 2, Height - i * 2), Radius); g.FillPath(sb, sp); }
            using var wb = new SolidBrush(Color.White);
            using var wp = RRP(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
            g.FillPath(wb, wp);
        }
        private static GraphicsPath RRP(Rectangle r, int rad)
        { int d = rad * 2; var p = new GraphicsPath(); p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p; }
    }

    internal static class TrayIconGenerator
    {
        public static System.Drawing.Icon CreateOolioIcon()
        {
            using var bmp = new System.Drawing.Bitmap(32, 32); using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Color.FromArgb(39, 46, 63));
            using var pen = new Pen(Color.White, 3f);
            g.DrawEllipse(pen, 2, 9, 14, 14);
            g.DrawEllipse(pen, 16, 9, 14, 14);
            return System.Drawing.Icon.FromHandle(bmp.GetHicon());
        }
    }

    internal sealed class IngressItem
    {
        public string CloudEndpoint { get; }
        public string LocalEndpoint { get; }
        public IngressItem(string cloud, string local) { CloudEndpoint = cloud; LocalEndpoint = local; }
        public bool IsCatchAll =>
            LocalEndpoint.StartsWith("http_status:", StringComparison.OrdinalIgnoreCase) ||
            (CloudEndpoint == "*" && string.IsNullOrWhiteSpace(LocalEndpoint));
    }

    internal static class IngressHelper
    {
        public static IngressItem? Build(string? hostname, string? path, string? service)
        {
            string svc = service ?? "";
            if (svc.StartsWith("http_status:", StringComparison.OrdinalIgnoreCase)) return null;
            string host = hostname ?? "";
            if (host == "*" && string.IsNullOrWhiteSpace(svc)) return null;
            string cloud = host;
            if (!string.IsNullOrWhiteSpace(path) && path != "*")
                cloud = host.TrimEnd('/') + "/" + path.TrimStart('/');
            return new IngressItem(cloud, svc);
        }
    }

    public partial class MainForm : Form
    {
        private readonly CloudflaredServiceManager _serviceManager = new();
        private readonly CloudflaredInstaller      _installer      = new();
        private readonly FileLogger                _logger         = new();
        private readonly DiagnosticsExporter       _exporter;
        private TunnelServiceStatus? _currentStatus;
        private readonly List<string> _uiLogs = new();
        private const string AppVersion     = "1.1.0.2";
        private const string VersionJsonUrl = "https://raw.githubusercontent.com/BenWfromBepoz/CloudflaredMonitor/main/version.json";

        private static string TunnelDetailsDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Bepoz", "CloudflaredMonitor", "tunnel-details");

        private static string TunnelDetailsPath(string id) =>
            Path.Combine(TunnelDetailsDir, id + ".json");

        public MainForm()
        {
            InitializeComponent();
            _exporter = new DiagnosticsExporter(_logger);
            dgvIngress.CellPainting += DgvIngress_CellPainting;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Reapply header styles after Windows theming has settled
            ApplyGridHeaderStyles();
            _ = LoadTodaysLogAsync();
            _ = CheckTunnelStatusAsync();
        }

        // Force header colours after form is shown - Windows theme can override during init
        private void ApplyGridHeaderStyles()
        {
            dgvIngress.EnableHeadersVisualStyles = false;
            var cc = dgvIngress.Columns["colCloud"];
            var cl = dgvIngress.Columns["colLocal"];
            if (cc != null)
            {
                cc.HeaderCell.Style.BackColor = Color.FromArgb(237, 233, 254);
                cc.HeaderCell.Style.ForeColor = Color.FromArgb(76, 29, 149);
                cc.HeaderCell.Style.Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                cc.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                cc.HeaderCell.Style.Padding   = new Padding(6, 0, 0, 0);
            }
            if (cl != null)
            {
                cl.HeaderCell.Style.BackColor = Color.FromArgb(241, 245, 249);
                cl.HeaderCell.Style.ForeColor = Color.FromArgb(51, 65, 85);
                cl.HeaderCell.Style.Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                cl.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                cl.HeaderCell.Style.Padding   = new Padding(6, 0, 0, 0);
            }
            dgvIngress.Invalidate();
        }

        private void DgvIngress_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            e.PaintBackground(e.ClipBounds, false);
            e.PaintContent(e.ClipBounds);
            e.Handled = true;
        }

        private async Task LoadTodaysLogAsync()
        {
            try
            {
                var logPath = _logger.LogFilePath;
                if (!File.Exists(logPath)) return;
                var lines = await File.ReadAllLinesAsync(logPath);
                if (IsDisposed) return;
                if (txtLog.InvokeRequired)
                    txtLog.BeginInvoke(() => { if (!IsDisposed) { txtLog.Lines = lines; ScrollLogToEnd(); } });
                else { txtLog.Lines = lines; ScrollLogToEnd(); }
            }
            catch { }
        }

        private void ScrollLogToEnd()
        {
            if (IsDisposed) return;
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        private static string Ts() => DateTime.Now.ToString("yy-MM-dd | HH:mm:ss");

        private void LogInfo(string m) { AppendLog(m); _logger.Info(m); }
        private void LogWarn(string m) { AppendLog("WARN: " + m); _logger.Warn(m); }
        private void LogError(string m, Exception? ex = null)
        {
            string detail = ex == null ? m : m + " - " + ex.Message;
            if (ex != null && ex.Message.Contains("403") && ex.Message.Contains("10000"))
                detail += " | TOKEN SCOPE: Needs 'Cloudflare Tunnel:Edit' permission for Repair.";
            AppendLog("ERROR: " + detail);
            if (ex == null) _logger.Error(m); else _logger.Error(m, ex);
        }
        private void AppendLog(string message)
        {
            string line = Ts() + " " + message;
            _uiLogs.Add(line);
            if (IsDisposed) return;
            if (txtLog.InvokeRequired)
                txtLog.BeginInvoke(() => { if (!IsDisposed) { txtLog.AppendText(line + Environment.NewLine); ScrollLogToEnd(); } });
            else { txtLog.AppendText(line + Environment.NewLine); ScrollLogToEnd(); }
        }

        private static Color BadgeColour(string? v, bool svc = false)
        {
            if (string.IsNullOrWhiteSpace(v) || v == "-") return Color.Transparent;
            var s = v.ToLowerInvariant();
            if (svc) return s == "running"                       ? Color.FromArgb(22, 163, 74)
                       : s is "stopped" or "notinstalled"        ? Color.FromArgb(220, 38, 38)
                                                                 : Color.FromArgb(217, 119, 6);
            return s is "healthy" or "active" or "connected" or "reachable"  ? Color.FromArgb(22, 163, 74)
                 : s is "inactive" or "degraded" or "down" or "unreachable"  ? Color.FromArgb(220, 38, 38)
                                                                              : Color.FromArgb(217, 119, 6);
        }

        private static string ServiceTooltip(string? value) =>
            (value ?? "").ToLowerInvariant() switch
            {
                "running"      => "The local Cloudflared service is installed and running.",
                "notinstalled" => "The Cloudflared service is not installed. Use Repair Tunnel or install a new tunnel.",
                "stopped"      => "The Cloudflared service is installed but not running. Use Repair Tunnel to re-initiate it.",
                _              => "Cloudflared service state is unknown."
            };

        private static string RemoteTooltip(string? value) =>
            (value ?? "").ToLowerInvariant() switch
            {
                "healthy" or "active" or "connected" or "reachable" => "URL endpoints are reachable — the tunnel is fully operational.",
                "inactive" or "degraded" or "down" or "unreachable" => "URL endpoints are not reachable. Use Repair Tunnel or install a new tunnel.",
                _                                                     => "Tunnel remote status is unknown or not yet retrieved."
            };

        private void ApplyBadge(PillLabel lbl, string text, bool isService = false)
        {
            lbl.Text       = text;
            lbl.PillColour = BadgeColour(text, isService);
            toolTip.SetToolTip(lbl, isService ? ServiceTooltip(text) : RemoteTooltip(text));
        }

        private string GetToken() => txtApiToken.Text.Trim();
        private bool   HasToken() => !string.IsNullOrWhiteSpace(txtApiToken.Text);

        private void PopulateIngress(IEnumerable<IngressItem> items)
        {
            dgvIngress.Rows.Clear();
            foreach (var item in items)
            {
                if (item.IsCatchAll) continue;
                dgvIngress.Rows.Add(item.CloudEndpoint, item.LocalEndpoint);
            }
        }

        public void OpenLogFolder()
        {
            try { Process.Start("explorer.exe", _logger.LogDirectory); }
            catch (Exception ex) { LogError("Could not open log folder", ex); }
        }

        public void OpenConfigFolder()
        {
            try { Directory.CreateDirectory(TunnelDetailsDir); Process.Start("explorer.exe", TunnelDetailsDir); }
            catch (Exception ex) { LogError("Could not open config folder", ex); }
        }

        private async Task SaveTunnelDetailsAsync(string tunnelId, string? tunnelName, string? status, List<CfIngressRule> ingressRules)
        {
            Directory.CreateDirectory(TunnelDetailsDir);
            await File.WriteAllTextAsync(TunnelDetailsPath(tunnelId), JsonSerializer.Serialize(
                new { TunnelId = tunnelId, TunnelName = tunnelName, Status = status, Retrieved = DateTime.UtcNow.ToString("o"),
                      Routes = ingressRules.ConvertAll(r => new { Hostname = r.Hostname ?? "*", Path = r.Path ?? "*", r.Service }) },
                new JsonSerializerOptions { WriteIndented = true }));
            LogInfo("Config saved: " + TunnelDetailsPath(tunnelId));
        }

        private async Task LoadTunnelDetailsFromJsonAsync(string jsonPath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(jsonPath);
                using var doc = JsonDocument.Parse(json); var root = doc.RootElement;
                if (root.TryGetProperty("TunnelName", out var tn)) lblTunnelName.Text = tn.GetString() ?? "-";
                if (root.TryGetProperty("Status",     out var st)) ApplyBadge(lblRemoteStatus, st.GetString() ?? "-");
                if (root.TryGetProperty("Routes", out var routes))
                {
                    var items = new List<IngressItem>();
                    foreach (var route in routes.EnumerateArray())
                    {
                        string h = route.TryGetProperty("Hostname", out var hp) ? hp.GetString() ?? "" : "";
                        string p = route.TryGetProperty("Path",     out var pp) ? pp.GetString() ?? "" : "";
                        string s = route.TryGetProperty("Service",  out var sp) ? sp.GetString() ?? "" : "";
                        var item = IngressHelper.Build(h, p, s); if (item != null) items.Add(item);
                    }
                    PopulateIngress(items);
                }
            }
            catch { }
        }

        private string? GetFirstEndpointUrl(string tunnelId)
        {
            try
            {
                var jp = TunnelDetailsPath(tunnelId);
                if (!File.Exists(jp)) return null;
                using var doc = JsonDocument.Parse(File.ReadAllText(jp)); var root = doc.RootElement;
                if (!root.TryGetProperty("Routes", out var routes)) return null;
                foreach (var route in routes.EnumerateArray())
                {
                    string svc  = route.TryGetProperty("Service",  out var sp) ? sp.GetString() ?? "" : "";
                    if (svc.StartsWith("http_status:", StringComparison.OrdinalIgnoreCase)) continue;
                    string host = route.TryGetProperty("Hostname", out var hp) ? hp.GetString() ?? "" : "";
                    string path = route.TryGetProperty("Path",     out var pp) ? pp.GetString() ?? "" : "";
                    if (string.IsNullOrEmpty(host) || host == "*") continue;
                    string url = "https://" + host;
                    if (!string.IsNullOrEmpty(path) && path != "*") url += "/" + path.TrimStart('/');
                    return url;
                }
            }
            catch { }
            return null;
        }

        private TunnelServiceStatus GetLocalStatus()
        {
            var status = new TunnelServiceStatus();
            if (!_serviceManager.IsInstalled()) { status.ServiceState = "NotInstalled"; status.DiagnosticsNote = "Cloudflared service is not installed."; return status; }
            status.ServiceState = _serviceManager.GetStatusText();
            var imagePath = TunnelDiscovery.TryGetServiceImagePath();
            var token     = TunnelDiscovery.TryExtractTokenFromImagePath(imagePath);
            var tunnelId  = TunnelDiscovery.TryDecodeTunnelIdFromToken(token);
            status.TunnelId = tunnelId;
            if (tunnelId == null) status.DiagnosticsNote = "Could not decode tunnel ID from service command line.";
            return status;
        }

        public async Task TestTokenAsync()
        {
            if (!HasToken()) { LogWarn("No API token entered."); return; }
            LogInfo("Testing API token..."); btnTestToken.Enabled = false;
            try
            {
                var api = new CloudflareApi(GetToken()); var tid = _currentStatus?.TunnelId;
                using var ctsV = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                try { var v = await api.VerifyTokenAsync(ctsV.Token); LogInfo("Token status: " + (v?.Status ?? "unknown") + (v?.ExpiresOn != null ? " | Expires: " + v.ExpiresOn : " | No expiry")); }
                catch (Exception vEx) { LogWarn("Could not verify via /user/tokens/verify: " + vEx.Message); }
                if (tid != null)
                {
                    using var ctsR = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                    var tunnel = await api.GetTunnelAsync(tid, ctsR.Token);
                    LogInfo("Read access OK - Tunnel: " + (tunnel?.Name ?? tid));
                    try { using var ctsW = new CancellationTokenSource(TimeSpan.FromSeconds(10)); await api.GetTunnelTokenAsync(tid, ctsW.Token); LogInfo("Write access OK - Token scope: READ + WRITE (suitable for Repair)"); }
                    catch (Exception wEx) { if (wEx.Message.Contains("403")) LogWarn("Write access DENIED - READ ONLY. Repair requires 'Cloudflare Tunnel:Edit' permission."); else LogWarn("Write access inconclusive: " + wEx.Message); }
                }
                else LogInfo("Token appears valid. Run Check Tunnel Status to associate with a tunnel.");
            }
            catch (Exception ex) { LogError("Token test failed", ex); }
            finally { btnTestToken.Enabled = true; }
        }

        public async Task CheckTunnelStatusAsync()
        {
            btnTunnelStatus.Enabled = false; dgvIngress.Rows.Clear();
            try
            {
                LogInfo("Checking local service...");
                var localStatus = GetLocalStatus(); _currentStatus = localStatus;
                ApplyBadge(lblService, localStatus.ServiceState, isService: true);
                lblTunnelId.Text = localStatus.TunnelId ?? "-";
                if (!string.IsNullOrWhiteSpace(localStatus.DiagnosticsNote)) LogInfo(localStatus.DiagnosticsNote!);
                if (localStatus.ServiceState == "NotInstalled") { ApplyBadge(lblRemoteStatus, "-"); lblTunnelName.Text = "-"; LogWarn("Service not installed. Use Install New Tunnel or Repair Tunnel."); return; }
                if (localStatus.ServiceState != "Running") { LogWarn("Service installed but not running. Use Repair Tunnel."); return; }
                LogInfo("Service is running.");
                var tid = localStatus.TunnelId;
                if (tid != null)
                {
                    var jp = TunnelDetailsPath(tid);
                    if (File.Exists(jp)) { await LoadTunnelDetailsFromJsonAsync(jp); LogInfo("Loaded cached tunnel details."); }
                    var endpointUrl = GetFirstEndpointUrl(tid);
                    if (endpointUrl != null)
                    {
                        LogInfo("Pinging endpoint: " + endpointUrl + " ...");
                        try
                        {
                            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                            var resp = await http.GetAsync(endpointUrl, HttpCompletionOption.ResponseHeadersRead);
                            int code = (int)resp.StatusCode;
                            if (code >= 200 && code < 500) { ApplyBadge(lblRemoteStatus, "Reachable"); LogInfo("Endpoint responded (" + code + ") — tunnel appears reachable."); }
                            else { ApplyBadge(lblRemoteStatus, "Unreachable"); LogWarn("Endpoint returned " + code + " — tunnel may be down."); }
                        }
                        catch (Exception pingEx) { ApplyBadge(lblRemoteStatus, "Unreachable"); LogWarn("Endpoint ping failed: " + pingEx.Message); }
                        if (!HasToken()) LogInfo("No API token — endpoint ping only. Add a token for detailed tunnel status.");
                    }
                    else if (!HasToken()) LogInfo("No cached routes and no API token. Add a token then re-run to fetch full details.");
                }
                if (HasToken() && tid != null)
                {
                    LogInfo("API token found — fetching authoritative tunnel status...");
                    var api = new CloudflareApi(GetToken());
                    using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    var tunnel = await api.GetTunnelAsync(tid, cts1.Token);
                    if (tunnel != null) { lblTunnelName.Text = tunnel.Name ?? "-"; ApplyBadge(lblRemoteStatus, tunnel.Status ?? "-"); LogInfo("Tunnel: " + tunnel.Name + "  Status: " + tunnel.Status); }
                    using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    var config  = await api.GetTunnelConfigAsync(tid, cts2.Token);
                    var ingress = config?.Config?.Ingress ?? new List<CfIngressRule>();
                    var items   = ingress.Select(r => IngressHelper.Build(r.Hostname, r.Path, r.Service)).Where(i => i != null).Cast<IngressItem>().ToList();
                    PopulateIngress(items);
                    await SaveTunnelDetailsAsync(tid, tunnel?.Name, tunnel?.Status, ingress);
                    LogInfo("Tunnel details refreshed and saved (" + items.Count + " route(s)).");
                }
                LogInfo("Check complete.");
            }
            catch (Exception ex) { LogError("Check tunnel status failed", ex); }
            finally { btnTunnelStatus.Enabled = true; }
        }

        public async Task CreateTunnelAsync()
        {
            if (!HasToken()) { LogWarn("Enter an API token first."); return; }
            using var dlg = new CreateTunnelForm();
            if (dlg.ShowDialog(this) != DialogResult.OK || dlg.Result == null) { LogInfo("Install tunnel cancelled."); return; }
            var spec = dlg.Result; LogInfo("Creating tunnel: " + spec.TunnelName);
            var api = new CloudflareApi(GetToken());
            try
            {
                using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var tunnel = await api.CreateTunnelAsync(spec.TunnelName, cts1.Token);
                if (tunnel?.Id == null) throw new InvalidOperationException("Tunnel creation returned no ID.");
                LogInfo("Tunnel created: " + tunnel.Name + " (" + tunnel.Id + ")");
                var ingressRules = spec.Routes.Select(r => new CfIngressRule { Hostname = r.Hostname, Path = string.IsNullOrEmpty(r.Path) ? null : r.Path, Service = r.Service }).ToList();
                using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await api.PutTunnelConfigAsync(tunnel.Id, ingressRules, cts2.Token);
                LogInfo("Configured " + ingressRules.Count + " route(s).");
                await SaveTunnelDetailsAsync(tunnel.Id, tunnel.Name, tunnel.Status ?? "pending", ingressRules);
                PopulateIngress(ingressRules.Select(r => IngressHelper.Build(r.Hostname, r.Path, r.Service)).Where(i => i != null).Cast<IngressItem>());
                lblTunnelName.Text = tunnel.Name ?? "-"; ApplyBadge(lblRemoteStatus, "pending");
                if (MessageBox.Show(this, "Tunnel '" + spec.TunnelName + "' created!\n\nTunnel ID: " + tunnel.Id + "\n\nInstall cloudflared on this machine now?",
                    "Tunnel Created", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    LogInfo("Fetching tunnel token...");
                    using var cts3 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    var token = await api.GetTunnelTokenAsync(tunnel.Id, cts3.Token) ?? throw new InvalidOperationException("API returned empty token.");
                    LogInfo("Downloading MSI..."); using var dlc = new CancellationTokenSource(TimeSpan.FromMinutes(3));
                    var msiPath = await _installer.DownloadMsiAsync(dlc.Token);
                    LogInfo("Uninstalling any existing cloudflared..."); await Task.Run(() => _installer.UninstallExistingMsi());
                    LogInfo("Installing MSI..."); await Task.Run(() => _installer.InstallMsi(msiPath)); try { File.Delete(msiPath); } catch { }
                    var exe = _installer.FindCloudflaredExeOrThrow();
                    LogInfo("Installing service..."); await Task.Run(() => _installer.InstallServiceWithToken(exe, token));
                    _serviceManager.StartService(); LogInfo("Installation complete."); await CheckTunnelStatusAsync();
                }
            }
            catch (Exception ex) { LogError("Install tunnel failed", ex); }
        }

        public async Task RepairAsync()
        {
            if (!HasToken()) { LogWarn("Enter an API token first."); return; }
            if (MessageBox.Show(this, "This will stop, uninstall and reinstall the cloudflared service.\n\nContinue?",
                    "Confirm Repair", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            { LogInfo("Repair cancelled."); return; }
            var api = new CloudflareApi(GetToken()); btnRepair.Enabled = false;
            try
            {
                string? tid = _currentStatus?.TunnelId;
                if (tid == null)
                {
                    if (Directory.Exists(TunnelDetailsDir))
                    {
                        var files = Directory.GetFiles(TunnelDetailsDir, "*.json");
                        if (files.Length == 1) { tid = Path.GetFileNameWithoutExtension(files[0]); LogInfo("Using cached tunnel ID: " + tid); }
                        else if (files.Length > 1) { LogWarn("Multiple cached tunnels found. Run Check Tunnel Status first."); return; }
                    }
                }
                if (tid == null) { LogError("No tunnel ID found. Run Check Tunnel Status or install a new tunnel first."); return; }
                LogInfo("Starting repair for tunnel " + tid + "...");
                LogInfo("Step 1/8  Stopping cloudflared service..."); _serviceManager.StopServiceBestEffort();
                LogInfo("Step 2/8  Force-killing cloudflared.exe processes..."); _serviceManager.KillCloudflaredProcess();
                LogInfo("Step 3/8  Removing service from SCM..."); _serviceManager.DeleteService();
                LogInfo("Step 4/8  Uninstalling existing cloudflared MSI..."); await Task.Run(() => _installer.UninstallExistingMsi()); LogInfo("          Uninstall complete (or no existing install found).");
                LogInfo("Step 5/8  Downloading latest cloudflared MSI...");
                string msiPath; using (var dlc = new CancellationTokenSource(TimeSpan.FromMinutes(5))) msiPath = await _installer.DownloadMsiAsync(dlc.Token);
                LogInfo("          Installing MSI (may take up to 90 seconds)..."); await Task.Run(() => _installer.InstallMsi(msiPath)); try { File.Delete(msiPath); } catch { } LogInfo("          MSI installed successfully.");
                LogInfo("Step 6/8  Locating cloudflared executable..."); var exe = _installer.FindCloudflaredExeOrThrow(); LogInfo("          Found: " + exe);
                LogInfo("Step 7/8  Requesting fresh tunnel token...");
                string newToken; using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    newToken = await api.GetTunnelTokenAsync(tid, cts.Token) ?? throw new InvalidOperationException("API returned empty token. Ensure token has Cloudflare Tunnel:Edit permission.");
                LogInfo("          Token received.");
                LogInfo("Step 8/8  Installing cloudflared Windows service..."); await Task.Run(() => _installer.InstallServiceWithToken(exe, newToken));
                _serviceManager.StartService(); LogInfo("          Service started.");
                LogInfo("Repair complete. Running status refresh..."); await CheckTunnelStatusAsync();
            }
            catch (Exception ex) { LogError("Repair failed", ex); }
            finally { btnRepair.Enabled = true; }
        }

        public void ExportDiagnostics()
        {
            try
            {
                if (_currentStatus == null) { MessageBox.Show(this, "Check tunnel status first.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var lines = new List<string>();
                foreach (DataGridViewRow row in dgvIngress.Rows) lines.Add((row.Cells[0].Value?.ToString() ?? "") + "  ->  " + (row.Cells[1].Value?.ToString() ?? ""));
                var zipPath = _exporter.Export(_currentStatus, _uiLogs, lines);
                MessageBox.Show(this, "Exported to:" + Environment.NewLine + zipPath, "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(this, "Export failed: " + ex.Message, "Export", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private async void btnTunnelStatus_Click(object? sender, EventArgs e) => await CheckTunnelStatusAsync();
        private async void btnTestToken_Click(object? sender, EventArgs e)     => await TestTokenAsync();
        private void btnOpenLogs_Click(object? sender, EventArgs e)             => OpenLogFolder();
        private void btnOpenConfig_Click(object? sender, EventArgs e)           => OpenConfigFolder();
        private async void btnRepair_Click(object? sender, EventArgs e)         => await RepairAsync();
        private async void btnCreateTunnel_Click(object? sender, EventArgs e)   => await CreateTunnelAsync();
    }
}
