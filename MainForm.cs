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
    // ── New enums for AppMode architecture ────────────────────────────────────
    public enum PillButtonStyle   { Normal, Active }
    public enum ModernButtonStyle { Primary, Muted }
    public enum AppMode           { Main, Install, Tools, Help }

    // ── Helper classes from main (unchanged) ──────────────────────────────────
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
        private PillButtonStyle _style = PillButtonStyle.Normal;
        public PillButtonStyle Style { get => _style; set { _style = value; Invalidate(); } }
        public PillButton()
        {
            FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent; ForeColor = Color.White;
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
            Cursor = Cursors.Hand; TextAlign = ContentAlignment.MiddleCenter;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.Opaque, true);
        }
        protected override void OnMouseEnter(EventArgs e) { _hovered = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Parent?.BackColor ?? Color.White);
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = ShapeHelper.RoundedPath(bounds, Radius);
            Color topCol, botCol, fgCol;
            if (_style == PillButtonStyle.Active)
            {
                // Lighter violet + dark text for "Back to Monitor"
                topCol = _hovered ? Color.FromArgb(196, 181, 253) : Color.FromArgb(167, 139, 250);
                botCol = _hovered ? Color.FromArgb(167, 139, 250) : Color.FromArgb(124,  58, 237);
                fgCol  = Color.FromArgb(30, 10, 80);
            }
            else
            {
                topCol = _hovered ? Color.FromArgb(160, 115, 240) : Color.FromArgb(140, 95, 220);
                botCol = _hovered ? Color.FromArgb(90,  50, 160)  : Color.FromArgb(75,  40, 140);
                fgCol  = Color.White;
            }
            using var grad = new LinearGradientBrush(new Point(0, 0), new Point(Width, Height), topCol, botCol);
            g.FillPath(grad, path);
            if (Height > 4 && _style == PillButtonStyle.Normal)
            {
                var gr = new Rectangle(0, 0, Width, Height / 2);
                using var gloss = new LinearGradientBrush(gr, Color.FromArgb(80, Color.White), Color.FromArgb(0, Color.White), LinearGradientMode.Vertical);
                g.SetClip(path); g.FillRectangle(gloss, gr); g.ResetClip();
            }
            using var fg = new SolidBrush(fgCol);
            g.DrawString(Text, Font, fg, new RectangleF(0, 0, Width, Height),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        }
    }

    internal sealed class ModernButton : Button
    {
        private static readonly Color _normal = Color.FromArgb(45, 52, 68);
        private static readonly Color _hover  = Color.FromArgb(60, 68, 88);
        private static readonly Color _accent = Color.FromArgb(103, 58, 182);
        private static readonly Color _muted  = Color.FromArgb(108, 117, 125);
        private static readonly Color _mutedH = Color.FromArgb(120, 128, 140);
        private const int Radius = 8;
        private ModernButtonStyle _style = ModernButtonStyle.Primary;
        public ModernButtonStyle Style { get => _style; set { _style = value; Invalidate(); } }
        public ModernButton()
        {
            FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0;
            BackColor = _normal; ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f); Cursor = Cursors.Hand;
            TextAlign = ContentAlignment.MiddleLeft; Padding = new Padding(14, 0, 0, 0);
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }
        protected override void OnMouseEnter(EventArgs e) { BackColor = _style == ModernButtonStyle.Muted ? _mutedH : _hover;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { BackColor = _style == ModernButtonStyle.Muted ? _muted  : _normal; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(39, 46, 63));
            using var path = RR(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
            using var brush = new SolidBrush(BackColor); g.FillPath(brush, path);
            if (_style == ModernButtonStyle.Primary)
                { using var ab = new SolidBrush(_accent); g.FillRectangle(ab, new Rectangle(0, Radius, 3, Height - Radius * 2)); }
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
        private readonly System.Windows.Forms.Timer _resizeTimer;
        private static readonly Color _pageBg = Color.FromArgb(226, 232, 240);
        public RoundedPanel()
        {
            DoubleBuffered = true; ResizeRedraw = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.White;
            _resizeTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _resizeTimer.Tick += (_, _) => { _resizeTimer.Stop(); Invalidate(); };
        }
        protected override void OnResize(EventArgs e) { base.OnResize(e); _resizeTimer.Stop(); _resizeTimer.Start(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(_pageBg);
            for (int i = 3; i >= 1; i--)
            { using var sb = new SolidBrush(Color.FromArgb(18, 0, 0, 0)); using var sp = RRP(new Rectangle(i, i, Width - i * 2, Height - i * 2), Radius); g.FillPath(sb, sp); }
            using var wb = new SolidBrush(Color.White);
            using var wp = RRP(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
            g.FillPath(wb, wp);
        }
        protected override void Dispose(bool disposing) { if (disposing) _resizeTimer.Dispose(); base.Dispose(disposing); }
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

    // ═══════════════════════════════════════════════════════════════════════════
    // MainForm
    // ═══════════════════════════════════════════════════════════════════════════
    public partial class MainForm : Form
    {
        // ── Core services (from main) ─────────────────────────────────────────
        private readonly CloudflaredServiceManager _serviceManager = new();
        private readonly CloudflaredInstaller      _installer      = new();
        private readonly FileLogger                _logger         = new();
        private readonly DiagnosticsExporter       _exporter;
        private TunnelServiceStatus? _currentStatus;
        private readonly List<string> _uiLogs = new();
        private const string AppVersion     = "1.2.1.0";
        private const string VersionJsonUrl = "https://raw.githubusercontent.com/BenWfromBepoz/OolioTunnelMonitor/refs/heads/main/version.json";

        private static readonly string _updateCheckFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Bepoz", "CloudflaredMonitor", "last-update-check.txt");

        private static string TunnelDetailsDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Bepoz", "CloudflaredMonitor", "tunnel-details");
        private static string TunnelDetailsPath(string id) => Path.Combine(TunnelDetailsDir, id + ".json");

        // ── AppMode ───────────────────────────────────────────────────────────
        private AppMode _mode = AppMode.Main;

        // ── Sidebar panels ────────────────────────────────────────────────────
        private Panel pnlSidebarMain    = null!;
        private Panel pnlSidebarInstall = null!;
        private Panel pnlSidebarTools   = null!;
        private Panel pnlSidebarHelp    = null!;

        // ── Content panels ────────────────────────────────────────────────────
        private Panel pnlInstall = null!;
        private Panel pnlTools   = null!;
        private Panel pnlHelp    = null!;

        // ── Back buttons (one per secondary sidebar) ──────────────────────────
        private PillButton btnBackFromInstall = null!;
        private PillButton btnBackFromTools   = null!;
        private PillButton btnBackFromHelp    = null!;

        // ── Tools/Help nav buttons (on main sidebar) ──────────────────────────
        private PillButton btnToolsNav = null!;
        private PillButton btnHelpNav  = null!;

        // ── Constructor ───────────────────────────────────────────────────────
        public MainForm()
        {
            InitializeComponent();
            _exporter = new DiagnosticsExporter(_logger);
            dgvIngress.CellPainting += DgvIngress_CellPainting;
            this.FormClosing += (_, e) => { e.Cancel = true; Hide(); };
            BuildSidebars();
            BuildInstallPanel();
            BuildToolsPanel();
            BuildHelpPanel();
            SetMode(AppMode.Main);
            WireNavEvents();
        }

        // ── SetMode ───────────────────────────────────────────────────────────
        private void SetMode(AppMode mode)
        {
            _mode = mode;
            pnlSidebarMain.Visible    = (mode == AppMode.Main);
            pnlSidebarInstall.Visible = (mode == AppMode.Install);
            pnlSidebarTools.Visible   = (mode == AppMode.Tools);
            pnlSidebarHelp.Visible    = (mode == AppMode.Help);
            tblMain.Visible    = (mode == AppMode.Main);
            pnlInstall.Visible = (mode == AppMode.Install);
            pnlTools.Visible   = (mode == AppMode.Tools);
            pnlHelp.Visible    = (mode == AppMode.Help);
        }

        // ── BuildSidebars ─────────────────────────────────────────────────────
        private void BuildSidebars()
        {
            const int btnX = 12, btnW = 200, btnH = 40, btnGap = 4;
            PillButton Btn(string text, PillButtonStyle style = PillButtonStyle.Normal) =>
                new PillButton { Text = text, Size = new Size(btnW, btnH), Style = style };

            // ── Main sidebar panel ────────────────────────────────────────────
            pnlSidebarMain = new Panel { BackColor = Color.Transparent, Size = new Size(224, 600), Location = new Point(0, 120) };

            // Lay out the existing Designer buttons vertically with Tools/Help inserted
            // Designer already placed btnCreateTunnel, btnTunnelStatus etc — we add Tools/Help
            btnToolsNav = Btn("\u2630  Tools");
            btnHelpNav  = Btn("?  Help");

            // Position below the existing sidebar buttons
            // Designer has: Create(130), Status(178), Logs(226), Config(274), Repair(322), chk(372), Updates(404)
            // We insert Tools and Help after Status, moving the rest down
            btnToolsNav.Location = new Point(btnX, 226);
            btnHelpNav.Location  = new Point(btnX, 270);

            // Shift existing buttons down to make room
            btnOpenLogs.Location   = new Point(btnX, 318);
            btnOpenConfig.Location = new Point(btnX, 362);
            btnRepair.Location     = new Point(btnX, 410);
            chkReinstall.Location  = new Point(20,   458);
            btnCheckUpdates.Location = new Point(btnX, 490);
            lblVersion.Location    = new Point(14,   532);

            pnlSidebar.Controls.Add(btnToolsNav);
            pnlSidebar.Controls.Add(btnHelpNav);

            // ── Install sidebar ───────────────────────────────────────────────
            pnlSidebarInstall = new Panel { BackColor = Color.Transparent, Size = new Size(224, 600), Location = new Point(0, 120), Visible = false };
            btnBackFromInstall = Btn("\u2190  Back to Monitor", PillButtonStyle.Active);
            btnBackFromInstall.Location = new Point(btnX, 0);
            pnlSidebarInstall.Controls.Add(btnBackFromInstall);
            pnlSidebar.Controls.Add(pnlSidebarInstall);

            // ── Tools sidebar ─────────────────────────────────────────────────
            pnlSidebarTools = new Panel { BackColor = Color.Transparent, Size = new Size(224, 600), Location = new Point(0, 120), Visible = false };
            btnBackFromTools = Btn("\u2190  Back to Monitor", PillButtonStyle.Active);
            var btnLogsTools   = Btn("\u2261  Open Logfile Folder");
            var btnConfigTools = Btn("\u25a4  Open Config Folder");
            btnBackFromTools.Location  = new Point(btnX,  0);
            btnLogsTools.Location      = new Point(btnX, 52);
            btnConfigTools.Location    = new Point(btnX, 96);
            btnLogsTools.Click   += (_, _) => OpenLogFolder();
            btnConfigTools.Click += (_, _) => OpenConfigFolder();
            pnlSidebarTools.Controls.AddRange(new Control[] { btnBackFromTools, btnLogsTools, btnConfigTools });
            pnlSidebar.Controls.Add(pnlSidebarTools);

            // ── Help sidebar ──────────────────────────────────────────────────
            pnlSidebarHelp = new Panel { BackColor = Color.Transparent, Size = new Size(224, 600), Location = new Point(0, 120), Visible = false };
            btnBackFromHelp = Btn("\u2190  Back to Monitor", PillButtonStyle.Active);
            btnBackFromHelp.Location = new Point(btnX, 0);
            pnlSidebarHelp.Controls.Add(btnBackFromHelp);
            pnlSidebar.Controls.Add(pnlSidebarHelp);
        }

        // ── BuildInstallPanel ─────────────────────────────────────────────────
        private void BuildInstallPanel()
        {
            pnlInstall = new Panel { Visible = false, BackColor = Color.Transparent, Dock = DockStyle.Fill };
            // Install panel uses the existing CreateTunnelForm as a dialog for now
            Controls.Add(pnlInstall);
        }

        // ── BuildToolsPanel ───────────────────────────────────────────────────
        private void BuildToolsPanel()
        {
            pnlTools = new Panel { Visible = false, BackColor = Color.FromArgb(226, 232, 240), Dock = DockStyle.Fill };
            var header = new Label
            {
                Text = "Activity Log", Dock = DockStyle.Top, Height = 40,
                Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42), BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0)
            };
            var logBox = new RichTextBox
            {
                Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(203, 213, 225), Font = new Font("Cascadia Mono", 8.5f),
                BorderStyle = BorderStyle.None, ScrollBars = RichTextBoxScrollBars.Vertical, WordWrap = false
            };
            // Mirror existing log content
            logBox.Text = txtLog.Text;
            txtLog.TextChanged += (_, _) => { if (!pnlTools.IsDisposed) logBox.Text = txtLog.Text; };
            pnlTools.Controls.Add(logBox);
            pnlTools.Controls.Add(header);
            Controls.Add(pnlTools);
        }

        // ── BuildHelpPanel ────────────────────────────────────────────────────
        private void BuildHelpPanel()
        {
            pnlHelp = new Panel { Visible = false, BackColor = Color.FromArgb(226, 232, 240), Dock = DockStyle.Fill };
            var helpBox = new RichTextBox
            {
                Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42), Font = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.None,
                Text = "Oolio Tunnel Monitor\r\n\r\nHelp content coming soon.\r\n\r\nFor support, visit the Cloudflare Zero Trust dashboard."
            };
            pnlHelp.Controls.Add(helpBox);
            Controls.Add(pnlHelp);
        }

        // ── WireNavEvents ─────────────────────────────────────────────────────
        private void WireNavEvents()
        {
            btnToolsNav.Click += (_, _) => SetMode(AppMode.Tools);
            btnHelpNav.Click  += (_, _) => SetMode(AppMode.Help);
            btnBackFromInstall.Click += (_, _) => SetMode(AppMode.Main);
            btnBackFromTools.Click   += (_, _) => SetMode(AppMode.Main);
            btnBackFromHelp.Click    += (_, _) => SetMode(AppMode.Main);
        }

        // ── All original MainForm methods from main (unchanged) ───────────────
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyGridHeaderStyles();
            _ = LoadTodaysLogAsync();
            _ = CheckTunnelStatusAsync();
            _ = CheckForUpdatesAsync(silent: true);
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
                detail += " | TOKEN SCOPE: Needs Cloudflare Tunnel:Edit permission.";
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
        private static string ServiceTooltip(string? v) => (v ?? "").ToLowerInvariant() switch
        { "running" => "Service is running.", "notinstalled" => "Service not installed.", "stopped" => "Service stopped.", _ => "Unknown." };
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
                    string url = "https://" + host; if (!string.IsNullOrEmpty(path) && path != "*") url += "/" + path.TrimStart('/');
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
                    LogWarn("No API token — running HTTP endpoint check only.");
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
                            string cfRay  = resp.Headers.Contains("CF-RAY")  ? resp.Headers.GetValues("CF-RAY").First()  : "";
                            string server = resp.Headers.Contains("Server")   ? resp.Headers.GetValues("Server").First()  : "unknown";
                            if (hasCf) { ApplyBadge(lblRemoteStatus, "Tunnel OK"); LogInfo("HTTP " + code + " — Cloudflare responded (Server: " + server + (cfRay != "" ? ", CF-RAY: " + cfRay : "") + ")"); }
                            else if (code >= 200 && code < 500) { ApplyBadge(lblRemoteStatus, "Reachable"); LogInfo("HTTP " + code + " — endpoint responded. Server: " + server); }
                            else { ApplyBadge(lblRemoteStatus, "Unreachable"); LogWarn("HTTP " + code + " — endpoint may not be functioning."); }
                        }
                        catch (HttpRequestException hrEx) { ApplyBadge(lblRemoteStatus, "Unreachable"); LogWarn("HTTP check failed: " + hrEx.Message); }
                        catch (TaskCanceledException)    { ApplyBadge(lblRemoteStatus, "Unreachable"); LogWarn("HTTP check timed out after 10s."); }
                    }
                    else LogInfo("No endpoint URL available. Add an API token to fetch route config.");
                    LogInfo("Check complete (no API token — limited detail)."); return;
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
                LogInfo("Saved " + items.Count + " route(s) to cache."); LogInfo("Check complete.");
            }
            catch (Exception ex) { LogError("Check failed", ex); }
            finally { btnTunnelStatus.Enabled = true; }
        }

        public async Task CreateTunnelAsync()
        {
            if (!HasToken()) { LogWarn("Enter an API token first."); return; }
            SetMode(AppMode.Install);
            using var dlg = new CreateTunnelForm();
            if (dlg.ShowDialog(this) != DialogResult.OK || dlg.Result == null) { LogInfo("Install tunnel cancelled."); SetMode(AppMode.Main); return; }
            var spec = dlg.Result;
            LogInfo("Creating tunnel: " + spec.TunnelName);
            var api = new CloudflareApi(GetToken());
            try
            {
                using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var tunnel = await api.CreateTunnelAsync(spec.TunnelName, cts1.Token);
                if (tunnel?.Id == null) throw new InvalidOperationException("Tunnel creation returned no ID.");
                LogInfo("Created: " + tunnel.Name + " (" + tunnel.Id + ")");
                var ingressRules = spec.Routes.Select(r => new CfIngressRule { Hostname = r.Hostname, Path = string.IsNullOrEmpty(r.Path) ? null : r.Path, Service = r.Service }).ToList();
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
            if (silent)
            {
                try { if (File.Exists(_updateCheckFile)) { var lastCheck = File.ReadAllText(_updateCheckFile).Trim(); if (lastCheck == DateTime.Today.ToString("yyyy-MM-dd")) return; } }
                catch { }
            }
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                http.DefaultRequestHeaders.Add("User-Agent", "OolioTunnelMonitor/" + AppVersion);
                var json = await http.GetStringAsync(VersionJsonUrl);
                try { Directory.CreateDirectory(Path.GetDirectoryName(_updateCheckFile)!); File.WriteAllText(_updateCheckFile, DateTime.Today.ToString("yyyy-MM-dd")); } catch { }
                using var doc = JsonDocument.Parse(json); var root = doc.RootElement;
                string latest = root.TryGetProperty("version", out var v) ? v.GetString() ?? AppVersion : AppVersion;
                string url    = root.TryGetProperty("downloadUrl", out var d) ? d.GetString() ?? "" : "";
                string notes  = root.TryGetProperty("releaseNotes", out var n) ? n.GetString() ?? "" : "";
                if (IsNewerVersion(latest, AppVersion))
                {
                    LogInfo("Update available: v" + latest);
                    string msg = $"A new version of Oolio Tunnel Monitor is available!\n\nYour version:\tv{AppVersion}\nNew version:\tv{latest}";
                    if (!string.IsNullOrWhiteSpace(notes)) msg += "\n\n" + notes;
                    msg += "\n\nOpen the download page?";
                    if (MessageBox.Show(this, msg, "Update Available", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes && !string.IsNullOrWhiteSpace(url))
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (!silent) { LogInfo("Up to date (v" + AppVersion + ")"); MessageBox.Show(this, $"Oolio Tunnel Monitor is up to date.\n\nVersion: v{AppVersion}", "No Updates", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            }
            catch (Exception ex) { if (!silent) LogWarn("Update check failed: " + ex.Message); }
        }

        private static bool IsNewerVersion(string latest, string current)
        { try { return new Version(latest) > new Version(current); } catch { return latest != current && latest != ""; } }

        private async void btnCreateTunnel_Click(object? sender, EventArgs e)   => await CreateTunnelAsync();
        private async void btnTunnelStatus_Click(object? sender, EventArgs e)    => await CheckTunnelStatusAsync();
        private async void btnTestToken_Click(object? sender, EventArgs e)       => await TestTokenAsync();
        private void btnOpenLogs_Click(object? sender, EventArgs e)               => OpenLogFolder();
        private void btnOpenConfig_Click(object? sender, EventArgs e)             => OpenConfigFolder();
        private async void btnRepair_Click(object? sender, EventArgs e)           => await RepairAsync();
        private async void btnCheckUpdates_Click(object? sender, EventArgs e)     => await CheckForUpdatesAsync(silent: false);
    }
}
