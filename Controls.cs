using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace OolioTunnelMonitor
{

    internal static class UiFactory
    {
        public static readonly Color Lavender   = Color.FromArgb(237, 233, 254);
        public static readonly Color Purple200  = Color.FromArgb(196, 181, 253);
        public static readonly Color Purple700  = Color.FromArgb(109,  40, 217);
        public static readonly Color SlateKey   = Color.FromArgb(100, 116, 139);
        public static readonly Color Slate900   = Color.FromArgb( 15,  23,  42);
        public static readonly Color PageBg     = Color.FromArgb(226, 232, 240);
        public static readonly Color White      = Color.White;

        public static Panel StyledTextBox(TextBox txt, int x, int y, int w, int h = 28)
        {
            txt.BorderStyle = BorderStyle.None;
            txt.BackColor   = Lavender;
            txt.ForeColor   = Slate900;
            txt.Font        = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            txt.Dock        = DockStyle.Fill;
            txt.Margin      = new Padding(6, 0, 6, 0);
            var wrap = new BorderPanel { Location = new Point(x, y), Size = new Size(w, h) };
            wrap.Controls.Add(txt);
            return wrap;
        }

        public static Label MakeLabel(string text, int x, int y, int w = 300)
        {
            return new Label
            {
                Text      = text,
                Location  = new Point(x, y),
                Size      = new Size(w, 18),
                ForeColor = SlateKey,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                BackColor = Color.Transparent
            };
        }

        public static Panel StyledCombo(ComboBox cmb, int x, int y, int w, int h = 28)
        {
            cmb.FlatStyle     = FlatStyle.Flat;
            cmb.BackColor     = Lavender;
            cmb.ForeColor     = Slate900;
            cmb.Font          = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            cmb.Dock          = DockStyle.Fill;
            cmb.Margin        = new Padding(4, 2, 4, 2);
            cmb.DrawMode      = DrawMode.OwnerDrawFixed;
            cmb.ItemHeight    = 20;
            cmb.DrawItem     += ComboDrawItem;
            var wrap = new BorderPanel { Location = new Point(x, y), Size = new Size(w, h) };
            wrap.Controls.Add(cmb);
            return wrap;
        }

        private static void ComboDrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            if (sender is not ComboBox cmb) return;
            bool selected = (e.State & DrawItemState.Selected) != 0;
            using var bgBrush = new SolidBrush(selected ? Purple200 : Lavender);
            e.Graphics.FillRectangle(bgBrush, e.Bounds);
            using var fgBrush = new SolidBrush(Slate900);
            var sf = new StringFormat { LineAlignment = StringAlignment.Center };
            var text = cmb.Items[e.Index]?.ToString() ?? "";
            e.Graphics.DrawString(text, cmb.Font, fgBrush,
                new RectangleF(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 4, e.Bounds.Height), sf);
        }
    }


    internal static class ShapeHelper
    {
        public static GraphicsPath RoundedPath(Rectangle r, int rad)
        { int d = rad * 2; var p = new GraphicsPath(); p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p; }
    }


    internal sealed class OolioSidebarLogo : Control
    {
        private static readonly Image? _logo = LoadLogo();
        private static Image? LoadLogo()
        {
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                var stream = asm.GetManifestResourceStream("OolioTunnelMonitor.Resources.OolioTaskbar256.png")
                          ?? asm.GetManifestResourceStream("OolioTunnelMonitor.Resources.Oolio.png");
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
            const int subtitleH = 10;
            int imgArea = Height - subtitleH;
            int availW = Width - 24, availH = imgArea -24;
            if (availW <= 0 || availH <= 0) return;
            float baseScale = Math.Min(availW / (float)_logo.Width, availH / (float)_logo.Height);
            float scale = baseScale * 1f;
            int w = (int)(_logo.Width * scale), h = (int)(_logo.Height * scale);
            int x = (Width - w) / 2, y = 4 + (availH - h) / 2;
            g.DrawImage(_logo, new Rectangle(x, y, w, h));
            using var sf = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
            using var sb = new SolidBrush(Color.FromArgb(180, 195, 220));
            g.DrawString("Oolio Tunnel Monitor", sf, sb, new RectangleF(0, imgArea - 40, Width, subtitleH),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        }
    }


    internal sealed class ToggleSwitch : Control
    {
        private bool _checked; private bool _hovered;
        public bool Checked { get => _checked; set { _checked = value; CheckedChanged?.Invoke(this, EventArgs.Empty); Invalidate(); } }
        public event EventHandler? CheckedChanged;
        public ToggleSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent; Size = new Size(40, 20); Cursor = Cursors.Hand;
        }
        protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnClick(EventArgs e) { Checked = !Checked; base.OnClick(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            Color trackCol = _checked ? (_hovered ? Color.FromArgb(130, 80, 220) : Color.FromArgb(109, 40, 217)) : (_hovered ? Color.FromArgb(90, 100, 120) : Color.FromArgb(71, 85, 105));
            using var tp = ShapeHelper.RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), Height / 2);
            using var tb = new SolidBrush(trackCol); g.FillPath(tb, tp);
            int d = Height - 6, tx = _checked ? Width - d - 3 : 3;
            using var thb = new SolidBrush(Color.White); g.FillEllipse(thb, tx, 3, d, d);
        }
    }


    internal sealed class PillLabel : Label
    {
        private const int PillRadius = 9, PillWidth = 120;
        private Color _pillColour = Color.Transparent;
        public Color PillColour { get => _pillColour; set { _pillColour = value; Invalidate(); } }
        public PillLabel()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            BackColor = Color.Transparent; TextAlign = ContentAlignment.MiddleLeft; AutoSize = false;
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
        }
        protected override void OnPaintBackground(PaintEventArgs e) { e.Graphics.Clear(Color.White); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            bool hasPill = _pillColour != Color.Transparent && Text.Length > 0 && Text != "-";
            if (!hasPill)
            {
                using var fg = new SolidBrush(Color.FromArgb(100, 116, 139));
                g.DrawString(Text, Font, fg, new RectangleF(0, 0, Width, Height), new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });
                return;
            }
            int pw = PillWidth, ph = (int)g.MeasureString(Text, Font).Height + 8, py = (Height - ph) / 2;
            var rect = new Rectangle(0, py, pw, ph);
            var r = _pillColour.R; var gC = _pillColour.G; var b = _pillColour.B;
            Color baseCol = gC > r && gC > b ? Color.FromArgb(34, 197, 94) : r > gC && r > b ? Color.FromArgb(239, 68, 68) : Color.FromArgb(234, 179, 8);
            Color endCol = Color.FromArgb(14, 26, 119);
            using var grad = new LinearGradientBrush(
                rect,
                Color.Empty,
                Color.Empty,
                15f,   // shallower angle
                true);
            var blend = new ColorBlend
            {
                Colors = new[]
                {
                    ControlPaint.Light(baseCol, 0.4f),   // 0% light green
                    baseCol,                             // 20% normal green
                    ControlPaint.Dark(baseCol, 0.2f),    // 50% deeper green
                    Color.FromArgb(14, 26, 119)          // 100% dark blue
                },
                Positions = new[]
                {
                    0.0f,
                    0.2f,
                    0.65f,
                    1.0f
                }
            };
            grad.InterpolationColors = blend;
            using var path = ShapeHelper.RoundedPath(rect, PillRadius);
            g.FillPath(grad, path);
            var glossRect = new Rectangle(0, py, pw, ph / 2);
            using var gloss = new LinearGradientBrush(glossRect, Color.FromArgb(70, Color.White), Color.FromArgb(0, Color.White), LinearGradientMode.Vertical);
            g.SetClip(path); g.FillRectangle(gloss, glossRect); g.ResetClip();
            using var fgB = new SolidBrush(Color.White);
            g.DrawString(Text, Font, fgB, new RectangleF(0, py, pw, ph), new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        }
    }


    internal sealed class PillButton : Button
    {
        private const int Radius = 13; private bool _hovered;
        private PillButtonStyle _style = PillButtonStyle.Normal;
        public PillButtonStyle Style { get => _style; set { _style = value; Invalidate(); } }
        public PillButton()
        {
            FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; BackColor = Color.Transparent; ForeColor = Color.White;
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold); Cursor = Cursors.Hand; TextAlign = ContentAlignment.MiddleCenter;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Opaque, true);
        }
        protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Parent?.BackColor ?? Color.FromArgb(39, 46, 63));
            using var path = ShapeHelper.RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
            Color topCol, botCol, fgCol;
            if (_style == PillButtonStyle.Active) { topCol = _hovered ? Color.FromArgb(196, 181, 253) : Color.FromArgb(167, 139, 250); botCol = _hovered ? Color.FromArgb(167, 139, 250) : Color.FromArgb(124, 58, 237); fgCol = Color.FromArgb(30, 10, 80); }
            else { topCol = Color.FromArgb(200, 200, 200); botCol = Color.FromArgb(170, 170, 170); fgCol = Color.FromArgb(80, 80, 80); }
            using var grad = new LinearGradientBrush(new Point(0, 0), new Point(Width, Height), topCol, botCol);
            g.FillPath(grad, path);
            if (Height > 4 && _style == PillButtonStyle.Normal)
            { var gr = new Rectangle(0, 0, Width, Height / 2); using var gloss = new LinearGradientBrush(gr, Color.FromArgb(80, Color.White), Color.FromArgb(0, Color.White), LinearGradientMode.Vertical); g.SetClip(path); g.FillRectangle(gloss, gr); g.ResetClip(); }
            using var fg = new SolidBrush(fgCol);
            g.DrawString(Text, Font, fg, new RectangleF(0, 0, Width, Height), new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        }
    }


    internal sealed class ModernButton : Button
    {
        private static readonly Color _normal = Color.FromArgb(45, 52, 68), _hover = Color.FromArgb(60, 68, 88), _accent = Color.FromArgb(103, 58, 182);
        private static readonly Color _muted = Color.FromArgb(108, 117, 125), _mutedH = Color.FromArgb(120, 128, 140);
        private static readonly Color _back = Color.FromArgb(223, 218, 242), _backH = Color.FromArgb(60, 68, 88);
        private const int Radius = 8; private bool _hovered;
        private ModernButtonStyle _style = ModernButtonStyle.Primary;
        public ModernButtonStyle Style { get => _style; set { _style = value; Invalidate(); } }
        private bool _isBack;
        public bool IsBack { get => _isBack; set { _isBack = value; ForeColor = value ? Color.FromArgb(30, 10, 60) : Color.White; Invalidate(); } }
        public ModernButton()
        {
            FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; BackColor = _normal; ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f); Cursor = Cursors.Hand; TextAlign = ContentAlignment.MiddleLeft; Padding = new Padding(14, 0, 0, 0);
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }
        protected override void OnMouseEnter(EventArgs e) { _hovered = true; BackColor = _isBack ? _backH : (_style == ModernButtonStyle.Muted ? _mutedH : _hover); Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; BackColor = _isBack ? _back : (_style == ModernButtonStyle.Muted ? _muted : _normal); Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(39, 46, 63));
            using var path = RR(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
            Color bgCol = _isBack ? (_hovered ? _backH : _back) : BackColor;
            using var brush = new SolidBrush(bgCol); g.FillPath(brush, path);
            if (_style == ModernButtonStyle.Primary)
                {
                    var accentCol = _isBack
                        ? Color.FromArgb(160, _accent)   // subtle version
                        : _accent;
                
                    using var ab = new SolidBrush(accentCol);
                
                    g.FillRectangle(ab, new Rectangle(
                        0,
                        Radius,
                        3,
                        Height - Radius * 2));
                };
            using var fg = new SolidBrush(ForeColor);
            var ta = _isBack ? StringAlignment.Center : StringAlignment.Near;
            g.DrawString(Text, Font, fg, new RectangleF(_isBack ? 0 : Padding.Left + 4, 0, Width - (_isBack ? 0 : Padding.Left + 8), Height),
                new StringFormat { Alignment = ta, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap });
        }
        private static GraphicsPath RR(Rectangle r, int rad) { int d = rad * 2; var p = new GraphicsPath(); p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p; }
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
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(_pageBg);
            for (int i = 3; i >= 1; i--) { using var sb = new SolidBrush(Color.FromArgb(18, 0, 0, 0)); using var sp = RRP(new Rectangle(i, i, Width - i * 2, Height - i * 2), Radius); g.FillPath(sb, sp); }
            using var wb = new SolidBrush(Color.White); using var wp = RRP(new Rectangle(0, 0, Width - 1, Height - 1), Radius); g.FillPath(wb, wp);
        }
        protected override void Dispose(bool disposing) { if (disposing) _resizeTimer.Dispose(); base.Dispose(disposing); }
        private static GraphicsPath RRP(Rectangle r, int rad) { int d = rad * 2; var p = new GraphicsPath(); p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p; }
    }


    internal class BorderPanel : Panel
    {
        public BorderPanel() { Padding = new Padding(4, 2, 4, 2); }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            e.Control.BackColor = UiFactory.Lavender;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var pen  = new Pen(UiFactory.Purple700, 1.5f);
            using var path = RoundedRect(rect, 6);
            using var fill = new SolidBrush(UiFactory.Lavender);
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
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


    internal class RoundedCardPanel : Panel
    {
        public RoundedCardPanel()
        {
            ResizeRedraw = true;
            BackColor      = Color.White;
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                                                using var sp     = RoundedPath(new Rectangle(2, 3, Width - 3, Height - 2), 12);
            using var shadow = new SolidBrush(Color.FromArgb(18, 0, 0, 0));
            g.FillPath(shadow, sp);
            using var fp   = RoundedPath(rect, 12);
            using var fill = new SolidBrush(Color.White);
            g.FillPath(fill, fp);
            using var pen = new Pen(Color.FromArgb(220, 220, 235), 1f);
            g.DrawPath(pen, fp);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            var rect = new Rectangle(0, 0, Width, Height);
            using var path = RoundedPath(rect, 12);
            Region = new Region(path);
        }

        private static GraphicsPath RoundedPath(Rectangle r, int radius)
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


    internal class RouteRow : Panel
    {
        private readonly ComboBox _svcCombo;
        private readonly TextBox  _portBox;
        private readonly TextBox  _prefixBox;
        private readonly TextBox  _domainBox;
        private readonly Panel    _svcWrap;
        private readonly Panel    _portWrap;
        private readonly Panel    _prefixWrap;
        private readonly Panel    _domainWrap;

        public event EventHandler? RemoveClicked;
        public RouteSpec Spec => BuildSpec();

        private static readonly string[] Services = { "TSPlus", "YourOrder", "MyPlace", "Other" };

        public RouteRow()
        {
            this.Size   = new Size(660, 38);
            this.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            Height    = 36;
            Dock      = DockStyle.Top;
            BackColor = Color.Transparent;

            _svcCombo  = new ComboBox();
            _portBox   = new TextBox { PlaceholderText = "Port" };
            _prefixBox = new TextBox { PlaceholderText = "Prefix" };
            _domainBox = new TextBox { PlaceholderText = "Domain" };

            foreach (var s in Services) _svcCombo.Items.Add(s);
            _svcCombo.SelectedIndex         = 0;
            _svcCombo.SelectedIndexChanged += OnServiceChanged;

            _svcWrap    = UiFactory.StyledCombo(_svcCombo,    0, 4, 130);
            _portWrap   = UiFactory.StyledTextBox(_portBox, 138, 4,  80);
            _prefixWrap = UiFactory.StyledTextBox(_prefixBox, 226, 4, 100);
            _domainWrap = UiFactory.StyledTextBox(_domainBox, 334, 4, 260);

            var removeBtn = new Label
            {
                Text      = "X",
                Location  = new Point(600, 8),
                Size      = new Size(22, 22),
                ForeColor = Color.FromArgb(239, 68, 68),
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            removeBtn.Click += (_, _) => RemoveClicked?.Invoke(this, EventArgs.Empty);
            Controls.AddRange(new Control[] { _svcWrap, _portWrap, _prefixWrap, _domainWrap, removeBtn });
            OnServiceChanged(null, EventArgs.Empty);
        }

        private void OnServiceChanged(object? sender, EventArgs e)
        {
            var svc = _svcCombo.SelectedItem?.ToString() ?? "";
            switch (svc)
            {
                case "TSPlus":
                    _portBox.Text = "54363"; _prefixBox.Text = ""; _domainBox.Text = "";
                    _prefixWrap.Visible = false; _domainWrap.Visible = false;
                    break;
                case "YourOrder":
                    _portBox.Text = "8080"; _prefixBox.Text = "yo"; _domainBox.Text = "bepozcloud.com";
                    _prefixWrap.Visible = true; _domainWrap.Visible = true;
                    break;
                case "MyPlace":
                    _portBox.Text = "3434"; _prefixBox.Text = ""; _domainBox.Text = "bepozcloud.com";
                    _prefixWrap.Visible = false; _domainWrap.Visible = true;
                    break;
                default:
                    _prefixWrap.Visible = true; _domainWrap.Visible = true;
                    break;
            }
        }

        private RouteSpec BuildSpec() => new RouteSpec
        {
            Service = _svcCombo.SelectedItem?.ToString() ?? "",
            Port    = int.TryParse(_portBox.Text, out var p) ? p : 0,
            Prefix  = _prefixBox.Text.Trim(),
            Domain  = _domainBox.Text.Trim()
        };
    }

}
