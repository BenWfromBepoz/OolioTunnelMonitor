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
using OolioTunnelMonitor.Services;

namespace OolioTunnelMonitor
{
    public enum ModernButtonStyle { Primary, Muted }
    public enum AppMode           { Main, Install, Tools, Help }

internal sealed class ContentPanel : Panel
    {
        private const int Radius = 16;
        private static readonly Color _cardBg = Color.FromArgb(226, 232, 240);
        private static readonly Color _formBg = Color.FromArgb(39, 46, 63);
    
        public ContentPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = _cardBg;
            UpdateRegion();
        }
    
        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            UpdateRegion();
        }
    
        private void UpdateRegion()
        {
            Region?.Dispose();
            using var path = ShapeHelper.RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
            Region = new Region(path);
        }
    
    protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.Clear(_formBg);
        
            var rect = new Rectangle(1, 1, Width - 3, Height - 3);
        
            using var path = ShapeHelper.RoundedPath(rect, Radius);
            using var brush = new SolidBrush(_cardBg);
            g.FillPath(brush, path);
        
            using var border = new Pen(Color.FromArgb(35, 255, 255, 255), 1f);
            g.DrawPath(border, path);
        }
    }


    internal static class TrayIconGenerator
    {
        public static System.Drawing.Icon CreateOolioIcon()
        {
            using var bmp = new System.Drawing.Bitmap(32, 32); using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Color.FromArgb(39, 46, 63));
            using var pen = new Pen(Color.White, 3f); g.DrawEllipse(pen, 2, 9, 14, 14); g.DrawEllipse(pen, 16, 9, 14, 14);
            return System.Drawing.Icon.FromHandle(bmp.GetHicon());
        }
    }

    internal sealed class IngressItem
    {
        public string CloudEndpoint { get; } public string LocalEndpoint { get; }
        public IngressItem(string cloud, string local) { CloudEndpoint = cloud; LocalEndpoint = local; }
        public bool IsCatchAll => LocalEndpoint.StartsWith("http_status:", StringComparison.OrdinalIgnoreCase) || (CloudEndpoint == "*" && string.IsNullOrWhiteSpace(LocalEndpoint));
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
            if (!string.IsNullOrWhiteSpace(path) && path != "*") cloud = host.TrimEnd('/') + "/" + path.TrimStart('/');
            return new IngressItem(cloud, svc);
        }
    }

    public partial class MainForm : Form
    {
        private readonly CloudflaredServiceManager _serviceManager = new();
        private readonly CloudflaredInstaller _installer = new();
        private readonly FileLogger _logger = new();
        private readonly DiagnosticsExporter _exporter;
        private TunnelServiceStatus? _currentStatus;
        private readonly List<string> _uiLogs = new();
        private const string AppVersion = "1.2.1.0";
        private const string VersionJsonUrl = "https://raw.githubusercontent.com/BenWfromBepoz/OolioTunnelMonitor/refs/heads/main/version.json";
        private static readonly string _updateCheckFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Bepoz", "OolioTunnelMonitor", "last-update-check.txt");
        private static string TunnelDetailsDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Bepoz", "OolioTunnelMonitor", "tunnel-details");
        private static string TunnelDetailsPath(string id) => Path.Combine(TunnelDetailsDir, id + ".json");

        private AppMode _mode = AppMode.Main;
        private Panel pnlSidebarMain = null!, pnlSidebarInstall = null!, pnlSidebarTools = null!, pnlSidebarHelp = null!;
        private Panel pnlInstall = null!, pnlTools = null!, pnlHelp = null!;
        private ModernButton btnBackFromInstall = null!, btnBackFromTools = null!, btnBackFromHelp = null!;
        private ModernButton btnToolsNav = null!, btnHelpNav = null!;
        private ToggleSwitch tglReinstall = null!;

        public MainForm()
        {
            InitializeComponent();
            _exporter = new DiagnosticsExporter(_logger);
            dgvIngress.CellPainting += DgvIngress_CellPainting;
            this.FormClosing += (_, e) => { e.Cancel = true; Hide(); };
            BuildSidebars(); BuildInstallPanel(); BuildToolsPanel(); BuildHelpPanel();
            SetMode(AppMode.Main); WireNavEvents();
            contentPanel.Visible = false;
        }

        protected override void OnLoad(EventArgs e) { base.OnLoad(e); BeginInvoke(() => { ResizeContentPanel(); contentPanel.Visible = true; }); }
        protected override void OnResize(EventArgs e) { base.OnResize(e); ResizeContentPanel(); }

        private void ResizeContentPanel()
        {
            if (contentPanel == null) return;
            const int sW = 224, m = 10;
            contentPanel.Location = new Point(sW + m, m);
            contentPanel.Size = new Size(ClientSize.Width - sW - m * 2, ClientSize.Height - m * 2);
            int bY = pnlSidebar.Height;
            lblVersion.Location = new Point(14, bY - 24);
            btnCheckUpdates.Location = new Point(12, bY - 62);
        }

        private void SetMode(AppMode mode)
        {
            _mode = mode;
            pnlSidebarMain.Visible = (mode == AppMode.Main); pnlSidebarInstall.Visible = (mode == AppMode.Install);
            pnlSidebarTools.Visible = (mode == AppMode.Tools); pnlSidebarHelp.Visible = (mode == AppMode.Help);
            tblMain.Visible = (mode == AppMode.Main); pnlInstall.Visible = (mode == AppMode.Install);
            pnlTools.Visible = (mode == AppMode.Tools); pnlHelp.Visible = (mode == AppMode.Help);
        }

        private void BuildSidebars()
        {
            const int btnX = 12, btnW = 200, btnH = 40;
            ModernButton BackBtn(string text) => new ModernButton { Text = text, Size = new Size(btnW, btnH), IsBack = true };
            btnToolsNav = new ModernButton { Text = "\u2630  Tools", Size = new Size(btnW, btnH) };
            btnHelpNav = new ModernButton { Text = "?  Help", Size = new Size(btnW, btnH) };
            tglReinstall = new ToggleSwitch { Checked = true, Location = new Point(20, 466) };
            var lblReinstall = new Label { Text = "Reinstall MSI", Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(180, 190, 210), BackColor = Color.Transparent, Location = new Point(66, 470), Size = new Size(140, 20) };
            pnlSidebarMain = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill };
            btnCreateTunnel.Location = new Point(btnX, 220); btnTunnelStatus.Location = new Point(btnX, 268);
            btnToolsNav.Location = new Point(btnX, 316); btnHelpNav.Location = new Point(btnX, 364);
            btnRepair.Location = new Point(btnX, 412);
            pnlSidebarMain.Controls.AddRange(new Control[] { btnCreateTunnel, btnTunnelStatus, btnToolsNav, btnHelpNav, btnRepair, tglReinstall, lblReinstall, btnCheckUpdates, lblVersion });
            pnlSidebar.Controls.Add(pnlSidebarMain);

            pnlSidebarInstall = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill, Visible = false };
            btnBackFromInstall = BackBtn("\u2190  Back to Monitor"); btnBackFromInstall.Location = new Point(btnX, 220);
            pnlSidebarInstall.Controls.Add(btnBackFromInstall); pnlSidebar.Controls.Add(pnlSidebarInstall);

            pnlSidebarTools = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill, Visible = false };
            btnBackFromTools = BackBtn("\u2190  Back to Monitor");
            var btnLogsTools = new ModernButton { Text = "\u2261  Open Logfile Folder", Size = new Size(btnW, btnH) };
            var btnConfigTools = new ModernButton { Text = "\u25a4  Open Config Folder", Size = new Size(btnW, btnH) };
            btnBackFromTools.Location = new Point(btnX, 220); btnLogsTools.Location = new Point(btnX, 268); btnConfigTools.Location = new Point(btnX, 316);
            btnLogsTools.Click += (_, _) => OpenLogFolder(); btnConfigTools.Click += (_, _) => OpenConfigFolder();
            pnlSidebarTools.Controls.AddRange(new Control[] { btnBackFromTools, btnLogsTools, btnConfigTools }); pnlSidebar.Controls.Add(pnlSidebarTools);

            pnlSidebarHelp = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill, Visible = false };
            btnBackFromHelp = BackBtn("\u2190  Back to Monitor"); btnBackFromHelp.Location = new Point(btnX, 220);
            pnlSidebarHelp.Controls.Add(btnBackFromHelp); pnlSidebar.Controls.Add(pnlSidebarHelp);
        }

        private void BuildInstallPanel()
        {
            pnlInstall = new Panel { Visible = false, BackColor = Color.Transparent, Dock = DockStyle.Fill };
            contentPanel.Controls.Add(pnlInstall);
        }

        private void BuildToolsPanel()
        {
            pnlTools = new Panel { Visible = false, BackColor = Color.FromArgb(226, 232, 240), Dock = DockStyle.Fill };
            var header = new Label { Text = "Activity Log", Dock = DockStyle.Top, Height = 40, Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0) };
            var logBox = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.FromArgb(203, 213, 225), Font = new Font("Cascadia Mono", 8.5f), BorderStyle = BorderStyle.None, ScrollBars = RichTextBoxScrollBars.Vertical, WordWrap = false };
            logBox.Text = txtLog.Text;
            txtLog.TextChanged += (_, _) => { if (!pnlTools.IsDisposed) logBox.Text = txtLog.Text; };
            pnlTools.Controls.Add(logBox); pnlTools.Controls.Add(header);
            contentPanel.Controls.Add(pnlTools);
        }

        private void BuildHelpPanel()
        {
            pnlHelp = new Panel { Visible = false, BackColor = Color.FromArgb(226, 232, 240), Dock = DockStyle.Fill, Padding = new Padding(20) };
            var helpCard = new RoundedPanel { Dock = DockStyle.Fill };
            var helpBox = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.White, ForeColor = Color.FromArgb(15, 23, 42), Font = new Font("Segoe UI", 10f), BorderStyle = BorderStyle.None,
                Text = "Oolio Tunnel Monitor v" + AppVersion + "\r\n\r\nA Cloudflare Zero Trust tunnel monitoring and management tool.\r\n\r\nFeatures:\r\n  \u2022 Monitor tunnel status and connectivity\r\n  \u2022 Install and configure new tunnels\r\n  \u2022 Repair and reinstall tunnel services\r\n  \u2022 View published routes and endpoints\r\n\r\nFor support, contact Oolio Group IT or visit the\r\nCloudflare Zero Trust dashboard." };
            helpCard.Controls.Add(helpBox); pnlHelp.Controls.Add(helpCard);
            contentPanel.Controls.Add(pnlHelp);
        }

        private void WireNavEvents()
        {
            btnToolsNav.Click += (_, _) => SetMode(AppMode.Tools); btnHelpNav.Click += (_, _) => SetMode(AppMode.Help);
            btnBackFromInstall.Click += (_, _) => SetMode(AppMode.Main); btnBackFromTools.Click += (_, _) => SetMode(AppMode.Main); btnBackFromHelp.Click += (_, _) => SetMode(AppMode.Main);
        }

        protected override void OnShown(EventArgs e) { base.OnShown(e); ApplyGridHeaderStyles(); _ = LoadTodaysLogAsync(); _ = CheckTunnelStatusAsync(); _ = CheckForUpdatesAsync(silent: true); }

        private void ApplyGridHeaderStyles()
        {
            dgvIngress.EnableHeadersVisualStyles = false;
            var cc = dgvIngress.Columns["colCloud"]; var cl = dgvIngress.Columns["colLocal"];
            if (cc != null) { cc.HeaderCell.Style.BackColor = Color.FromArgb(237,233,254); cc.HeaderCell.Style.ForeColor = Color.FromArgb(76,29,149); cc.HeaderCell.Style.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold); cc.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft; cc.HeaderCell.Style.Padding = new Padding(6,0,0,0); }
            if (cl != null) { cl.HeaderCell.Style.BackColor = Color.FromArgb(241,245,249); cl.HeaderCell.Style.ForeColor = Color.FromArgb(76,29,149); cl.HeaderCell.Style.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold); cl.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft; cl.HeaderCell.Style.Padding = new Padding(6,0,0,0); }
            dgvIngress.Invalidate();
        }

        private void DgvIngress_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e) { if (e.RowIndex < 0) return; e.PaintBackground(e.ClipBounds, false); e.PaintContent(e.ClipBounds); e.Handled = true; }

        private async Task LoadTodaysLogAsync()
        {
            try { var logPath = _logger.LogFilePath; if (!File.Exists(logPath)) return; var lines = await File.ReadAllLinesAsync(logPath); if (IsDisposed) return;
                if (txtLog.InvokeRequired) txtLog.BeginInvoke(() => { if (!IsDisposed) { txtLog.Lines = lines; ScrollLogToEnd(); } }); else { txtLog.Lines = lines; ScrollLogToEnd(); } } catch { }
        }
        private void ScrollLogToEnd() { if (IsDisposed) return; txtLog.SelectionStart = txtLog.TextLength; txtLog.ScrollToCaret(); }
        private static string Ts() => DateTime.Now.ToString("yy-MM-dd | HH:mm:ss");
        private void LogInfo(string m) { AppendLog(m); _logger.Info(m); }
        private void LogWarn(string m) { AppendLog("WARN: " + m); _logger.Warn(m); }
        private void LogError(string m, Exception? ex = null)
        {
            string detail = ex == null ? m : m + " - " + ex.Message;
            if (ex != null && ex.Message.Contains("403") && ex.Message.Contains("10000")) detail += " | TOKEN SCOPE: Needs Cloudflare Tunnel:Edit permission.";
            AppendLog("ERROR: " + detail); if (ex == null) _logger.Error(m); else _logger.Error(m, ex);
        }
        private void AppendLog(string message)
        {
            string line = Ts() + " " + message; _uiLogs.Add(line); if (IsDisposed) return;
            if (txtLog.InvokeRequired) txtLog.BeginInvoke(() => { if (!IsDisposed) { txtLog.AppendText(line + Environment.NewLine); ScrollLogToEnd(); } });
            else { txtLog.AppendText(line + Environment.NewLine); ScrollLogToEnd(); }
        }

        private static Color BadgeColour(string? v, bool svc = false)
        {
            if (string.IsNullOrWhiteSpace(v) || v == "-") return Color.Transparent; var s = v.ToLowerInvariant();
            if (svc) return s == "running" ? Color.FromArgb(16,140,60) : s is "stopped" or "notinstalled" ? Color.FromArgb(200,30,30) : Color.FromArgb(180,100,0);
            return s is "healthy" or "active" or "connected" or "reachable" or "tunnel ok" ? Color.FromArgb(16,140,60) : s is "inactive" or "degraded" or "down" or "unreachable" ? Color.FromArgb(200,30,30) : Color.FromArgb(180,100,0);
        }
        private static string ServiceTooltip(string? v) => (v ?? "").ToLowerInvariant() switch { "running" => "Service is running.", "notinstalled" => "Service not installed.", "stopped" => "Service stopped.", _ => "Unknown." };
        private static string RemoteTooltip(string? v) => (v ?? "").ToLowerInvariant() switch { "healthy" or "active" or "connected" or "reachable" or "tunnel ok" => "Tunnel is reachable.", "inactive" or "degraded" or "down" or "unreachable" => "Tunnel is not reachable.", _ => "Status unknown." };
        private void ApplyBadge(PillLabel lbl, string text, bool isService = false) { lbl.Text = text; lbl.PillColour = BadgeColour(text, isService); toolTip.SetToolTip(lbl, isService ? ServiceTooltip(text) : RemoteTooltip(text)); }
        private string GetToken() => txtApiToken.Text.Trim();
        private bool HasToken() => !string.IsNullOrWhiteSpace(txtApiToken.Text);
        private void PopulateIngress(IEnumerable<IngressItem> items) { dgvIngress.Rows.Clear(); foreach (var item in items) { if (item.IsCatchAll) continue; dgvIngress.Rows.Add(item.CloudEndpoint, item.LocalEndpoint); } }
        public void OpenLogFolder() { try { Process.Start("explorer.exe", _logger.LogDirectory); } catch (Exception ex) { LogError("Could not open log folder", ex); } }
        public void OpenConfigFolder() { try { Directory.CreateDirectory(TunnelDetailsDir); Process.Start("explorer.exe", TunnelDetailsDir); } catch (Exception ex) { LogError("Could not open config folder", ex); } }

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
                var json = await File.ReadAllTextAsync(jsonPath); using var doc = JsonDocument.Parse(json); var root = doc.RootElement;
                if (root.TryGetProperty("TunnelName", out var tn)) lblTunnelName.Text = tn.GetString() ?? "-";
                if (root.TryGetProperty("Status", out var st)) ApplyBadge(lblRemoteStatus, st.GetString() ?? "-");
                if (root.TryGetProperty("Routes", out var routes))
                {
                    var items = new List<IngressItem>();
                    foreach (var route in routes.EnumerateArray())
                    {
                        string h = route.TryGetProperty("Hostname", out var hp) ? hp.GetString() ?? "" : "";
                        string p = route.TryGetProperty("Path", out var pp) ? pp.GetString() ?? "" : "";
                        string s = route.TryGetProperty("Service", out var sp) ? sp.GetString() ?? "" : "";
                        var item = IngressHelper.Build(h, p, s); if (item != null) items.Add(item);
                    }
                    PopulateIngress(items);
                }
            } catch { }
        }

        private string? GetFirstEndpointUrl(string tunnelId)
        {
            try
            {
                var jp = TunnelDetailsPath(tunnelId); if (!File.Exists(jp)) return null;
                using var doc = JsonDocument.Parse(File.ReadAllText(jp)); var root = doc.RootElement;
                if (!root.TryGetProperty("Routes", out var routes)) return null;
                foreach (var route in routes.EnumerateArray())
                {
                    string svc = route.TryGetProperty("Service", out var sp) ? sp.GetString() ?? "" : "";
                    if (svc.StartsWith("http_status:", StringComparison.OrdinalIgnoreCase)) continue;
                    string host = route.TryGetProperty("Hostname", out var hp) ? hp.GetString() ?? "" : "";
                    string path = route.TryGetProperty("Path", out var pp) ? pp.GetString() ?? "" : "";
                    if (string.IsNullOrEmpty(host) || host == "*") continue;
                    string url = "https://" + host; if (!string.IsNullOrEmpty(path) && path != "*") url += "/" + path.TrimStart('/');
                    return url;
                }
            } catch { }
            return null;
        }

        private TunnelServiceStatus GetLocalStatus()
        {
            var status = new TunnelServiceStatus();
            if (!_serviceManager.IsInstalled()) { status.ServiceState = "NotInstalled"; status.DiagnosticsNote = "Cloudflared service is not installed."; return status; }
            status.ServiceState = _serviceManager.GetStatusText();
            var imagePath = TunnelDiscovery.TryGetServiceImagePath(); var token = TunnelDiscovery.TryExtractTokenFromImagePath(imagePath); var tunnelId = TunnelDiscovery.TryDecodeTunnelIdFromToken(token);
            status.TunnelId = tunnelId; if (tunnelId == null) status.DiagnosticsNote = "Could not decode tunnel ID.";
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
                try { var v = await api.VerifyTokenAsync(ctsV.Token); LogInfo("Token: " + (v?.Status ?? "unknown") + (v?.ExpiresOn != null ? " | Expires: " + v.ExpiresOn : "")); }
                catch (Exception vEx) { LogWarn("Could not verify: " + vEx.Message); }
                if (tid != null)
                {
                    using var ctsR = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                    var tunnel = await api.GetTunnelAsync(tid, ctsR.Token); LogInfo("Read OK: " + (tunnel?.Name ?? tid));
                    try { using var ctsW = new CancellationTokenSource(TimeSpan.FromSeconds(10)); await api.GetTunnelTokenAsync(tid, ctsW.Token); LogInfo("Write OK - suitable for Repair"); }
                    catch (Exception wEx) { if (wEx.Message.Contains("403")) LogWarn("Write DENIED - READ ONLY."); else LogWarn("Write test inconclusive: " + wEx.Message); }
                }
                else LogInfo("Token valid. Run Check Tunnel Status to associate with a tunnel.");
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
                if (localStatus.ServiceState == "NotInstalled") { ApplyBadge(lblRemoteStatus, "-"); lblTunnelName.Text = "-"; LogWarn("Service not installed."); return; }
                if (localStatus.ServiceState != "Running") { LogWarn("Service not running. Use Repair Tunnel."); return; }
                LogInfo("Local service is running.");
                var tid = localStatus.TunnelId;
                if (!HasToken())
                {
                    LogWarn("No API token \u2014 running HTTP endpoint check only.");
                    if (tid != null) { var jp = TunnelDetailsPath(tid); if (File.Exists(jp)) { await LoadTunnelDetailsFromJsonAsync(jp); LogInfo("Loaded cached tunnel details."); } else LogInfo("No cached details. Add an API token and re-run."); }
                    var endpointUrl = tid != null ? GetFirstEndpointUrl(tid) : null;
                    if (endpointUrl != null)
                    {
                        LogInfo("HTTP check: GET " + endpointUrl);
                        try
                        {
                            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                            http.DefaultRequestHeaders.Add("User-Agent", "OolioTunnelMonitor/1.2");
                            var resp = await http.GetAsync(endpointUrl, HttpCompletionOption.ResponseHeadersRead);
                            int code = (int)resp.StatusCode;
                            bool hasCf = resp.Headers.Contains("CF-RAY") || (resp.Headers.Contains("Server") && resp.Headers.GetValues("Server").Any(s => s.Contains("cloudflare")));
                            string cfRay = resp.Headers.Contains("CF-RAY") ? resp.Headers.GetValues("CF-RAY").First() : "";
                            string server = resp.Headers.Contains("Server") ? resp.Headers.GetValues("Server").First() : "unknown";
                            if (hasCf) { ApplyBadge(lblRemoteStatus, "Tunnel OK"); LogInfo("HTTP " + code + " \u2014 Cloudflare responded (Server: " + server + (cfRay != "" ? ", CF-RAY: " + cfRay : "") + ")"); }
                            else if (code >= 200 && code < 500) { ApplyBadge(lblRemoteStatus, "Reachable"); LogInfo("HTTP " + code + " \u2014 endpoint responded. Server: " + server); }
                            else { ApplyBadge(lblRemoteStatus, "Unreachable"); LogWarn("HTTP " + code + " \u2014 endpoint may not be functioning."); }
                        }
                        catch (HttpRequestException hrEx) { ApplyBadge(lblRemoteStatus, "Unreachable"); LogWarn("HTTP check failed: " + hrEx.Message); }
                        catch (TaskCanceledException) { ApplyBadge(lblRemoteStatus, "Unreachable"); LogWarn("HTTP check timed out after 10s."); }
                    }
                    else LogInfo("No endpoint URL available. Add an API token to fetch route config.");
                    LogInfo("Check complete (no API token \u2014 limited detail)."); return;
                }
                LogInfo("API token found \u2014 querying Cloudflare...");
                var api = new CloudflareApi(GetToken());
                using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var tunnel = await api.GetTunnelAsync(tid!, cts1.Token);
                if (tunnel != null) { lblTunnelName.Text = tunnel.Name ?? "-"; ApplyBadge(lblRemoteStatus, tunnel.Status ?? "-"); LogInfo("Tunnel: " + tunnel.Name + "  |  API status: " + tunnel.Status); }
                using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var config = await api.GetTunnelConfigAsync(tid!, cts2.Token);
                var ingress = config?.Config?.Ingress ?? new List<CfIngressRule>();
                var items = ingress.Select(r => IngressHelper.Build(r.Hostname, r.Path, r.Service)).Where(i => i != null).Cast<IngressItem>().ToList();
                PopulateIngress(items); await SaveTunnelDetailsAsync(tid!, tunnel?.Name, tunnel?.Status, ingress);
                LogInfo("Saved " + items.Count + " route(s) to cache."); LogInfo("Check complete.");
            }
            catch (Exception ex) { LogError("Check failed", ex); }
            finally { btnTunnelStatus.Enabled = true; }
        }

        public async Task CreateTunnelAsync()
        {
            if (!HasToken()) { LogWarn("Enter an API token first."); return; }
            SetMode(AppMode.Install);
            pnlInstall.Controls.Clear();
            var installForm = new CreateTunnelForm();
            installForm.TopLevel = false;
            installForm.FormBorderStyle = FormBorderStyle.None;
            installForm.Dock = DockStyle.Fill;
            installForm.Visible = true;
            pnlInstall.Controls.Add(installForm);
            var tcs = new TaskCompletionSource<DialogResult>();
            installForm.FormClosed += (_, _) => tcs.TrySetResult(installForm.DialogResult);
            var result = await tcs.Task;
            pnlInstall.Controls.Clear();
            if (result != DialogResult.OK || installForm.Result == null) { LogInfo("Install tunnel cancelled."); SetMode(AppMode.Main); return; }
            var spec = installForm.Result;
            LogInfo("Creating tunnel: " + spec.TunnelName);
            var api = new CloudflareApi(GetToken());
            try
            {
                using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var tunnel = await api.CreateTunnelAsync(spec.TunnelName, cts1.Token);
                if (tunnel?.Id == null) throw new InvalidOperationException("Tunnel creation returned no ID.");
                LogInfo("Created: " + tunnel.Name + " (" + tunnel.Id + ")");
                var ingressRules = spec.Routes
                    .Select(r => {
                        var host = CreateTunnelForm.BuildHostname(r, spec);
                        if (string.IsNullOrWhiteSpace(host) || r.Port <= 0) return null;
                        return new CfIngressRule { Hostname = host, Service = "http://localhost:" + r.Port };
                    }).Where(r => r != null).Cast<CfIngressRule>().ToList();
                using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await api.PutTunnelConfigAsync(tunnel.Id, ingressRules, cts2.Token); LogInfo("Configured " + ingressRules.Count + " route(s).");
                await SaveTunnelDetailsAsync(tunnel.Id, tunnel.Name, tunnel.Status ?? "pending", ingressRules);
                using var cts3 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var token = await api.GetTunnelTokenAsync(tunnel.Id, cts3.Token) ?? throw new InvalidOperationException("Empty token.");
                LogInfo("Downloading MSI..."); using var dlc = new CancellationTokenSource(TimeSpan.FromMinutes(3));
                var msiPath = await _installer.DownloadMsiAsync(dlc.Token);
                await Task.Run(() => _installer.UninstallExistingMsi());
                LogInfo("Installing MSI..."); await Task.Run(() => _installer.InstallMsi(msiPath)); try { File.Delete(msiPath); } catch { }
                var exe = _installer.FindCloudflaredExeOrThrow();
                LogInfo("Installing service..."); await Task.Run(() => _installer.InstallServiceWithToken(exe, token));
                _serviceManager.StartService(); LogInfo("Installation complete."); await CheckTunnelStatusAsync();
            }
            catch (Exception ex) { LogError("Install tunnel failed", ex); }
            finally { SetMode(AppMode.Main); }
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
                        if (files.Length == 1) { tid = Path.GetFileNameWithoutExtension(files[0]); LogInfo("Using cached ID: " + tid); }
                        else if (files.Length > 1) { LogWarn("Multiple cached tunnels. Run Check Tunnel Status first."); return; }
                    }
                }
                if (tid == null) { LogError("No tunnel ID found."); return; }
                LogInfo("Repairing " + tid + "...");
                _serviceManager.StopServiceBestEffort(); _serviceManager.KillCloudflaredProcess(); _serviceManager.DeleteService();
                await Task.Run(() => _installer.UninstallExistingMsi());
                string msiPath; using (var dlc = new CancellationTokenSource(TimeSpan.FromMinutes(5))) msiPath = await _installer.DownloadMsiAsync(dlc.Token);
                await Task.Run(() => _installer.InstallMsi(msiPath)); try { File.Delete(msiPath); } catch { }
                var exe = _installer.FindCloudflaredExeOrThrow();
                string newToken; using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    newToken = await api.GetTunnelTokenAsync(tid, cts.Token) ?? throw new InvalidOperationException("Empty token.");
                await Task.Run(() => _installer.InstallServiceWithToken(exe, newToken));
                _serviceManager.StartService(); LogInfo("Repair complete."); await CheckTunnelStatusAsync();
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

        public async Task CheckForUpdatesAsync(bool silent = false)
        {
            if (silent) { try { if (File.Exists(_updateCheckFile)) { var lastCheck = File.ReadAllText(_updateCheckFile).Trim(); if (lastCheck == DateTime.Today.ToString("yyyy-MM-dd")) return; } } catch { } }
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                http.DefaultRequestHeaders.Add("User-Agent", "OolioTunnelMonitor/" + AppVersion);
                var json = await http.GetStringAsync(VersionJsonUrl);
                try { Directory.CreateDirectory(Path.GetDirectoryName(_updateCheckFile)!); File.WriteAllText(_updateCheckFile, DateTime.Today.ToString("yyyy-MM-dd")); } catch { }
                using var doc = JsonDocument.Parse(json); var root = doc.RootElement;
                string latest = root.TryGetProperty("version", out var v) ? v.GetString() ?? AppVersion : AppVersion;
                string url = root.TryGetProperty("downloadUrl", out var d) ? d.GetString() ?? "" : "";
                string notes = root.TryGetProperty("releaseNotes", out var n) ? n.GetString() ?? "" : "";
                if (IsNewerVersion(latest, AppVersion))
                {
                    LogInfo("Update available: v" + latest);
                    string msg = "A new version of Oolio Tunnel Monitor is available!\n\nYour version:\tv" + AppVersion + "\nNew version:\tv" + latest;
                    if (!string.IsNullOrWhiteSpace(notes)) msg += "\n\n" + notes;
                    msg += "\n\nOpen the download page?";
                    if (MessageBox.Show(this, msg, "Update Available", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes && !string.IsNullOrWhiteSpace(url))
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (!silent) { LogInfo("Up to date (v" + AppVersion + ")"); MessageBox.Show(this, "Oolio Tunnel Monitor is up to date.\n\nVersion: v" + AppVersion, "No Updates", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            }
            catch (Exception ex) { if (!silent) LogWarn("Update check failed: " + ex.Message); }
        }

        private static bool IsNewerVersion(string latest, string current) { try { return new Version(latest) > new Version(current); } catch { return latest != current && latest != ""; } }

        private async void btnCreateTunnel_Click(object? sender, EventArgs e) => await CreateTunnelAsync();
        private async void btnTunnelStatus_Click(object? sender, EventArgs e) => await CheckTunnelStatusAsync();
        private async void btnTestToken_Click(object? sender, EventArgs e) => await TestTokenAsync();
        private async void btnRepair_Click(object? sender, EventArgs e) => await RepairAsync();
        private async void btnCheckUpdates_Click(object? sender, EventArgs e) => await CheckForUpdatesAsync(silent: false);
    }
}
