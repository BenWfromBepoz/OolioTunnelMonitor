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
        private static readonly Image? _logo = LoadLogo();
        private const int Radius = 10;
        private static Image? LoadLogo()
        {
            try
            {
                var stream = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("CloudflaredMonitor.Resources.Oolio.png");
                return stream != null ? Image.FromStream(stream) : null;
            }
            catch { return null; }
        }
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
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            for (int i = 3; i >= 1; i--)
            { using var sb = new SolidBrush(Color.FromArgb(35, 0, 0, 0)); using var sp = Rnd(bounds, Radius, i); g.FillPath(sb, sp); }
            using (var path = Rnd(bounds, Radius, 0)) using (var b = new SolidBrush(Color.White)) g.FillPath(b, path);
            if (_logo != null)
            {
                var lr = new Rectangle(12, 10, Width - 24, Height - 42);
                float sc = Math.Min(lr.Width / (float)_logo.Width, lr.Height / (float)_logo.Height);
                int w = (int)(_logo.Width * sc), h = (int)(_logo.Height * sc);
                g.DrawImage(_logo, new Rectangle(lr.X + (lr.Width - w) / 2, lr.Y + (lr.Height - h) / 2, w, h));
            }
            else
            {
                using var f = new Font("Segoe UI", 18f, FontStyle.Bold);
                using var b = new SolidBrush(Color.FromArgb(103, 58, 182));
                g.DrawString("oolio", f, b, new RectangleF(0, 0, Width, Height - 26),
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }
            using var sf = new Font("Segoe UI", 8.5f);
            using var sb2 = new SolidBrush(Color.FromArgb(100, 116, 139));
            g.DrawString("ZeroTrust Tunnel Monitor", sf, sb2, new RectangleF(0, Height - 26, Width, 22),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        }
        private static GraphicsPath Rnd(Rectangle r, int radius, int inset)
        {
            var rect = new Rectangle(r.X + inset, r.Y + inset, r.Width - inset * 2, r.Height - inset * 2);
            int d = radius * 2; var p = new GraphicsPath();
            p.AddArc(rect.X, rect.Y, d, d, 180, 90); p.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            p.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90); p.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }
    }

    internal static class ShapeHelper
    {
        public static GraphicsPath RoundedPath(Rectangle r, int rad)
        { int d = rad * 2; var p = new GraphicsPath(); p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p; }
    }

    internal sealed class PillLabel : Label
    {
        private const int PillRadius = 9;
        private const int PillWidth  = 150;
        private Color _pillColour = Color.Transparent;
        public Color PillColour { get => _pillColour; set { _pillColour = value; Invalidate(); } }
        public PillLabel()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            BackColor = Color.Transparent; TextAlign = ContentAlignment.MiddleLeft;
            AutoSize = false; Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
        }
        protected override void OnPaintBackground(PaintEventArgs e) { e.Graphics.Clear(Color.White); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            bool hasPill = _pillColour != Color.Transparent && Text.Length > 0 && Text != "-";
            if (!hasPill)
            {
                using var fg = new SolidBrush(Color.FromArgb(100, 116, 139));
                g.DrawString(Text, Font, fg, new RectangleF(0, 0, Width, Height),
                    new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });
                return;
            }
            int pw = PillWidth, ph = (int)g.MeasureString(Text, Font).Height + 8, py = (Height - ph) / 2;
            var rect = new Rectangle(0, py, pw, ph);
            Color topCol, botCol;
            var r = _pillColour.R; var gC = _pillColour.G; var b = _pillColour.B;
            if (gC > r && gC > b)      { topCol = Color.FromArgb(125, 230, 135); botCol = Color.FromArgb(90,  200, 100); }
            else if (r > gC && r > b)  { topCol = Color.FromArgb(230, 135, 125); botCol = Color.FromArgb(200,  80,  70); }
            else                       { topCol = Color.FromArgb(240, 230,  90);  botCol = Color.FromArgb(230, 180,  50); }
            using var grad = new LinearGradientBrush(new Point(0, py), new Point(pw, py + ph), topCol, botCol);
            using var path = ShapeHelper.RoundedPath(rect, PillRadius);
            g.FillPath(grad, path);
            var glossRect = new Rectangle(0, py, pw, ph / 2);
            using var gloss = new LinearGradientBrush(glossRect, Color.FromArgb(80, Color.White), Color.FromArgb(0, Color.White), LinearGradientMode.Vertical);
            g.SetClip(path); g.FillRectangle(gloss, glossRect); g.ResetClip();
            using var fgB = new SolidBrush(Color.White);
            g.DrawString(Text, Font, fgB, new RectangleF(0, py, pw, ph),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        }
    }

    internal sealed class PillButton : Button
    {
        private const int Radius = 13;
        private bool _hovered;
        public PillButton()
        {
            FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent; ForeColor = Color.White;
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
            Cursor = Cursors.Hand; TextAlign = ContentAlignment.MiddleCenter;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
        }
        protected override void OnMouseEnter(EventArgs e) { _hovered = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaintBackground(PaintEventArgs e) { e.Graphics.Clear(Color.White); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = ShapeHelper.RoundedPath(bounds, Radius);
            var topCol = _hovered ? Color.FromArgb(160, 115, 240) : Color.FromArgb(140, 95, 220);
            var botCol = _hovered ? Color.FromArgb(90,  50, 160)  : Color.FromArgb(75,  40, 140);
            using var grad = new LinearGradientBrush(new Point(0, 0), new Point(Width, Height), topCol, botCol);
            g.FillPath(grad, path);
            if (Height > 4)
            {
                var gr = new Rectangle(0, 0, Width, Height / 2);
                using var gloss = new LinearGradientBrush(gr, Color.FromArgb(80, Color.White), Color.FromArgb(0, Color.White), LinearGradientMode.Vertical);
                g.SetClip(path); g.FillRectangle(gloss, gr); g.ResetClip();
            }
            using var fg = new SolidBrush(Color.White);
            g.DrawString(Text, Font, fg, new RectangleF(0, 0, Width, Height),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        }
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

    // ── RoundedPanel ──────────────────────────────────────────────────────────
    // Fix: debounce Region recalc using a Timer so it only fires after the
    // resize interaction settles (eliminates flicker/glitch during drag-resize).
    internal sealed class RoundedPanel : Panel
    {
        private const int Radius = 10;
        private readonly System.Windows.Forms.Timer _resizeTimer;

        public RoundedPanel()
        {
            DoubleBuffered = true; ResizeRedraw = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.White;

            // Debounce: wait 50ms after last resize event before updating Region
            _resizeTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _resizeTimer.Tick += (_, _) =>
            {
                _resizeTimer.Stop();
                ApplyRegion();
                Invalidate();
            };
        }

        private void ApplyRegion()
        {
            if (Width > 0 && Height > 0)
            {
                using var p = RRP(new Rectangle(0, 0, Width, Height), Radius);
                Region = new Region(p);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // Restart the debounce timer on every resize event
            _resizeTimer.Stop();
            _resizeTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            for (int i = 3; i >= 1; i--)
            { using var sb = new SolidBrush(Color.FromArgb(18, 0, 0, 0)); using var sp = RRP(new Rectangle(i, i, Width - i * 2, Height - i * 2), Radius); g.FillPath(sb, sp); }
            using var wb = new SolidBrush(Color.White); using var wp = RRP(new Rectangle(0, 0, Width - 1, Height - 1), Radius); g.FillPath(wb, wp);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _resizeTimer.Dispose();
            base.Dispose(disposing);
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
            g.DrawEllipse(pen, 2, 9, 14, 14); g.DrawEllipse(pen, 16, 9, 14, 14);
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
        private const string AppVersion     = "1.2.0.1";
        private const string VersionJsonUrl = "https://raw.githubusercontent.com/BenWfromBepoz/CloudflaredMonitor/main/version.json";

        private static string TunnelDetailsDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Bepoz", "CloudflaredMonitor", "tunnel-details");
        private static string TunnelDetailsPath(string id) => Path.Combine(TunnelDetailsDir, id + ".json");

        private Panel? _pnlInstall;
        private bool   _installViewActive;

        public MainForm()
        {
            InitializeComponent();
            _exporter = new DiagnosticsExporter(_logger);
            dgvIngress.CellPainting += DgvIngress_CellPainting;
            this.FormClosing += (_, e) => { e.Cancel = true; Hide(); };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyGridHeaderStyles();
            _ = LoadTodaysLogAsync();
            _ = CheckTunnelStatusAsync();
        }

        private void ShowStandardView()
        {
            if (_installViewActive && _pnlInstall != null)
            { tblMain.Visible = true; _pnlInstall.Visible = false; _installViewActive = false; }
        }

        private void ShowInstallView()
        {
            if (_installViewActive) return;
            if (_pnlInstall == null) _pnlInstall = BuildInstallPanel();
            tblMain.Visible = false; _pnlInstall.Visible = true; _installViewActive = true;
        }

        private Panel BuildInstallPanel()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(226, 232, 240), Visible = false };
            Controls.Add(pnl); pnl.BringToFront(); pnlSidebar.BringToFront();
            var card = new RoundedPanel { Dock = DockStyle.None, Location = new Point(20, 20) };
            card.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            card.Size = new Size(pnl.Width - 40, pnl.Height - 40);
            pnl.Controls.Add(card);
            pnl.Resize += (_, _) => { card.Size = new Size(pnl.Width - 40, pnl.Height - 40); };
            var title     = new Label { Text = "Install New Tunnel", Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(24, 20), Size = new Size(500, 30), BackColor = Color.Transparent };
            var lblName   = new Label { Text = "Tunnel Name", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(100,116,139), Location = new Point(24, 70), AutoSize = true, BackColor = Color.Transparent };
            var txtName   = new TextBox { Location = new Point(24, 92), Size = new Size(400, 28), Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle };
            var lblRoutes = new Label { Text = "Ingress Routes  (one per line: hostname=service  e.g.  app.example.com=http://localhost:8080)", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(100,116,139), Location = new Point(24, 134), AutoSize = true, BackColor = Color.Transparent };
            var txtRoutes = new TextBox { Location = new Point(24, 156), Size = new Size(600, 140), Multiline = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Cascadia Mono", 8.5f), BorderStyle = BorderStyle.FixedSingle };
            var lblStatus = new Label { Location = new Point(24, 314), Size = new Size(700, 18), Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(100,116,139), BackColor = Color.Transparent };
            var btnInstall = new PillButton { Text = "Install Tunnel", Location = new Point(24, 344), Size = new Size(140, 34) };
            var btnCancel  = new Button { Text = "Cancel", Location = new Point(174, 344), Size = new Size(90, 34), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(226,232,240), ForeColor = Color.FromArgb(71,85,105), Font = new Font("Segoe UI", 9f), Cursor = Cursors.Hand };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 210, 220);
            btnCancel.Click += (_, _) => ShowStandardView();
            btnInstall.Click += async (_, _) =>
            {
                if (!HasToken()) { lblStatus.Text = "Enter an API token first."; lblStatus.ForeColor = Color.FromArgb(200,30,30); return; }
                string tunnelName = txtName.Text.Trim();
                if (string.IsNullOrEmpty(tunnelName)) { lblStatus.Text = "Tunnel name is required."; lblStatus.ForeColor = Color.FromArgb(200,30,30); return; }
                var routes = new List<CfIngressRule>();
                foreach (var line in txtRoutes.Lines)
                {
                    var trimmed = line.Trim(); if (string.IsNullOrEmpty(trimmed)) continue;
                    var eqIdx = trimmed.IndexOf('='); if (eqIdx < 0) continue;
                    var hostname = trimmed[..eqIdx].Trim(); var service = trimmed[(eqIdx + 1)..].Trim();
                    if (!string.IsNullOrEmpty(hostname) && !string.IsNullOrEmpty(service))
                        routes.Add(new CfIngressRule { Hostname = hostname, Service = service });
                }
                btnInstall.Enabled = false;
                lblStatus.Text = "Creating tunnel..."; lblStatus.ForeColor = Color.FromArgb(100,116,139);
                try { await CreateTunnelFromSpecAsync(tunnelName, routes); lblStatus.Text = "Tunnel created!"; lblStatus.ForeColor = Color.FromArgb(16,140,60); await Task.Delay(1500); ShowStandardView(); }
                catch (Exception ex) { lblStatus.Text = "Error: " + ex.Message; lblStatus.ForeColor = Color.FromArgb(200,30,30); }
                finally { btnInstall.Enabled = true; }
            };
            card.Controls.Add(title); card.Controls.Add(lblName); card.Controls.Add(txtName);
            card.Controls.Add(lblRoutes); card.Controls.Add(txtRoutes);
            card.Controls.Add(lblStatus); card.Controls.Add(btnInstall); card.Controls.Add(btnCancel);
            return pnl;
        }

        private void ApplyGridHeaderStyles()
        {
            dgvIngress.EnableHeadersVisualStyles = false;
            var cc = dgvIngress.Columns["colCloud"]; var cl = dgvIngress.Columns["colLocal"];
            if (cc != null) { cc.HeaderCell.Style.BackColor = Color.FromArgb(237,233,254); cc.HeaderCell.Style.ForeColor = Color.FromArgb(76,29,149); cc.HeaderCell.Style.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold); cc.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft; cc.HeaderCell.Style.Padding = new Padding(6,0,0,0); }
            if (cl != null) { cl.HeaderCell.Style.BackColor = Color.FromArgb(241,245,249); cl.HeaderCell.Style.ForeColor = Color.FromArgb(76,29,149); cl.HeaderCell.Style.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold); cl.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft; cl.HeaderCell.Style.Padding = new Padding(6,0,0,0); }
            dgvIngress.Invalidate();
        }

        private void DgvIngress_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        { if (e.RowIndex < 0) return; e.PaintBackground(e.ClipBounds, false); e.PaintContent(e.ClipBounds); e.Handled = true; }

        private async Task LoadTodaysLogAsync()
        {
            try
            {
                var logPath = _logger.LogFilePath; if (!File.Exists(logPath)) return;
                var lines = await File.ReadAllLinesAsync(logPath); if (IsDisposed) return;
                if (txtLog.InvokeRequired) txtLog.BeginInvoke(() => { if (!IsDisposed) { txtLog.Lines = lines; ScrollLogToEnd(); } });
                else { txtLog.Lines = lines; ScrollLogToEnd(); }
            }
            catch { }
        }

        private void ScrollLogToEnd() { if (IsDisposed) return; txtLog.SelectionStart = txtLog.TextLength; txtLog.ScrollToCaret(); }
        private static string Ts() => DateTime.Now.ToString("yy-MM-dd | HH:mm:ss");
        private void LogInfo(string m) { AppendLog(m); _logger.Info(m); }
        private void LogWarn(string m) { AppendLog("WARN: " + m); _logger.Warn(m); }
        private void LogError(string m, Exception? ex = null)
        {
            string detail = ex == null ? m : m + " - " + ex.Message;
            if (ex != null && ex.Message.Contains("403") && ex.Message.Contains("10000"))
                detail += " | TOKEN SCOPE: Needs 'Cloudflare Tunnel:Edit' permission.";
            AppendLog("ERROR: " + detail);
            if (ex == null) _logger.Error(m); else _logger.Error(m, ex);
        }
        private void AppendLog(string message)
        {
            string line = Ts() + " " + message; _uiLogs.Add(line);
            if (IsDisposed) return;
            if (txtLog.InvokeRequired) txtLog.BeginInvoke(() => { if (!IsDisposed) { txtLog.AppendText(line + Environment.NewLine); ScrollLogToEnd(); } });
            else { txtLog.AppendText(line + Environment.NewLine); ScrollLogToEnd(); }
        }

        private static Color BadgeColour(string? v, bool svc = false)
        {
            if (string.IsNullOrWhiteSpace(v) || v == "-") return Color.Transparent;
            var s = v.ToLowerInvariant();
            if (svc) return s == "running" ? Color.FromArgb(16,140,60) : s is "stopped" or "notinstalled" ? Color.FromArgb(200,30,30) : Color.FromArgb(180,100,0);
            return s is "healthy" or "active" or "connected" or "reachable" or "tunnel ok" ? Color.FromArgb(16,140,60)
                 : s is "inactive" or "degraded" or "down" or "unreachable"               ? Color.FromArgb(200,30,30)
                                                                                           : Color.FromArgb(180,100,0);
        }
        private static string ServiceTooltip(string? v) => (v ?? "").ToLowerInvariant() switch { "running" => "Service is running.", "notinstalled" => "Service not installed.", "stopped" => "Service stopped.", _ => "Unknown." };
        private static string RemoteTooltip(string? v) => (v ?? "").ToLowerInvariant() switch
        { "healthy" or "active" or "connected" or "reachable" or "tunnel ok" => "Tunnel is reachable.", "inactive" or "degraded" or "down" or "unreachable" => "Tunnel is not reachable.", _ => "Status unknown." };
        private void ApplyBadge(PillLabel lbl, string text, bool isService = false)
        { lbl.Text = text; lbl.PillColour = BadgeColour(text, isService); toolTip.SetToolTip(lbl, isService ? ServiceTooltip(text) : RemoteTooltip(text)); }

        private string GetToken() => txtApiToken.Text.Trim();
        private bool   HasToken() => !string.IsNullOrWhiteSpace(txtApiToken.Text);

        private void PopulateIngress(IEnumerable<IngressItem> items)
        { dgvIngress.Rows.Clear(); foreach (var item in items) { if (item.IsCatchAll) continue; dgvIngress.Rows.Add(item.CloudEndpoint, item.LocalEndpoint); } }

        public void OpenLogFolder()    { try { Process.Start("explorer.exe", _logger.LogDirectory); } catch (Exception ex) { LogError("Could not open log folder", ex); } }
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
                var jp = TunnelDetailsPath(tunnelId); if (!File.Exists(jp)) return null;
                using var doc = JsonDocument.Parse(File.ReadAllText(jp)); var root = doc.RootElement;
                if (!root.TryGetProperty("Routes", out var routes)) return null;
                foreach (var route in routes.EnumerateArray())
                {
                    string svc = route.TryGetProperty("Service", out var sp) ? sp.GetString() ?? "" : "";
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
            ShowStandardView();
            btnTunnelStatus.Enabled = false; dgvIngress.Rows.Clear();
            try
            {
                LogInfo("Checking local service...");
                var localStatus = GetLocalStatus(); _currentStatus = localStatus;
                ApplyBadge(lblService, localStatus.ServiceState, isService: true);
                lblTunnelId.Text = localStatus.TunnelId ?? "-";
                if (!string.IsNullOrWhiteSpace(localStatus.DiagnosticsNote)) LogInfo(localStatus.DiagnosticsNote!);
                if (localStatus.ServiceState == "NotInstalled")
                { ApplyBadge(lblRemoteStatus, "-"); lblTunnelName.Text = "-"; LogWarn("Service not installed."); return; }
                if (localStatus.ServiceState != "Running")
                { LogWarn("Service installed but not running. Use Repair Tunnel."); return; }
                LogInfo("Local service is running.");
                var tid = localStatus.TunnelId;
                if (!HasToken())
                {
                    LogWarn("No API token — running HTTP endpoint check only.");
                    LogInfo("Add a token above for authoritative Cloudflare API status, route config and auto-save.");
                    if (tid != null) { var jp = TunnelDetailsPath(tid); if (File.Exists(jp)) { await LoadTunnelDetailsFromJsonAsync(jp); LogInfo("Loaded cached tunnel details."); } else LogInfo("No cached details. Add an API token and re-run."); }
                    var endpointUrl = tid != null ? GetFirstEndpointUrl(tid) : null;
                    if (endpointUrl != null)
                    {
                        LogInfo("HTTP check: GET " + endpointUrl);
                        try
                        {
                            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                            http.DefaultRequestHeaders.Add("User-Agent", "CloudflaredMonitor/1.2");
                            var resp = await http.GetAsync(endpointUrl, HttpCompletionOption.ResponseHeadersRead);
                            int  code  = (int)resp.StatusCode;
                            bool hasCf = resp.Headers.Contains("CF-RAY") || (resp.Headers.Contains("Server") && resp.Headers.GetValues("Server").Any(s => s.Contains("cloudflare")));
                            string cfRay  = resp.Headers.Contains("CF-RAY")  ? resp.Headers.GetValues("CF-RAY").First()  : "";
                            string server = resp.Headers.Contains("Server")   ? resp.Headers.GetValues("Server").First()  : "unknown";
                            if (hasCf)
                            {
                                ApplyBadge(lblRemoteStatus, "Tunnel OK");
                                LogInfo("HTTP " + code + " — Cloudflare responded (Server: " + server + (cfRay != "" ? ", CF-RAY: " + cfRay : "") + ")");
                                if (code == 502) LogWarn("  502 — tunnel working, origin service not responding.");
                                else if (code == 503) LogWarn("  503 — tunnel working, origin temporarily unavailable.");
                                else if (code >= 200 && code < 300) LogInfo("  " + code + " — origin also responding normally.");
                                else LogInfo("  " + code + " — origin returned this status (tunnel itself is fine).");
                            }
                            else if (code >= 200 && code < 500) { ApplyBadge(lblRemoteStatus, "Reachable"); LogInfo("HTTP " + code + " — endpoint responded. Server: " + server); }
                            else { ApplyBadge(lblRemoteStatus, "Unreachable"); LogWarn("HTTP " + code + " — endpoint may not be functioning."); }
                        }
                        catch (HttpRequestException hrEx) { ApplyBadge(lblRemoteStatus, "Unreachable"); LogWarn("HTTP check failed: " + hrEx.Message); }
                        catch (TaskCanceledException)    { ApplyBadge(lblRemoteStatus, "Unreachable"); LogWarn("HTTP check timed out after 10s."); }
                    }
                    else LogInfo("No endpoint URL available. Add an API token to fetch route config.");
                    LogInfo("Check complete (no API token — limited detail).");
                    return;
                }
                LogInfo("API token found — querying Cloudflare...");
                var api = new CloudflareApi(GetToken());
                using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var tunnel = await api.GetTunnelAsync(tid!, cts1.Token);
                if (tunnel != null) { lblTunnelName.Text = tunnel.Name ?? "-"; ApplyBadge(lblRemoteStatus, tunnel.Status ?? "-"); LogInfo("Tunnel: " + tunnel.Name + "  |  API status: " + tunnel.Status); }
                using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var config = await api.GetTunnelConfigAsync(tid!, cts2.Token);
                var ingress = config?.Config?.Ingress ?? new List<CfIngressRule>();
                var items = ingress.Select(r => IngressHelper.Build(r.Hostname, r.Path, r.Service)).Where(i => i != null).Cast<IngressItem>().ToList();
                PopulateIngress(items); await SaveTunnelDetailsAsync(tid!, tunnel?.Name, tunnel?.Status, ingress);
                LogInfo("Saved " + items.Count + " route(s) to cache.");
                LogInfo("Check complete.");
            }
            catch (Exception ex) { LogError("Check failed", ex); }
            finally { btnTunnelStatus.Enabled = true; }
        }

        private async Task CreateTunnelFromSpecAsync(string tunnelName, List<CfIngressRule> routes)
        {
            LogInfo("Creating tunnel: " + tunnelName);
            var api = new CloudflareApi(GetToken());
            using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var tunnel = await api.CreateTunnelAsync(tunnelName, cts1.Token);
            if (tunnel?.Id == null) throw new InvalidOperationException("Tunnel creation returned no ID.");
            LogInfo("Created: " + tunnel.Name + " (" + tunnel.Id + ")");
            using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await api.PutTunnelConfigAsync(tunnel.Id, routes, cts2.Token); LogInfo("Configured " + routes.Count + " route(s).");
            await SaveTunnelDetailsAsync(tunnel.Id, tunnel.Name, tunnel.Status ?? "pending", routes);
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

        public async Task RepairAsync()
        {
            ShowStandardView();
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
            ShowStandardView();
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
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var json = await http.GetStringAsync(VersionJsonUrl);
                using var doc = JsonDocument.Parse(json); var root = doc.RootElement;
                string latest = root.TryGetProperty("version", out var v) ? v.GetString() ?? AppVersion : AppVersion;
                string url    = root.TryGetProperty("downloadUrl", out var d) ? d.GetString() ?? "" : "";
                if (latest != AppVersion)
                {
                    LogInfo("Update available: v" + latest);
                    if (MessageBox.Show(this, "New version v" + latest + " is available.\nOpen download page?",
                        "Update Available", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes && !string.IsNullOrWhiteSpace(url))
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (!silent) LogInfo("Up to date (v" + AppVersion + ")");
            }
            catch (Exception ex) { if (!silent) LogWarn("Update check failed: " + ex.Message); }
        }

        private void btnCreateTunnel_Click(object? sender, EventArgs e)          => ShowInstallView();
        private async void btnTunnelStatus_Click(object? sender, EventArgs e)    => await CheckTunnelStatusAsync();
        private async void btnTestToken_Click(object? sender, EventArgs e)       => await TestTokenAsync();
        private void btnOpenLogs_Click(object? sender, EventArgs e)               => OpenLogFolder();
        private void btnOpenConfig_Click(object? sender, EventArgs e)             => OpenConfigFolder();
        private async void btnRepair_Click(object? sender, EventArgs e)           => await RepairAsync();
        private async void btnCheckUpdates_Click(object? sender, EventArgs e)     => await CheckForUpdatesAsync();
    }
}
