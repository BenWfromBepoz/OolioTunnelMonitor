// ============================================================
// MainForm.cs  –  Oolio Tunnel Monitor
// Architecture: sidebar-swap + content-panel-swap
// AppMode: Main | Install | Tools | Help
// ============================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using CloudflaredMonitor.Services;

namespace CloudflaredMonitor
{
    public enum PillButtonStyle   { Normal, Active }
    public enum ModernButtonStyle { Primary, Muted }
    public enum AppMode           { Main, Install, Tools, Help }

    internal static class ShapeHelper
    {
        public static GraphicsPath RoundedPath(Rectangle r, int rad)
        {
            int d = rad * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    internal sealed class OolioSidebarLogo : Panel
    {
        private Image? _logo;
        public OolioSidebarLogo()
        {
            Height = 200; BackColor = Color.Transparent;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            try
            {
                using var s = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("CloudflaredMonitor.Resources.Oolio.png");
                if (s != null) _logo = Image.FromStream(s);
            }
            catch { }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            if (_logo != null)
            {
                int pad = 20, imgArea = 120;
                float scale = Math.Min((float)(Width - pad * 2) / _logo.Width, (float)imgArea / _logo.Height);
                int w = (int)(_logo.Width * scale), h = (int)(_logo.Height * scale), x = (Width - w) / 2;
                g.DrawImage(_logo, new Rectangle(x, pad, w, h));
                using var sf = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
                using var sb = new SolidBrush(Color.FromArgb(180, 195, 220));
                g.DrawString("Oolio Tunnel Monitor", sf, sb, new RectangleF(0, imgArea + 46, Width, 20),
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }
        }
    }

    internal sealed class PillLabel : Label
    {
        private const int PillRadius = 9, PillWidth = 150;
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
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Parent?.BackColor ?? Color.White);
            bool hasPill = _pillColour != Color.Transparent && Text.Length > 0 && Text != "-";
            int ph = (int)g.MeasureString(Text, Font).Height + 8, py = (Height - ph) / 2;
            if (hasPill)
            {
                using var fg = new SolidBrush(Color.FromArgb(100, 116, 139));
                g.DrawString(Text, Font, fg, new RectangleF(0, 0, Width, Height),
                    new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap });
                using var path = ShapeHelper.RoundedPath(new Rectangle(Width - PillWidth - 4, py, PillWidth, ph), PillRadius);
                using var grad = new LinearGradientBrush(new Rectangle(Width - PillWidth - 4, py, PillWidth, ph), _pillColour, _pillColour, 45f);
                g.FillPath(grad, path);
                using var fg2 = new SolidBrush(Color.White);
                g.DrawString(Text, Font, fg2, new RectangleF(Width - PillWidth - 4, py, PillWidth, ph),
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }
            else
            {
                using var fg = new SolidBrush(ForeColor);
                g.DrawString(Text, Font, fg, new RectangleF(0, 0, Width, Height),
                    new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap });
            }
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
                topCol = _hovered ? Color.FromArgb(196, 181, 253) : Color.FromArgb(167, 139, 250);
                botCol = _hovered ? Color.FromArgb(167, 139, 250) : Color.FromArgb(124,  58, 237);
                fgCol  = Color.FromArgb(30, 10, 80);
            }
            else
            {
                topCol = _hovered ? Color.FromArgb(75, 20, 200) : Color.FromArgb(60,  6, 186);
                botCol = _hovered ? Color.FromArgb(90, 50, 160) : Color.FromArgb(75, 40, 140);
                fgCol  = Color.White;
            }
            using var grad = new LinearGradientBrush(bounds, topCol, botCol, 45f);
            g.FillPath(grad, path);
            int ph = (int)g.MeasureString(Text, Font).Height + 8, py = (Height - ph) / 2;
            using var fgb = new SolidBrush(fgCol);
            g.DrawString(Text, Font, fgb, new RectangleF(0, py, Width, ph),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        }
    }

    internal sealed class ModernButton : Button
    {
        private const int Radius = 8;
        private bool _hovered;
        private ModernButtonStyle _style = ModernButtonStyle.Primary;
        public ModernButtonStyle Style { get => _style; set { _style = value; Invalidate(); } }
        private static readonly Color _normal = Color.FromArgb(45,  52, 68);
        private static readonly Color _hover  = Color.FromArgb(60,  68, 88);
        private static readonly Color _accent = Color.FromArgb(103, 58, 182);
        public ModernButton()
        {
            FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0;
            BackColor = _normal; ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f); Cursor = Cursors.Hand;
            TextAlign = ContentAlignment.MiddleLeft; Padding = new Padding(14, 0, 0, 0);
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }
        protected override void OnMouseEnter(EventArgs e) { _hovered = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Parent?.BackColor ?? Color.White);
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = ShapeHelper.RoundedPath(bounds, Radius);
            Color bg = _style == ModernButtonStyle.Muted
                ? (_hovered ? Color.FromArgb(120, 128, 140) : Color.FromArgb(108, 117, 125))
                : (_hovered ? _hover : _normal);
            using var brush = new SolidBrush(bg);
            g.FillPath(brush, path);
            if (_style == ModernButtonStyle.Primary)
            {
                int barH = (int)(g.MeasureString(Text, Font).Height * 0.8f);
                using var ab = new SolidBrush(_accent);
                g.FillRectangle(ab, new Rectangle(0, (Height - barH) / 2, 3, barH));
            }
            using var fgb = new SolidBrush(Color.White);
            g.DrawString(Text, Font, fgb, new RectangleF(Padding.Left, 0, Width - Padding.Left, Height),
                new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap });
        }
    }

    internal sealed class ContentPanel : Panel
    {
        private const int Radius = 16;
        private static readonly Color _sidebar = Color.FromArgb(39, 46, 63);
        private static readonly Color _pageBg  = Color.FromArgb(226, 232, 240);
        private readonly System.Windows.Forms.Timer _resizeTimer;
        public ContentPanel()
        {
            DoubleBuffered = true; ResizeRedraw = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = _pageBg;
            _resizeTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _resizeTimer.Tick += (_, _) => { _resizeTimer.Stop(); Invalidate(); };
        }
        protected override void OnResize(EventArgs e)  { base.OnResize(e); _resizeTimer.Stop(); _resizeTimer.Start(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(_sidebar);
            using var path  = ShapeHelper.RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
            using var brush = new SolidBrush(_pageBg); g.FillPath(brush, path);
        }
        protected override void Dispose(bool disposing) { if (disposing) _resizeTimer.Dispose(); base.Dispose(disposing); }
    }

    internal sealed class ToggleSwitch : Control
    {
        private bool _on; private bool _hovered; private float _thumbX;
        private readonly System.Windows.Forms.Timer _anim;
        private static readonly Color _trackOn  = Color.FromArgb(103, 58, 182);
        private static readonly Color _trackOff = Color.FromArgb(180, 190, 210);
        private const int TrackH = 14, ThumbSz = 18;
        public bool IsOn { get => _on; set { if (_on == value) return; _on = value; _anim.Start(); ToggleChanged?.Invoke(this, EventArgs.Empty); } }
        public event EventHandler? ToggleChanged;
        public ToggleSwitch()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            Size = new Size(40, 22); Cursor = Cursors.Hand;
            _anim = new System.Windows.Forms.Timer { Interval = 16 };
            _anim.Tick += (_, _) =>
            {
                float target = _on ? Width - ThumbSz - 2 : 2;
                _thumbX += (target - _thumbX) * 0.3f;
                if (Math.Abs(_thumbX - target) < 0.5f) { _thumbX = target; _anim.Stop(); }
                Invalidate();
            };
            _thumbX = 2;
        }
        protected override void OnMouseEnter(EventArgs e) { _hovered = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnClick(EventArgs e) { IsOn = !_on; base.OnClick(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? Color.White);
            int ty = (Height - TrackH) / 2;
            using var tp = ShapeHelper.RoundedPath(new Rectangle(0, ty, Width, TrackH), TrackH / 2);
            using var tb = new SolidBrush(_on ? _trackOn : _trackOff); g.FillPath(tb, tp);
            int tx = (int)_thumbX, tcy = (Height - ThumbSz) / 2;
            using var tp2 = ShapeHelper.RoundedPath(new Rectangle(tx, tcy, ThumbSz, ThumbSz), ThumbSz / 2);
            using var tb2 = new SolidBrush(_hovered ? Color.FromArgb(240, 240, 255) : Color.White); g.FillPath(tb2, tp2);
        }
        protected override void Dispose(bool disposing) { if (disposing) _anim.Dispose(); base.Dispose(disposing); }
    }

    // ═══════════════════════════════════════════════════════════
    // MainForm
    // ═══════════════════════════════════════════════════════════
    public partial class MainForm : Form
    {
        private readonly CloudflaredServiceManager _monitor;
        private readonly CloudflaredInstaller     _installer;
        private readonly CloudflaredServiceManager           _serviceManager;
        private readonly FileLogger             _logger;
        private AppMode _mode = AppMode.Main;
        private static readonly string AppVersion = System.Reflection.Assembly
            .GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        private const string TunnelDetailsDir = @"C:\ProgramData\OolioTunnelMonitor";
        private const string _updateCheckFile = @"C:\ProgramData\OolioTunnelMonitor\lastUpdateCheck.json";

        private Panel pnlSidebarMain    = null!;
        private Panel pnlSidebarInstall = null!;
        private Panel pnlSidebarTools   = null!;
        private Panel pnlSidebarHelp    = null!;
        private Panel pnlInstall = null!;
        private Panel pnlTools   = null!;
        private Panel pnlHelp    = null!;
        private PillButton btnCreateTunnel = null!;
        private PillButton btnTunnelStatus = null!;
        private PillButton btnTools        = null!;
        private PillButton btnHelp         = null!;
        private PillButton btnRepair       = null!;
        private PillButton btnCheckUpdates = null!;
        private CheckBox   chkReinstall    = null!;
        private PillButton btnBackFromInstall = null!;
        private PillButton btnBackFromTools   = null!;
        private PillButton btnBackFromHelp    = null!;
        private PillButton btnOpenLogs   = null!;
        private PillButton btnOpenConfig = null!;
        private Panel            contentPanel    = null!;
        private Panel            tblMain         = null!;
        private DataGridView     dgvIngress      = null!;
        private DataGridViewTextBoxColumn colCloud = null!;
        private DataGridViewTextBoxColumn colLocal = null!;
        private Panel            pnlIngressCard  = null!;
        private Panel            pnlTokenCard    = null!;
        private Panel            pnlTokenWrap    = null!;
        private TextBox          txtApiToken     = null!;
        private ToggleSwitch     tglShowToken    = null!;
        private PillButton       btnTestToken    = null!;
        private Panel            pnlLogCard      = null!;
        private RichTextBox      txtLog          = null!;
        private PillLabel        lblRemoteStatus = null!;
        private Label            lblIdLabel      = null!;
        private Label            lblTunnelId     = null!;
        private Label            lblRemoteLabel  = null!;
        private Label            lblIngressTitle = null!;
        private Label            lblTokenTitle   = null!;
        private Label            lblLogTitle     = null!;
        private ToolTip          toolTip         = null!;
        private OolioSidebarLogo oolioLogo       = null!;
        private Panel            pnlSidebar      = null!;
        public MainForm()
        {
            _installer      = new CloudflaredInstaller();
            _serviceManager = new CloudflaredServiceManager();
            _logger         = new FileLogger();
            InitializeComponent();
            BuildSidebars();
            BuildInstallPanel();
            BuildToolsPanel();
            BuildHelpPanel();
            SetMode(AppMode.Main);
            ResizeContentPanel();
            WireEvents();
            _ = CheckForUpdatesAsync(silent: true);
        }

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

        private void BuildSidebars()
        {
            const int btnX = 12, btnW = 200, btnH = 40, btnGap = 4;
            PillButton Btn(string text, PillButtonStyle style = PillButtonStyle.Normal) =>
                new PillButton { Text = text, Size = new Size(btnW, btnH), Style = style };

            pnlSidebarMain  = new Panel { BackColor = Color.Transparent };
            btnCreateTunnel = Btn("+  Install New Tunnel");
            btnTunnelStatus = Btn("\u25cb  Check Tunnel Status");
            btnTools        = Btn("\u2630  Tools");
            btnHelp         = Btn("?  Help");
            btnRepair       = Btn("\u21ba  Repair Tunnel");
            btnCheckUpdates = Btn("\u21e7  Check for Updates");
            chkReinstall    = new CheckBox
            {
                Text = "Reinstall MSI on repair", Size = new Size(btnW - 4, 20),
                Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(180, 195, 220),
                BackColor = Color.Transparent, Checked = true
            };
            int y = 0;
            foreach (var c in new Control[] { btnCreateTunnel, btnTunnelStatus, btnTools, btnHelp, btnRepair, chkReinstall, btnCheckUpdates })
            {
                c.Location = new Point(btnX, y);
                if (c == btnTunnelStatus || c == btnHelp || c == btnRepair) y += btnGap * 3;
                y += (c is CheckBox ? 24 : btnH) + btnGap;
            }
            pnlSidebarMain.Controls.AddRange(new Control[] { btnCreateTunnel, btnTunnelStatus, btnTools, btnHelp, btnRepair, chkReinstall, btnCheckUpdates });

            pnlSidebarInstall = new Panel { BackColor = Color.Transparent };
            btnBackFromInstall = Btn("\u2190  Back to Monitor", PillButtonStyle.Active);
            btnBackFromInstall.Location = new Point(btnX, 0);
            pnlSidebarInstall.Controls.Add(btnBackFromInstall);

            pnlSidebarTools = new Panel { BackColor = Color.Transparent };
            btnBackFromTools = Btn("\u2190  Back to Monitor", PillButtonStyle.Active);
            btnOpenLogs      = Btn("\u2261  Open Logfile Folder");
            btnOpenConfig    = Btn("\u22a1  Open Config Folder");
            int ty = 0;
            foreach (var c in new Control[] { btnBackFromTools, btnOpenLogs, btnOpenConfig })
            {
                c.Location = new Point(btnX, ty);
                ty += btnH + (c == btnBackFromTools ? btnGap * 3 : btnGap);
            }
            pnlSidebarTools.Controls.AddRange(new Control[] { btnBackFromTools, btnOpenLogs, btnOpenConfig });

            pnlSidebarHelp  = new Panel { BackColor = Color.Transparent };
            btnBackFromHelp = Btn("\u2190  Back to Monitor", PillButtonStyle.Active);
            btnBackFromHelp.Location = new Point(btnX, 0);
            pnlSidebarHelp.Controls.Add(btnBackFromHelp);

            foreach (var p in new[] { pnlSidebarMain, pnlSidebarInstall, pnlSidebarTools, pnlSidebarHelp })
            {
                p.Size = new Size(pnlSidebar.Width, 600);
                p.Location = new Point(0, 200);
                pnlSidebar.Controls.Add(p);
            }
        }

        private void BuildInstallPanel()
        {
            pnlInstall = new Panel { Visible = false, BackColor = Color.Transparent, Dock = DockStyle.Fill };
            contentPanel.Controls.Add(pnlInstall);
        }

        private void BuildToolsPanel()
        {
            pnlTools = new Panel { Visible = false, BackColor = Color.Transparent, Dock = DockStyle.Fill };
            var header = new Label { Text = "Activity Log", Dock = DockStyle.Top, Height = 40, Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0) };
            var logBox = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.White, ForeColor = Color.FromArgb(15, 23, 42), Font = new Font("Cascadia Mono", 8.5f), BorderStyle = BorderStyle.None, ScrollBars = RichTextBoxScrollBars.Vertical };
            pnlTools.Controls.Add(logBox); pnlTools.Controls.Add(header);
            contentPanel.Controls.Add(pnlTools);
        }

        private void BuildHelpPanel()
        {
            pnlHelp = new Panel { Visible = false, BackColor = Color.Transparent, Dock = DockStyle.Fill };
            var helpBox = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.White, ForeColor = Color.FromArgb(15, 23, 42), Font = new Font("Segoe UI", 10f), BorderStyle = BorderStyle.None, Text = "Oolio Tunnel Monitor\r\n\r\nHelp content coming soon.\r\n\r\nFor support, visit the Cloudflare Zero Trust dashboard." };
            pnlHelp.Controls.Add(helpBox); contentPanel.Controls.Add(pnlHelp);
        }

        private void WireEvents()
        {
            btnCreateTunnel.Click += (_, _) => { if (!HasToken()) { OolioMessageBox.Show(this, "Please enter a Cloudflare API token first.", "API Token Required"); return; } SetMode(AppMode.Install); ShowInstallPanel(); };
            btnTunnelStatus.Click  += (_, _) => _ = CheckTunnelStatusAsync();
            btnTools.Click         += (_, _) => SetMode(AppMode.Tools);
            btnHelp.Click          += (_, _) => SetMode(AppMode.Help);
            btnRepair.Click        += (_, _) => _ = RepairAsync();
            btnCheckUpdates.Click  += (_, _) => _ = CheckForUpdatesAsync(silent: false);
            btnBackFromInstall.Click += (_, _) => SetMode(AppMode.Main);
            btnBackFromTools.Click   += (_, _) => SetMode(AppMode.Main);
            btnBackFromHelp.Click    += (_, _) => SetMode(AppMode.Main);
            btnOpenLogs.Click   += (_, _) => OpenLogFolder();
            btnOpenConfig.Click += (_, _) => OpenConfigFolder();
            btnTestToken.Click  += (_, _) => _ = TestTokenAsync();
            tglShowToken.ToggleChanged += (_, _) => { txtApiToken.PasswordChar = tglShowToken.IsOn ? '\0' : '*'; };
        }

        private async void ShowInstallPanel()
        {
            using var dlg = new CreateTunnelForm();
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
                await DoInstallAsync(dlg.Result);
            SetMode(AppMode.Main);
        }

        private void ResizeContentPanel()
        {
            int margin = 12;
            contentPanel.Location = new Point(pnlSidebar.Width + margin, margin);
            contentPanel.Size     = new Size(ClientSize.Width  - pnlSidebar.Width - margin * 2, ClientSize.Height - margin * 2);
            if (chkReinstall    != null) chkReinstall.Location    = new Point(chkReinstall.Location.X,    ClientSize.Height - 68);
            if (btnCheckUpdates != null) btnCheckUpdates.Location  = new Point(btnCheckUpdates.Location.X,  ClientSize.Height - 48);
        }

        protected override void OnClientSizeChanged(EventArgs e) { base.OnClientSizeChanged(e); ResizeContentPanel(); }

        private void LogInfo (string msg) => AppendLog("[INFO]  " + msg);
        private void LogWarn (string msg) => AppendLog("[WARN]  " + msg);
        private void LogError(string msg, Exception? ex = null) => AppendLog("[ERROR] " + msg + (ex != null ? " \u2014 " + ex.Message : ""));

        private void AppendLog(string message)
        {
            if (InvokeRequired) { Invoke(() => AppendLog(message)); return; }
            var line = DateTime.Now.ToString("HH:mm:ss") + "  " + message;
            _logger.Write(line);
            if (txtLog.Text.Length > 0) txtLog.AppendText(Environment.NewLine + line);
            else txtLog.Text = line;
            txtLog.ScrollToCaret();
        }

        private void ApplyBadge(PillLabel lbl, string? text, bool isService = false)
        {
            if (InvokeRequired) { Invoke(() => ApplyBadge(lbl, text, isService)); return; }
            lbl.Text = text ?? "-"; lbl.PillColour = BadgeColour(text, isService);
        }

        private static Color BadgeColour(string? v, bool svc = false)
        {
            if (string.IsNullOrWhiteSpace(v) || v == "-") return Color.Transparent;
            var s = v.ToLowerInvariant();
            if (svc) return s == "running" ? Color.FromArgb(16,140,60) : s is "stopped" or "notinstalled" ? Color.FromArgb(200,30,30) : Color.FromArgb(100,100,100);
            return s is "healthy" or "active" or "connected" or "reachable" or "tunnel ok" ? Color.FromArgb(16,140,60) : s is "inactive" or "degraded" or "down" or "unreachable" ? Color.FromArgb(200,30,30) : Color.FromArgb(100,100,100);
        }

        private static string ServiceTooltip(string? v) => (v ?? "").ToLowerInvariant() switch { "running" => "Service is running.", "notinstalled" => "Service not installed.", _ => v ?? "" };
        private static string RemoteTooltip(string? v)  => (v ?? "").ToLowerInvariant() switch { "healthy" or "active" or "connected" or "reachable" or "tunnel ok" => "Tunnel is connected.", "inactive" or "degraded" or "down" => "Tunnel is down.", _ => v ?? "" };
        private void ApplyLabel(PillLabel lbl, string text, bool isService = false) { lbl.Text = text; lbl.PillColour = BadgeColour(text, isService); toolTip.SetToolTip(lbl, isService ? ServiceTooltip(text) : RemoteTooltip(text)); }
        private string GetToken() => txtApiToken.Text.Trim();
        private bool   HasToken() => !string.IsNullOrWhiteSpace(txtApiToken.Text);

        private void PopulateIngress(IEnumerable<IngressRuleView> items)
        {
            if (InvokeRequired) { Invoke(() => PopulateIngress(items)); return; }
            dgvIngress.Rows.Clear();
            foreach (var item in items) { if (string.IsNullOrEmpty(item.CloudEndpoint)) continue; dgvIngress.Rows.Add(item.CloudEndpoint, item.LocalEndpoint); }
        }

        public void OpenLogFolder()    { try { System.Diagnostics.Process.Start("explorer.exe", _logger.LogDirectory); } catch (Exception ex) { LogError("Could not open log folder", ex); } }
        public void OpenConfigFolder() { try { Directory.CreateDirectory(TunnelDetailsDir); System.Diagnostics.Process.Start("explorer.exe", TunnelDetailsDir); } catch (Exception ex) { LogError("Could not open config folder", ex); } }

        private async Task DoInstallAsync(InstallSpec spec)
        {
            var ingressRules = spec.Routes.Select(r => new CfIngressRule { Hostname = CreateTunnelForm.BuildHostname(r, spec), Path = null, Service = "http://localhost:" + r.Port }).Where(i => !string.IsNullOrEmpty(i.Hostname)).ToList();
            var api = new CloudflareApi(GetToken());
            try
            {
                using var cts1 = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));
                var tunnel = await api.CreateTunnelAsync(spec.TunnelName, cts1.Token);
                if (tunnel?.Id == null) throw new InvalidOperationException("Tunnel creation returned no ID.");
                LogInfo("Created: " + tunnel.Name + " (" + tunnel.Id + ")");
                using var cts2 = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));
                await api.PutTunnelConfigAsync(tunnel.Id, ingressRules, cts2.Token); LogInfo("Configured " + ingressRules.Count + " route(s).");
                await SaveTunnelDetailsAsync(tunnel.Id, tunnel.Name, tunnel.Status ?? "pending", ingressRules);
                using var cts3 = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));
                var token = await api.GetTunnelTokenAsync(tunnel.Id, cts3.Token) ?? throw new InvalidOperationException("Empty token.");
                LogInfo("Downloading MSI..."); using var dlc = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(3));
                var msiPath = await _installer.DownloadMsiAsync(dlc.Token);
                await Task.Run(() => _installer.UninstallExistingMsi());
                LogInfo("Installing MSI..."); await Task.Run(() => _installer.InstallMsi(msiPath)); try { File.Delete(msiPath); } catch { }
                var exe = _installer.FindCloudflaredExeOrThrow();
                LogInfo("Installing service..."); await Task.Run(() => _installer.InstallServiceWithToken(exe, token));
                _serviceManager.StartService(); LogInfo("Installation complete."); await CheckTunnelStatusAsync();
            }
            catch (Exception ex) { LogError("Install tunnel failed", ex); }
        }

        public async Task CheckTunnelStatusAsync()
        {
            if (InvokeRequired) { await Task.Run(() => Invoke(async () => await CheckTunnelStatusAsync())); return; }
            btnTunnelStatus.Enabled = false;
            try
            {
                ApplyBadge(lblRemoteStatus, _serviceManager.GetServiceStatus(), isService: true);
                lblTunnelId.Text = "-"; ApplyBadge(lblRemoteStatus, "Checking...");
                var jp = TunnelDetailsPath(null!);
                if (!string.IsNullOrEmpty(jp) && File.Exists(jp))
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(jp)); var root = doc.RootElement;
                    if (root.TryGetProperty("TunnelId",   out var tid)) lblTunnelId.Text = tid.GetString() ?? "-";
                    if (root.TryGetProperty("TunnelName", out var tn))  lblTunnelId.Text = tn.GetString()  ?? "-";
                    if (root.TryGetProperty("Status",     out var st))  ApplyBadge(lblRemoteStatus, st.GetString());
                    if (root.TryGetProperty("Routes", out var routes))
                    {
                        var items = new List<IngressRuleView>();
                        foreach (var route in routes.EnumerateArray())
                        {
                            string h = route.TryGetProperty("Hostname", out var hp) ? hp.GetString() ?? "" : "";
                            string p = route.TryGetProperty("Path",     out var pp) ? pp.GetString() ?? "" : "";
                            string s = route.TryGetProperty("Service",  out var sp) ? sp.GetString() ?? "" : "";
                            var item = (string.IsNullOrEmpty(h) ? null : new IngressRuleView { CloudEndpoint = h, LocalEndpoint = s }); if (item != null) items.Add(item);
                        }
                        PopulateIngress(items);
                    }
                }
                if (!HasToken()) { LogInfo("No API token \u2014 limited check."); return; }
                LogInfo("API token found \u2014 querying Cloudflare...");
                var api = new CloudflareApi(GetToken()); var tid2 = lblTunnelId.Text;
                if (string.IsNullOrWhiteSpace(tid2) || tid2 == "-") { LogInfo("No tunnel ID cached."); return; }
                using var cts1 = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(20));
                var tunnel = await api.GetTunnelAsync(tid2, cts1.Token);
                if (tunnel != null) { lblTunnelId.Text = tunnel.Name ?? "-"; ApplyBadge(lblRemoteStatus, tunnel.Status ?? "-"); LogInfo("Tunnel: " + tunnel.Name + " \u2014 " + tunnel.Status); }
                using var cts2 = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(20));
                var config = await api.GetTunnelConfigAsync(tid2, cts2.Token);
                var ingress = config?.Config?.Ingress ?? new List<CfIngressRule>();
                var items2 = ingress.Select(r => (string.IsNullOrEmpty(r.Hostname) ? null : new IngressRuleView { CloudEndpoint = r.Hostname ?? "", LocalEndpoint = r.Service ?? "" })).Where(i => i != null).Cast<IngressRuleView>().ToList();
                PopulateIngress(items2); await SaveTunnelDetailsAsync(tid2, tunnel?.Name, tunnel?.Status, ingress);
                LogInfo("Saved " + items2.Count + " route(s) to cache."); LogInfo("Check complete.");
                var url = GetFirstEndpointUrl(tid2);
                if (!string.IsNullOrEmpty(url))
                {
                    using var hc = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    try
                    {
                        using var cts3 = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                        var resp = await hc.GetAsync(url, cts3.Token); int code = (int)resp.StatusCode;
                        string cfRay  = resp.Headers.Contains("CF-RAY") ? resp.Headers.GetValues("CF-RAY").First() : "";
                        string server = resp.Headers.Contains("Server") ? resp.Headers.GetValues("Server").First() : "unknown";
                        if      (code == 200)              { ApplyBadge(lblRemoteStatus, "Tunnel OK");   LogInfo("HTTP " + code + " \u2014 Cloudflare responded (Server: " + server + (cfRay != "" ? " cfRay !=" + cfRay + ")" : ")")); }
                        else if (code >= 200 && code < 500){ ApplyBadge(lblRemoteStatus, "Reachable");   LogInfo("HTTP " + code + " \u2014 endpoint responded. Server: " + server); }
                        else                               { ApplyBadge(lblRemoteStatus, "Unreachable"); LogWarn("HTTP " + code + " \u2014 endpoint may not be functioning."); }
                    }
                    catch (System.Net.Http.HttpRequestException hrEx) { ApplyBadge(lblRemoteStatus, "Unreachable"); LogWarn("HTTP check failed: " + hrEx.Message); }
                    catch (TaskCanceledException)                      { ApplyBadge(lblRemoteStatus, "Unreachable"); LogWarn("HTTP check timed out after 10s."); }
                }
                else LogInfo("No endpoint URL available.");
            }
            catch (Exception ex) { LogError("Check failed", ex); }
            finally { btnTunnelStatus.Enabled = true; }
        }

        public async Task RepairAsync()
        {
            if (!HasToken()) { LogWarn("Enter an API token first."); return; }
            if (OolioMessageBox.Show(this, "This will stop, uninstall and reinstall the cloudflared service.\n\nContinue?", "Confirm Repair", yesNo: true) != DialogResult.Yes) return;
            var api = new CloudflareApi(GetToken()); btnRepair.Enabled = false;
            try
            {
                var jp = TunnelDetailsPath(null!);
                if (!File.Exists(jp!)) { LogWarn("No cached tunnel details found."); return; }
                using var doc = JsonDocument.Parse(File.ReadAllText(jp!)); var root = doc.RootElement;
                string? tid = root.TryGetProperty("TunnelId", out var t) ? t.GetString() : null;
                if (string.IsNullOrEmpty(tid)) { LogWarn("No tunnel ID in cache."); return; }
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));
                var token = await api.GetTunnelTokenAsync(tid, cts.Token) ?? throw new InvalidOperationException("Empty token.");
                LogInfo("Downloading MSI..."); using var dlc = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(3));
                var msiPath = await _installer.DownloadMsiAsync(dlc.Token);
                await Task.Run(() => _installer.UninstallExistingMsi());
                LogInfo("Installing MSI..."); await Task.Run(() => _installer.InstallMsi(msiPath)); try { File.Delete(msiPath); } catch { }
                var exe = _installer.FindCloudflaredExeOrThrow();
                LogInfo("Installing service..."); await Task.Run(() => _installer.InstallServiceWithToken(exe, token));
                _serviceManager.StartService(); LogInfo("Repair complete."); await CheckTunnelStatusAsync();
            }
            catch (Exception ex) { LogError("Repair failed", ex); }
            finally { btnRepair.Enabled = true; }
        }

        private async Task TestTokenAsync()
        {
            if (!HasToken()) { LogWarn("Enter an API token first."); return; }
            btnTestToken.Enabled = false;
            try
            {
                var api = new CloudflareApi(GetToken());
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(15));
                var valid = await api.ValidateTokenAsync(cts.Token);
                if (valid) { LogInfo("Token is valid."); ApplyBadge(lblRemoteStatus, "Token OK"); }
                else       { LogWarn("Token is invalid or lacks permissions."); ApplyBadge(lblRemoteStatus, "Token Invalid"); }
            }
            catch (Exception ex) { LogError("Token test failed", ex); }
            finally { btnTestToken.Enabled = true; }
        }

        public async Task CheckForUpdatesAsync(bool silent = false)
        {
            try
            {
                string? json = null; bool shouldCheck = true;
                if (File.Exists(_updateCheckFile))
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(_updateCheckFile)); var root = doc.RootElement;
                    if (root.TryGetProperty("lastCheck", out var lc) && DateTime.TryParse(lc.GetString(), out var lastCheck))
                        shouldCheck = (DateTime.UtcNow - lastCheck).TotalHours >= 24;
                    if (!shouldCheck && root.TryGetProperty("cachedResponse", out var cr)) json = cr.GetString();
                }
                if (shouldCheck)
                {
                    using var hc = new System.Net.Http.HttpClient();
                    hc.DefaultRequestHeaders.Add("User-Agent", "OolioTunnelMonitor/" + AppVersion);
                    using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                    json = await hc.GetStringAsync("https://api.github.com/repos/BenWfromBepoz/CloudflaredMonitor/releases/latest", cts.Token);
                    Directory.CreateDirectory(Path.GetDirectoryName(_updateCheckFile)!);
                    File.WriteAllText(_updateCheckFile, JsonSerializer.Serialize(new { lastCheck = DateTime.UtcNow.ToString("o"), cachedResponse = json }));
                }
                if (json == null) return;
                using var doc2 = JsonDocument.Parse(json); var root2 = doc2.RootElement;
                string latest = root2.TryGetProperty("version",      out var v) ? v.GetString() ?? AppVersion : AppVersion;
                string url    = root2.TryGetProperty("downloadUrl",  out var d) ? d.GetString() ?? "" : "";
                string notes  = root2.TryGetProperty("releaseNotes", out var n) ? n.GetString() ?? "" : "";
                if (IsNewerVersion(latest, AppVersion))
                {
                    LogInfo("Update available: v" + latest);
                    string msg = $"A new version is available!\n\nYour version:\tv{AppVersion}\nNew version:\tv{latest}";
                    if (!string.IsNullOrWhiteSpace(notes)) msg += "\n\n" + notes;
                    msg += "\n\nOpen the download page?";
                    if (OolioMessageBox.Show(this, msg, "Update Available", yesNo: true) == DialogResult.Yes && !string.IsNullOrWhiteSpace(url))
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (!silent) OolioMessageBox.Show(this, $"Oolio Tunnel Monitor is up to date.\n\nVersion: v{AppVersion}", "No Updates");
            }
            catch (Exception ex) { if (!silent) LogWarn("Update check failed: " + ex.Message); }
        }

        private static bool IsNewerVersion(string latest, string current)
        { try { return new Version(latest) > new Version(current); } catch { return latest != current && latest != ""; } }

        private static string? TunnelDetailsPath(string tunnelId)
        {
            try
            {
                if (!Directory.Exists(TunnelDetailsDir)) return null;
                var files = Directory.GetFiles(TunnelDetailsDir, "*.json").Where(f => !f.EndsWith("lastUpdateCheck.json")).OrderByDescending(File.GetLastWriteTime).ToArray();
                return files.Length > 0 ? files[0] : null;
            }
            catch { return null; }
        }

        private static string TunnelDetailsPath_Write(string tunnelId) => Path.Combine(TunnelDetailsDir, "tunnel-" + tunnelId + ".json");

        private string? GetFirstEndpointUrl(string tunnelId)
        {
            try
            {
                var jp = TunnelDetailsPath(tunnelId); if (!File.Exists(jp)) return null;
                using var doc = JsonDocument.Parse(File.ReadAllText(jp!)); var root = doc.RootElement;
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

        private async Task SaveTunnelDetailsAsync(string tunnelId, string? tunnelName, string? status, List<CfIngressRule> ingressRules)
        {
            Directory.CreateDirectory(TunnelDetailsDir);
            await File.WriteAllTextAsync(TunnelDetailsPath_Write(tunnelId), JsonSerializer.Serialize(
                new { TunnelId = tunnelId, TunnelName = tunnelName, Status = status, Retrieved = DateTime.UtcNow.ToString("o"),
                      Routes = ingressRules.ConvertAll(r => new { Hostname = r.Hostname ?? "", Path = r.Path ?? "", r.Service }) },
                new JsonSerializerOptions { WriteIndented = true }));
            LogInfo("Config saved: " + TunnelDetailsPath_Write(tunnelId));
        }

        private void btnTunnelStatus_Click(object? sender, EventArgs e) => _ = CheckTunnelStatusAsync();
        private void btnRepair_Click(object? sender, EventArgs e)       => _ = RepairAsync();
        private void btnOpenLogs_Click(object? sender, EventArgs e)     => OpenLogFolder();
        private void btnOpenConfig_Click(object? sender, EventArgs e)   => OpenConfigFolder();
        private void btnCheckUpdates_Click(object? sender, EventArgs e) => _ = CheckForUpdatesAsync(silent: false);
        private void btnCreateTunnel_Click(object? sender, EventArgs e)
        {
            if (!HasToken()) { OolioMessageBox.Show(this, "Please enter a Cloudflare API token first.", "API Token Required"); return; }
            SetMode(AppMode.Install); ShowInstallPanel();
        }
    }
}
