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
    internal sealed class OolioSidebarLogo : Control
    {
        private static readonly Image? _logo = LoadLogo();
        private static Image? LoadLogo()
        {
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                var stream = asm.GetManifestResourceStream("CloudflaredMonitor.Resources.OolioTaskbar256.png")
                          ?? asm.GetManifestResourceStream("CloudflaredMonitor.Resources.Oolio.png");
                return stream != null ? Image.FromStream(stream) : null;
            }
            catch { return null; }
        }
        public OolioSidebarLogo()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
        
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        
            if (_logo == null) return;
        
            const int pad = 6;
            const int subtitleH = 26;
        
            int avail = Math.Min(Width, Height) - pad * 2;
            if (avail <= 0) return;
        
            float scale = Math.Min(avail / (float)_logo.Width, avail / (float)_logo.Height);
            int w = (int)(_logo.Width * scale);
            int h = (int)(_logo.Height * scale);
            int x = (Width - w) / 2;
        
            int imageOffsetY = -10;
            int imageY = Math.Max(0, pad + imageOffsetY);
            int spacing = -60; // negative = overlap, positive = gap
            int textY = imageY + h + spacing;
        
            g.DrawImage(_logo, new Rectangle(x, imageY, w, h));
        
            using var sf = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
            using var sb = new SolidBrush(Color.FromArgb(180, 195, 220));
            using var shadowBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
            using var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

            g.DrawString("Oolio Tunnel Monitor", sf, shadowBrush,
                new RectangleF(0, textY + 1, Width, subtitleH),
                format);
            
            g.DrawString("Oolio Tunnel Monitor", sf, sb,
                new RectangleF(0, textY, Width, subtitleH),
                format);
        }
    }

    internal static class ShapeHelper
    {
        public static GraphicsPath RoundedPath(Rectangle r, int rad)
        { int d = rad * 2; var p = new GraphicsPath(); p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p; }
    }

    internal sealed class PillLabel : Label
    {
        private const int PillRadius = 9; private const int PillWidth = 150;
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
            bool hasPill = _pillColour != Color.Transparent && Text.Length > 0 && Text != "-";
            if (!hasPill) { using var fg = new SolidBrush(Color.FromArgb(100, 116, 139)); g.DrawString(Text, Font, fg, new RectangleF(0, 0, Width, Height), new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center }); return; }
            int pw = PillWidth, ph = (int)g.MeasureString(Text, Font).Height + 8, py = (Height - ph) / 2;
            var rect = new Rectangle(0, py, pw, ph);
            Color topCol, botCol;
            var r = _pillColour.R; var gC = _pillColour.G; var b = _pillColour.B;
            if (gC > r && gC > b)     { topCol = Color.FromArgb(125, 230, 135); botCol = Color.FromArgb(90, 200, 100); }
            else if (r > gC && r > b) { topCol = Color.FromArgb(230, 135, 125); botCol = Color.FromArgb(200, 80, 70); }
            else                      { topCol = Color.FromArgb(240, 230, 90);  botCol = Color.FromArgb(230, 180, 50); }
            using var grad = new LinearGradientBrush(new Point(0, py), new Point(pw, py + ph), topCol, botCol);
            using var path = ShapeHelper.RoundedPath(rect, PillRadius);
            g.FillPath(grad, path);
            var gr2 = new Rectangle(0, py, pw, ph / 2);
            using var gloss = new LinearGradientBrush(gr2, Color.FromArgb(80, Color.White), Color.FromArgb(0, Color.White), LinearGradientMode.Vertical);
            g.SetClip(path); g.FillRectangle(gloss, gr2); g.ResetClip();
            using var fgB = new SolidBrush(Color.White);
            g.DrawString(Text, Font, fgB, new RectangleF(0, py, pw, ph), new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        }
    }

    internal sealed class PillButton : Button
    {
        private const int Radius = 13; private bool _hovered;
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
            var topCol = _hovered ? Color.FromArgb(160, 115, 240) : Color.FromArgb(140, 95, 220);
            var botCol = _hovered ? Color.FromArgb(90, 50, 160)   : Color.FromArgb(75, 40, 140);
            using var grad = new LinearGradientBrush(new Point(0, 0), new Point(Width, Height), topCol, botCol);
            g.FillPath(grad, path);
            if (Height > 4) { var gr = new Rectangle(0, 0, Width, Height / 2); using var gloss = new LinearGradientBrush(gr, Color.FromArgb(80, Color.White), Color.FromArgb(0, Color.White), LinearGradientMode.Vertical); g.SetClip(path); g.FillRectangle(gloss, gr); g.ResetClip(); }
            using var fg = new SolidBrush(Color.White);
            g.DrawString(Text, Font, fg, new RectangleF(0, 0, Width, Height), new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
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
            g.DrawString(Text, Font, fg, new RectangleF(Padding.Left + 4, 0, Width - Padding.Left - 8, Height), new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap });
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
            for (int i = 3; i >= 1; i--) { using var sb = new SolidBrush(Color.FromArgb(18, 0, 0, 0)); using var sp = RRP(new Rectangle(i, i, Width - i * 2, Height - i * 2), Radius); g.FillPath(sb, sp); }
            using var wb = new SolidBrush(Color.White); using var wp = RRP(new Rectangle(0, 0, Width - 1, Height - 1), Radius); g.FillPath(wb, wp);
        }
        protected override void Dispose(bool disposing) { if (disposing) _resizeTimer.Dispose(); base.Dispose(disposing); }
        private static GraphicsPath RRP(Rectangle r, int rad)
        { int d = rad * 2; var p = new GraphicsPath(); p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p; }
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
        protected override void OnResize(EventArgs e) { base.OnResize(e); _resizeTimer.Stop(); _resizeTimer.Start(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(_sidebar);
            using var path = ShapeHelper.RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
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
        private const int TrackH = 14; private const int ThumbSz = 18;
        public bool IsOn { get => _on; set { if (_on == value) return; _on = value; _anim.Start(); ToggledChanged?.Invoke(this, EventArgs.Empty); } }
        public event EventHandler? ToggledChanged;
        public ToggleSwitch()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent; Cursor = Cursors.Hand; Size = new Size(46, 22);
            _thumbX = 2f;
            _anim = new System.Windows.Forms.Timer { Interval = 16 };
            _anim.Tick += (_, _) => { float t = TargetX, d = (t - _thumbX) * 0.35f; if (Math.Abs(d) < 0.5f) { _thumbX = t; _anim.Stop(); } else _thumbX += d; Invalidate(); };
        }
        private float TargetX => _on ? Width - ThumbSz - 2 : 2f;
        protected override void OnMouseEnter(EventArgs e) { _hovered = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnClick(EventArgs e) { IsOn = !IsOn; base.OnClick(e); }
        protected override void OnPaintBackground(PaintEventArgs e) { e.Graphics.Clear(Parent?.BackColor ?? Color.White); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? Color.White);
            int ty = (Height - TrackH) / 2;
            var trackCol = _on ? _trackOn : _trackOff;
            if (_hovered) trackCol = Color.FromArgb(Math.Min(255, trackCol.R + 20), Math.Min(255, trackCol.G + 20), Math.Min(255, trackCol.B + 20));
            using var tp = ShapeHelper.RoundedPath(new Rectangle(0, ty, Width - 1, TrackH), TrackH / 2);
            using var tb = new SolidBrush(trackCol); g.FillPath(tb, tp);
            int thumbY = (Height - ThumbSz) / 2;
            using var shadow = new SolidBrush(Color.FromArgb(40, 0, 0, 0)); g.FillEllipse(shadow, _thumbX, thumbY + 1, ThumbSz, ThumbSz);
            using var thumb  = new SolidBrush(Color.White);                  g.FillEllipse(thumb,  _thumbX, thumbY,     ThumbSz, ThumbSz);
        }
        protected override void Dispose(bool disposing) { if (disposing) _anim.Dispose(); base.Dispose(disposing); }
    }

    internal sealed class OolioMessageBox : Form
    {
        private static readonly Color _sidebar = Color.FromArgb(39, 46, 63);
        private OolioMessageBox(string title, string message, bool yesNo)
        {
            Text = title; FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.CenterParent;
            BackColor = _sidebar; Size = new Size(460, 220); MinimumSize = new Size(360, 180);
            var content = new ContentPanel { Dock = DockStyle.Fill }; Controls.Add(content);
            content.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59), Location = new Point(20, 18), Size = new Size(390, 24), BackColor = Color.Transparent });
            content.Controls.Add(new Label { Text = message, Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(20, 50), Size = new Size(420, 100), BackColor = Color.Transparent, AutoSize = false });
            if (yesNo)
            {
                var y = new PillButton { Text = "Yes", Size = new Size(90, 32), Location = new Point(250, 168) }; y.Click += (_, _) => { DialogResult = DialogResult.Yes; Close(); }; content.Controls.Add(y);
                var n = new PillButton { Text = "No",  Size = new Size(90, 32), Location = new Point(352, 168) }; n.Click += (_, _) => { DialogResult = DialogResult.No;  Close(); }; content.Controls.Add(n);
            }
            else { var ok = new PillButton { Text = "OK", Size = new Size(90, 32), Location = new Point(352, 168) }; ok.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); }; content.Controls.Add(ok); }
            var x = new Label { Text = "\u00d7", Font = new Font("Segoe UI", 13f), ForeColor = Color.FromArgb(120, 140, 160), Location = new Point(424, 10), Size = new Size(24, 24), BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand };
            x.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); }; content.Controls.Add(x);
            bool drag = false; Point ds = Point.Empty;
            content.MouseDown += (_, me) => { if (me.Button == MouseButtons.Left) { drag = true; ds = me.Location; } };
            content.MouseMove += (_, me) => { if (drag) Location = new Point(Location.X + me.X - ds.X, Location.Y + me.Y - ds.Y); };
            content.MouseUp   += (_, _)  => drag = false;
        }
        public static DialogResult Show(IWin32Window owner, string message, string title, bool yesNo = false)
        { using var d = new OolioMessageBox(title, message, yesNo); return d.ShowDialog(owner); }
    }

    internal sealed class IngressItem
    {
        public string CloudEndpoint { get; }
        public string LocalEndpoint { get; }
        public IngressItem(string cloud, string local) { CloudEndpoint = cloud; LocalEndpoint = local; }
        public bool IsCatchAll => LocalEndpoint.StartsWith("http_status:", StringComparison.OrdinalIgnoreCase) || (CloudEndpoint == "*" && string.IsNullOrWhiteSpace(LocalEndpoint));
    }

    internal static class IngressHelper
    {
        public static IngressItem? Build(string? hostname, string? path, string? service)
        {
            string svc = service ?? ""; if (svc.StartsWith("http_status:", StringComparison.OrdinalIgnoreCase)) return null;
            string host = hostname ?? ""; if (host == "*" && string.IsNullOrWhiteSpace(svc)) return null;
            string cloud = host; if (!string.IsNullOrWhiteSpace(path) && path != "*") cloud = host.TrimEnd('/') + "/" + path.TrimStart('/');
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
        private const string AppVersion     = "1.2.1.0";
        private const string VersionJsonUrl = "https://raw.githubusercontent.com/BenWfromBepoz/OolioTunnelMonitor/refs/heads/main/version.json";

        private static readonly string _updateCheckFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Bepoz", "CloudflaredMonitor", "last-update-check.txt");

        private static string TunnelDetailsDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Bepoz", "CloudflaredMonitor", "tunnel-details");
        private static string TunnelDetailsPath(string id) => Path.Combine(TunnelDetailsDir, id + ".json");

        // ── Install panel (inline, replaces main content) ─────────────────────
        private InstallPanel? _installPanel;

        public MainForm()
        {
            // Fix #3: enable WS_EX_COMPOSITED to suppress flicker during layout
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            InitializeComponent();
            _exporter = new DiagnosticsExporter(_logger);
            dgvIngress.CellPainting += DgvIngress_CellPainting;
            this.FormClosing += (_, e) => { e.Cancel = true; Hide(); };
            tglShowToken.ToggledChanged += (_, _) => { txtApiToken.UseSystemPasswordChar = !tglShowToken.IsOn; };
        }

        // Fix #3: override CreateParams to add WS_EX_COMPOSITED — eliminates repaint flicker
        protected override CreateParams CreateParams
        {
            get { var cp = base.CreateParams; cp.ExStyle |= 0x02000000; return cp; }
        }

        // Fix #3: use OnLoad (fires before first paint) instead of OnShown
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SuspendLayout();
            ResizeContentPanel();
            contentPanel.Visible = true;
            ResumeLayout(false);
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
            if (ex != null && ex.Message.Contains("403") && ex.Message.Contains("10000")) detail += " | TOKEN SCOPE: Needs Cloudflare Tunnel:Edit permission.";
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
            return s is "healthy" or "active" or "connected" or "reachable" or "tunnel ok" ? Color.FromArgb(16,140,60) : s is "inactive" or "degraded" or "down" or "unreachable" ? Color.FromArgb(200,30,30) : Color.FromArgb(180,100,0);
        }
        private static string ServiceTooltip(string? v) => (v ?? "").ToLowerInvariant() switch { "running" => "Service is running.", "notinstalled" => "Service not installed.", "stopped" => "Service stopped.", _ => "Unknown." };
        private static string RemoteTooltip(string? v)  => (v ?? "").ToLowerInvariant() switch { "healthy" or "active" or "connected" or "reachable" or "tunnel ok" => "Tunnel is reachable.", "inactive" or "degraded" or "down" or "unreachable" => "Tunnel is not reachable.", _ => "Status unknown." };
        private void ApplyBadge(PillLabel lbl, string text, bool isService = false)
        { lbl.Text = text; lbl.PillColour = BadgeColour(text, isService); toolTip.SetToolTip(lbl, isService ? ServiceTooltip(text) : RemoteTooltip(text)); }

        private string GetToken() => txtApiToken.Text.Trim();
        private bool   HasToken() => !string.IsNullOrWhiteSpace(txtApiToken.Text);

        private void PopulateIngress(IEnumerable<IngressItem> items)
        { dgvIngress.Rows.Clear(); foreach (var item in items) { if (item.IsCatchAll) continue; dgvIngress.Rows.Add(item.CloudEndpoint, item.LocalEndpoint); } }

        public void OpenLogFolder()    { try { Process.Start("explorer.exe", _logger.LogDirectory); } catch (Exception ex) { LogError("Could not open log folder", ex); } }
        public void OpenConfigFolder() { try { Directory.CreateDirectory(TunnelDetailsDir); Process.Start("explorer.exe", TunnelDetailsDir); } catch (Exception ex) { LogError("Could not open config folder", ex); } }

        // ── Show/hide inline install panel (#4) ───────────────────────────────
        private void ShowInstallPanel()
        {
            if (_installPanel == null)
            {
                _installPanel = new InstallPanel(GetToken());
                _installPanel.Dock = DockStyle.Fill;
                _installPanel.InstallRequested += async (spec) => await DoInstallAsync(spec);
                _installPanel.Cancelled += () => HideInstallPanel();
                contentPanel.Controls.Add(_installPanel);
            }
            else
            {
                _installPanel.Reset(GetToken());
            }
            tblMain.Visible      = false;
            _installPanel.Visible = true;
            _installPanel.BringToFront();
            btnCreateTunnel.Text = "\u2190  Back to Monitor";
        }

        private void HideInstallPanel()
        {
            if (_installPanel != null) _installPanel.Visible = false;
            tblMain.Visible          = true;
            btnCreateTunnel.Text     = "+  Install New Tunnel";
        }

        private async Task DoInstallAsync(InstallSpec spec)
        {
            HideInstallPanel();
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
                try { var v = await api.VerifyTokenAsync(ctsV.Token); LogInfo("Token: " + (v?.Status ?? "unknown") + (v?.ExpiresOn != null ? " | Expires: " + v.ExpiresOn : "")); } catch (Exception vEx) { LogWarn("Could not verify: " + vEx.Message); }
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
                    LogInfo("Add a token above for authoritative status, route config and auto-save.");
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
                            if (hasCf)
                            {
                                ApplyBadge(lblRemoteStatus, "Tunnel OK");
                                LogInfo("HTTP " + code + " — Cloudflare responded (Server: " + server + (cfRay != "" ? ", CF-RAY: " + cfRay : "") + ")");
                                if (code == 502) LogWarn("  502 — tunnel working, origin not responding.");
                                else if (code == 503) LogWarn("  503 — tunnel working, origin temporarily unavailable.");
                                else if (code >= 200 && code < 300) LogInfo("  " + code + " — origin also responding normally.");
                                else LogInfo("  " + code + " — origin returned this status (tunnel fine).");
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
                LogInfo("Saved " + items.Count + " route(s) to cache."); LogInfo("Check complete.");
            }
            catch (Exception ex) { LogError("Check failed", ex); }
            finally { btnTunnelStatus.Enabled = true; }
        }

        public async Task RepairAsync()
        {
            if (!HasToken()) { LogWarn("Enter an API token first."); return; }
            if (OolioMessageBox.Show(this, "This will stop, uninstall and reinstall the cloudflared service.\n\nContinue?", "Confirm Repair", yesNo: true) != DialogResult.Yes) { LogInfo("Repair cancelled."); return; }
            var api = new CloudflareApi(GetToken()); btnRepair.Enabled = false;
            try
            {
                string? tid = _currentStatus?.TunnelId;
                if (tid == null && Directory.Exists(TunnelDetailsDir))
                {
                    var files = Directory.GetFiles(TunnelDetailsDir, "*.json");
                    if (files.Length == 1) { tid = Path.GetFileNameWithoutExtension(files[0]); LogInfo("Using cached ID: " + tid); }
                    else if (files.Length > 1) { LogWarn("Multiple cached tunnels. Run Check Tunnel Status first."); return; }
                }
                if (tid == null) { LogError("No tunnel ID found."); return; }
                LogInfo("Repairing " + tid + "...");
                _serviceManager.StopServiceBestEffort(); _serviceManager.KillCloudflaredProcess(); _serviceManager.DeleteService();
                await Task.Run(() => _installer.UninstallExistingMsi());
                string msiPath; using (var dlc = new CancellationTokenSource(TimeSpan.FromMinutes(5))) msiPath = await _installer.DownloadMsiAsync(dlc.Token);
                await Task.Run(() => _installer.InstallMsi(msiPath)); try { File.Delete(msiPath); } catch { }
                var exe = _installer.FindCloudflaredExeOrThrow();
                string newToken; using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30))) newToken = await api.GetTunnelTokenAsync(tid, cts.Token) ?? throw new InvalidOperationException("Empty token.");
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
                if (_currentStatus == null) { OolioMessageBox.Show(this, "Check tunnel status first.", "Export"); return; }
                var lines = new List<string>();
                foreach (DataGridViewRow row in dgvIngress.Rows) lines.Add((row.Cells[0].Value?.ToString() ?? "") + "  ->  " + (row.Cells[1].Value?.ToString() ?? ""));
                var zipPath = _exporter.Export(_currentStatus, _uiLogs, lines);
                OolioMessageBox.Show(this, "Exported to:\n" + zipPath, "Export Complete");
            }
            catch (Exception ex) { OolioMessageBox.Show(this, "Export failed: " + ex.Message, "Export Error"); }
        }

        public async Task CheckForUpdatesAsync(bool silent = false)
        {
            if (silent) { try { if (File.Exists(_updateCheckFile)) { var lc = File.ReadAllText(_updateCheckFile).Trim(); if (lc == DateTime.Today.ToString("yyyy-MM-dd")) return; } } catch { } }
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
                    string msg = $"A new version is available!\n\nYour version:\tv{AppVersion}\nNew version:\tv{latest}";
                    if (!string.IsNullOrWhiteSpace(notes)) msg += "\n\n" + notes;
                    msg += "\n\nOpen the download page?";
                    if (OolioMessageBox.Show(this, msg, "Update Available", yesNo: true) == DialogResult.Yes && !string.IsNullOrWhiteSpace(url))
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (!silent) OolioMessageBox.Show(this, $"Oolio Tunnel Monitor is up to date.\n\nVersion: v{AppVersion}", "No Updates");
            }
            catch (Exception ex) { if (!silent) LogWarn("Update check failed: " + ex.Message); }
        }

        private static bool IsNewerVersion(string latest, string current)
        { try { return new Version(latest) > new Version(current); } catch { return latest != current && latest != ""; } }

        // #4: Install button now toggles inline panel instead of showing dialog
        private void btnCreateTunnel_Click(object? sender, EventArgs e)
        {
            if (tblMain.Visible)
            {
                if (!HasToken()) { OolioMessageBox.Show(this, "Please enter a Cloudflare API token first.", "API Token Required"); return; }
                ShowInstallPanel();
            }
            else HideInstallPanel();
        }

        private async void btnTunnelStatus_Click(object? sender, EventArgs e)    => await CheckTunnelStatusAsync();
        private async void btnTestToken_Click(object? sender, EventArgs e)        => await TestTokenAsync();
        private void btnOpenLogs_Click(object? sender, EventArgs e)                => OpenLogFolder();
        private void btnOpenConfig_Click(object? sender, EventArgs e)              => OpenConfigFolder();
        private async void btnRepair_Click(object? sender, EventArgs e)            => await RepairAsync();
        private async void btnCheckUpdates_Click(object? sender, EventArgs e)      => await CheckForUpdatesAsync(silent: false);
    }
}
