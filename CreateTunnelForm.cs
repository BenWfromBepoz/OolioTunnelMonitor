using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CloudflaredMonitor
{
    public sealed class CreateTunnelForm : Form
    {
        public NewTunnelSpec? Result { get; private set; }

        // Identity fields
        private readonly TextBox _txtNetSuiteId = new() { Width = 160 };
        private readonly TextBox _txtGroupName  = new() { Width = 220 };
        private readonly TextBox _txtVenueName  = new() { Width = 220 };
        private readonly Label   _lblTunnelPreview = new()
        {
            ForeColor = Color.FromArgb(103, 58, 182),
            Font      = new Font("Segoe UI", 9f, FontStyle.Italic),
            AutoSize  = true,
            MaximumSize = new Size(580, 0)
        };

        // YourOrder
        private readonly CheckBox        _chkYO  = new() { Text = "YourOrder", AutoSize = true };
        private readonly NumericUpDown   _nudYO  = new() { Minimum = 1, Maximum = 10, Value = 1, Width = 55 };
        private readonly FlowLayoutPanel _pnlYO  = new() { FlowDirection = FlowDirection.TopDown, AutoSize = true, Margin = new Padding(24, 0, 0, 4) };
        private readonly List<(TextBox path, TextBox port)> _yoRows = new();

        // Fixed services
        private readonly CheckBox _chkMP  = new() { Text = "MyPlace",   AutoSize = true };
        private readonly TextBox  _txtMPPort  = new() { Width = 80, Text = "8080" };
        private readonly CheckBox _chkEze = new() { Text = "EzeOffice", AutoSize = true };
        private readonly TextBox  _txtEzePort = new() { Width = 80, Text = "8081" };
        private readonly CheckBox _chkTSP = new() { Text = "TS Plus",   AutoSize = true };
        private readonly TextBox  _txtTSPPort = new() { Width = 80, Text = "3389" };

        // Other
        private readonly CheckBox _chkOther      = new() { Text = "Other", AutoSize = true };
        private readonly TextBox  _txtOtherPrefix = new() { Width = 100, PlaceholderText = "prefix" };
        private readonly TextBox  _txtOtherPath   = new() { Width = 100, PlaceholderText = "path (opt)" };
        private readonly TextBox  _txtOtherPort   = new() { Width = 80,  PlaceholderText = "port" };

        // Route preview
        private readonly Label _lblRoutePreview = new()
        {
            AutoSize    = true,
            MaximumSize = new Size(580, 0),
            Font        = new Font("Segoe UI", 8.5f),
            ForeColor   = Color.FromArgb(71, 85, 105)
        };

        // Buttons — OK is a purple PillButton-style, Cancel is rounded grey on the right
        private readonly RoundedButton _btnOk     = new(purple: true)  { Text = "Create Tunnel", DialogResult = DialogResult.OK,     Width = 140, Height = 34 };
        private readonly RoundedButton _btnCancel = new(purple: false) { Text = "Cancel",        DialogResult = DialogResult.Cancel, Width = 90,  Height = 34 };

        public CreateTunnelForm()
        {
            Text            = "Install New Tunnel";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = MinimizeBox = false;
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.FromArgb(241, 245, 249);
            Font            = new Font("Segoe UI", 9.5f);
            AutoScroll      = true;
            ClientSize      = new Size(640, 660);

            var flow = new FlowLayoutPanel
            {
                Dock           = DockStyle.Fill,
                FlowDirection  = FlowDirection.TopDown,
                WrapContents   = false,
                AutoScroll     = true,
                Padding        = new Padding(20, 16, 20, 16),
            };
            Controls.Add(flow);

            // --- Identity ---
            flow.Controls.Add(MakeHeader("Tunnel Identity"));
            flow.Controls.Add(MakeRow("NetSuite Company ID *", _txtNetSuiteId, "Found on the HubSpot Company Record"));
            flow.Controls.Add(MakeRow("Group Name",            _txtGroupName,  "Leave blank for sole-trader venues"));
            flow.Controls.Add(MakeRow("Venue Name *",          _txtVenueName));
            _lblTunnelPreview.Margin = new Padding(0, 0, 0, 8);
            flow.Controls.Add(_lblTunnelPreview);
            _txtNetSuiteId.TextChanged += (_, _) => UpdatePreview();
            _txtGroupName.TextChanged  += (_, _) => UpdatePreview();
            _txtVenueName.TextChanged  += (_, _) => UpdatePreview();

            // --- Services ---
            flow.Controls.Add(MakeHeader("Services"));

            var yoRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 4, 0, 0) };
            yoRow.Controls.Add(_chkYO);
            yoRow.Controls.Add(SmallLabel("Number of APIs:"));
            yoRow.Controls.Add(_nudYO);
            flow.Controls.Add(yoRow);
            _nudYO.Enabled  = false;
            _pnlYO.Visible  = false;
            flow.Controls.Add(_pnlYO);
            _chkYO.CheckedChanged += (_, _) => { _nudYO.Enabled = _chkYO.Checked; RebuildYORows((int)_nudYO.Value); _pnlYO.Visible = _chkYO.Checked; UpdatePreview(); };
            _nudYO.ValueChanged   += (_, _) => { if (_chkYO.Checked) RebuildYORows((int)_nudYO.Value); };

            flow.Controls.Add(MakeSvcRow(_chkMP,  "Port:", _txtMPPort));
            flow.Controls.Add(MakeSvcRow(_chkEze, "Port:", _txtEzePort));
            flow.Controls.Add(MakeSvcRow(_chkTSP, "Port:", _txtTSPPort));

            var otherRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 4, 0, 0) };
            otherRow.Controls.Add(_chkOther);
            otherRow.Controls.Add(SmallLabel("Prefix:")); otherRow.Controls.Add(_txtOtherPrefix);
            otherRow.Controls.Add(SmallLabel("Path:"));   otherRow.Controls.Add(_txtOtherPath);
            otherRow.Controls.Add(SmallLabel("Port:"));   otherRow.Controls.Add(_txtOtherPort);
            flow.Controls.Add(otherRow);

            _txtMPPort.Enabled = _txtEzePort.Enabled = _txtTSPPort.Enabled = false;
            _txtOtherPrefix.Enabled = _txtOtherPath.Enabled = _txtOtherPort.Enabled = false;
            _chkMP.CheckedChanged    += (_, _) => { _txtMPPort.Enabled    = _chkMP.Checked;    UpdatePreview(); };
            _chkEze.CheckedChanged   += (_, _) => { _txtEzePort.Enabled   = _chkEze.Checked;   UpdatePreview(); };
            _chkTSP.CheckedChanged   += (_, _) => { _txtTSPPort.Enabled   = _chkTSP.Checked;   UpdatePreview(); };
            _chkOther.CheckedChanged += (_, _) => { _txtOtherPrefix.Enabled = _txtOtherPath.Enabled = _txtOtherPort.Enabled = _chkOther.Checked; UpdatePreview(); };
            foreach (var t in new[] { _txtMPPort, _txtEzePort, _txtTSPPort, _txtOtherPrefix, _txtOtherPath, _txtOtherPort })
                t.TextChanged += (_, _) => UpdatePreview();

            // --- Review ---
            flow.Controls.Add(new Panel { Height = 8 });
            flow.Controls.Add(MakeHeader("Review"));
            _lblRoutePreview.Margin = new Padding(0, 0, 0, 8);
            flow.Controls.Add(_lblRoutePreview);

            // --- Buttons: OK left, Cancel pushed to right ---
            flow.Controls.Add(new Panel { Height = 4 });
            var btnRow = new FlowLayoutPanel
            {
                FlowDirection  = FlowDirection.LeftToRight,
                AutoSize       = true,
                Width          = 600,
            };
            // Spacer panel pushes Cancel to the right
            var spacer = new Panel { Width = 340, Height = 34 };
            btnRow.Controls.Add(_btnOk);
            btnRow.Controls.Add(spacer);
            btnRow.Controls.Add(_btnCancel);
            flow.Controls.Add(btnRow);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
            _btnOk.Click += BtnOk_Click;
            UpdatePreview();
        }

        // ── Dynamic YourOrder rows ────────────────────────────────────────────
        private void RebuildYORows(int count)
        {
            _pnlYO.Controls.Clear(); _yoRows.Clear();
            for (int i = 0; i < count; i++)
            {
                var row = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 2, 0, 2) };
                row.Controls.Add(new Label { Text = $"API {i + 1}:", AutoSize = true, TextAlign = System.Drawing.ContentAlignment.MiddleLeft });
                var path = new TextBox { Width = 140, PlaceholderText = "pathname (e.g. api)" };
                var port = new TextBox { Width = 80,  PlaceholderText = "port" };
                row.Controls.Add(SmallLabel("Path:")); row.Controls.Add(path);
                row.Controls.Add(SmallLabel("Port:")); row.Controls.Add(port);
                _pnlYO.Controls.Add(row); _yoRows.Add((path, port));
                path.TextChanged += (_, _) => UpdatePreview();
                port.TextChanged += (_, _) => UpdatePreview();
            }
        }

        // ── Preview ───────────────────────────────────────────────────────────
        private void UpdatePreview()
        {
            _lblTunnelPreview.Text = "Tunnel name: " + BuildTunnelName();
            var routes = BuildRoutes();
            _lblRoutePreview.Text = routes.Count == 0
                ? "No routes configured yet."
                : string.Join("\n", routes.Select(r => $"  {r.CloudUrl} \u2192 http://localhost:{r.Port}"));
        }

        private string BuildTunnelName()
        {
            string venue = _txtVenueName.Text.Trim();
            string group = _txtGroupName.Text.Trim();
            string ns    = _txtNetSuiteId.Text.Trim();
            if (string.IsNullOrEmpty(venue)) return "(enter venue name)";
            string name = string.IsNullOrEmpty(group) ? venue : $"{group} - {venue}";
            return string.IsNullOrEmpty(ns) ? name : $"{name} [{ns}]";
        }

        private static string Slugify(string s) =>
            Regex.Replace(s.ToLowerInvariant().Replace(' ', '-'), @"[^a-z0-9\-]", "");

        private List<RouteSpec> BuildRoutes()
        {
            var routes = new List<RouteSpec>();
            string slug = Slugify(_txtVenueName.Text.Trim());
            if (string.IsNullOrEmpty(slug)) return routes;
            if (_chkYO.Checked)
                foreach (var (path, port) in _yoRows)
                {
                    string p = path.Text.Trim().Trim('/');
                    if (!string.IsNullOrEmpty(port.Text.Trim()))
                        routes.Add(new RouteSpec($"yo-{slug}.bepozconnect.com", p, port.Text.Trim()));
                }
            if (_chkMP.Checked  && !string.IsNullOrWhiteSpace(_txtMPPort.Text))  routes.Add(new RouteSpec($"mp-{slug}.bepozconnect.com",  "", _txtMPPort.Text.Trim()));
            if (_chkEze.Checked && !string.IsNullOrWhiteSpace(_txtEzePort.Text)) routes.Add(new RouteSpec($"eze-{slug}.bepozconnect.com", "", _txtEzePort.Text.Trim()));
            if (_chkTSP.Checked && !string.IsNullOrWhiteSpace(_txtTSPPort.Text)) routes.Add(new RouteSpec($"tsp-{slug}.bepozconnect.com", "", _txtTSPPort.Text.Trim()));
            if (_chkOther.Checked && !string.IsNullOrWhiteSpace(_txtOtherPort.Text) && !string.IsNullOrWhiteSpace(_txtOtherPrefix.Text))
                routes.Add(new RouteSpec($"{Slugify(_txtOtherPrefix.Text)}-{slug}.bepozconnect.com", _txtOtherPath.Text.Trim().Trim('/'), _txtOtherPort.Text.Trim()));
            return routes;
        }

        // ── OK handler ────────────────────────────────────────────────────────
        private void BtnOk_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtNetSuiteId.Text))
            { MessageBox.Show("NetSuite Company ID is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); DialogResult = DialogResult.None; return; }
            if (string.IsNullOrWhiteSpace(_txtVenueName.Text))
            { MessageBox.Show("Venue Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); DialogResult = DialogResult.None; return; }
            var routes = BuildRoutes();
            if (routes.Count == 0)
            { MessageBox.Show("Please configure at least one service.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); DialogResult = DialogResult.None; return; }
            Result = new NewTunnelSpec
            {
                TunnelName = BuildTunnelName(),
                NetSuiteId = _txtNetSuiteId.Text.Trim(),
                GroupName  = _txtGroupName.Text.Trim(),
                VenueName  = _txtVenueName.Text.Trim(),
                Routes     = routes
            };
        }

        // ── UI helpers ────────────────────────────────────────────────────────
        private static Label MakeHeader(string text) => new()
        {
            Text      = text,
            Font      = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(39, 46, 63),
            AutoSize  = true,
            Margin    = new Padding(0, 8, 0, 4)
        };

        private static Label SmallLabel(string text) => new()
        {
            Text      = text,
            AutoSize  = true,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            Margin    = new Padding(4, 0, 2, 0)
        };

        private static FlowLayoutPanel MakeRow(string labelText, Control input, string? hint = null)
        {
            var row = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 3, 0, 3) };
            row.Controls.Add(new Label { Text = labelText, Width = 170, TextAlign = System.Drawing.ContentAlignment.MiddleLeft, AutoSize = false });
            row.Controls.Add(input);
            if (hint != null) row.Controls.Add(new Label { Text = hint, ForeColor = Color.FromArgb(100, 116, 139), Font = new Font("Segoe UI", 8f), AutoSize = true, TextAlign = System.Drawing.ContentAlignment.MiddleLeft, Margin = new Padding(6, 0, 0, 0) });
            return row;
        }

        private static FlowLayoutPanel MakeSvcRow(CheckBox chk, string portLabel, TextBox portBox)
        {
            var row = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 4, 0, 0) };
            row.Controls.Add(chk); row.Controls.Add(SmallLabel(portLabel)); row.Controls.Add(portBox);
            return row;
        }
    }

    // ── Rounded button used in CreateTunnelForm ───────────────────────────────
    // purple=true  → purple gradient (matches Test Token / Install Tunnel pill)
    // purple=false → rounded grey (Cancel)
    internal sealed class RoundedButton : Button
    {
        private const int Radius = 10;
        private readonly bool _purple;
        private bool _hovered;

        public RoundedButton(bool purple)
        {
            _purple   = purple;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            ForeColor = purple ? Color.White : Color.FromArgb(50, 60, 80);
            Font      = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            Cursor    = Cursors.Hand;
            TextAlign = ContentAlignment.MiddleCenter;
            Margin    = new Padding(0, 0, 0, 0);
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
        }

        protected override void OnMouseEnter(EventArgs e) { _hovered = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaintBackground(PaintEventArgs e) { e.Graphics.Clear(Color.FromArgb(241, 245, 249)); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundedPath(bounds, Radius);

            if (_purple)
            {
                var top = _hovered ? Color.FromArgb(160, 115, 240) : Color.FromArgb(140, 95, 220);
                var bot = _hovered ? Color.FromArgb(90,  50, 160)  : Color.FromArgb(75,  40, 140);
                using var grad = new LinearGradientBrush(new Point(0, 0), new Point(Width, Height), top, bot);
                g.FillPath(grad, path);
                // Gloss
                var gr = new Rectangle(0, 0, Width, Height / 2);
                using var gloss = new LinearGradientBrush(gr, Color.FromArgb(80, Color.White), Color.FromArgb(0, Color.White), LinearGradientMode.Vertical);
                g.SetClip(path); g.FillRectangle(gloss, gr); g.ResetClip();
            }
            else
            {
                var fill = _hovered ? Color.FromArgb(210, 215, 225) : Color.FromArgb(226, 232, 240);
                using var brush = new SolidBrush(fill);
                g.FillPath(brush, path);
                using var pen = new Pen(Color.FromArgb(200, 208, 220), 1f);
                g.DrawPath(pen, path);
            }

            using var fg = new SolidBrush(ForeColor);
            g.DrawString(Text, Font, fg, new RectangleF(0, 0, Width, Height),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        }

        private static GraphicsPath RoundedPath(Rectangle r, int rad)
        {
            int d = rad * 2; var p = new GraphicsPath();
            p.AddArc(r.X,         r.Y,          d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
            p.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
            p.CloseFigure(); return p;
        }
    }

    // ── Data models ───────────────────────────────────────────────────────────
    public sealed class NewTunnelSpec
    {
        public string TunnelName { get; init; } = "";
        public string NetSuiteId { get; init; } = "";
        public string GroupName  { get; init; } = "";
        public string VenueName  { get; init; } = "";
        public List<RouteSpec> Routes { get; init; } = new();
    }

    public sealed class RouteSpec
    {
        public string Hostname { get; }
        public string Path     { get; }
        public string Port     { get; }
        public string CloudUrl => string.IsNullOrEmpty(Path) ? Hostname : $"{Hostname}/{Path}";
        public string Service  => $"http://localhost:{Port}";
        public RouteSpec(string hostname, string path, string port)
        { Hostname = hostname; Path = path.Trim('/'); Port = port; }
    }
}
