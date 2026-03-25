using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace CloudflaredMonitor
{
    // ── Shared styling helpers ────────────────────────────────────────────────
    internal static class UiFactory
    {
        public static readonly Color Lavender   = Color.FromArgb(237, 233, 254);
        public static readonly Color Purple200  = Color.FromArgb(196, 181, 253);
        public static readonly Color Purple700  = Color.FromArgb(109,  40, 217);
        public static readonly Color SlateKey   = Color.FromArgb(100, 116, 139);
        public static readonly Color Slate900   = Color.FromArgb( 15,  23,  42);
        public static readonly Color PageBg     = Color.FromArgb(226, 232, 240);
        public static readonly Color White      = Color.White;

        // Rounded TextBox: purple-tinted, dark-purple border via owner-draw container
        public static Panel StyledTextBox(TextBox txt, int x, int y, int w, int h = 28)
        {
            txt.BorderStyle = BorderStyle.None;
            txt.BackColor   = Lavender;
            txt.ForeColor   = Purple700;
            txt.Font        = new Font("Cascadia Mono", 9f);
            txt.Location    = new Point(5, (h - txt.PreferredHeight) / 2 + 1);
            txt.Size        = new Size(w - 10, txt.PreferredHeight);
            txt.Anchor      = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            var wrap = new BorderPanel(Purple200, Purple700, 4)
            {
                Location  = new Point(x, y),
                Size      = new Size(w, h),
                BackColor = Lavender,
                Padding   = new Padding(5, 0, 5, 0)
            };
            wrap.Controls.Add(txt);
            return wrap;
        }

        public static Label MakeLabel(string text, bool key = true) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = key ? SlateKey : Slate900,
            AutoSize  = true,
            BackColor = Color.Transparent
        };

        public static ComboBox StyledCombo(int w)
        {
            var c = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 9f),
                BackColor     = Lavender,
                ForeColor     = Purple700,
                FlatStyle     = FlatStyle.Flat,
                Width         = w
            };
            return c;
        }
    }

    // Thin rounded border panel drawn with GDI+
    internal sealed class BorderPanel : Panel
    {
        private readonly Color _normalBorder;
        private readonly Color _focusBorder;
        private readonly int   _radius;
        private bool _focused;

        public BorderPanel(Color normalBorder, Color focusBorder, int radius)
        {
            _normalBorder = normalBorder; _focusBorder = focusBorder; _radius = radius;
            DoubleBuffered = true; ResizeRedraw = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            e.Control.GotFocus  += (_, _) => { _focused = true;  Invalidate(); };
            e.Control.LostFocus += (_, _) => { _focused = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? Color.White);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path  = ShapeHelper.RoundedPath(rect, _radius);
            using var fill  = new SolidBrush(BackColor);
            using var pen   = new Pen(_focused ? _focusBorder : _normalBorder, 1.5f);
            g.FillPath(fill, path);
            g.DrawPath(pen,  path);
        }
    }

    // ── Data models ───────────────────────────────────────────────────────────
    internal sealed class RouteSpec
    {
        public string Hostname { get; set; } = "";
        public string Path     { get; set; } = "";
        public string Service  { get; set; } = "";
    }

    internal sealed class InstallSpec
    {
        public string          TunnelName { get; set; } = "";
        public List<RouteSpec> Routes     { get; set; } = new();
    }

    // ── Per-route editor row ──────────────────────────────────────────────────
    internal sealed class RouteRow : Panel
    {
        public event EventHandler? Removed;
        public event EventHandler? Changed;

        private static readonly string[] Services = { "TSPlus", "YourOrder", "MyPlace", "Other" };

        private readonly ComboBox  _cboService  = UiFactory.StyledCombo(130);
        private readonly TextBox   _txtPort     = new();
        private readonly TextBox   _txtPrefix   = new();  // Other only
        private readonly ComboBox  _cboDomain   = UiFactory.StyledCombo(160); // Other only
        private readonly Panel     _pathsPanel  = new(); // YourOrder paths
        private readonly List<(TextBox name, TextBox path)> _paths = new();

        private static readonly Color PageBg = Color.FromArgb(226, 232, 240);

        public RouteRow()
        {
            BackColor    = PageBg;
            Height       = 36;
            Dock         = DockStyle.Top;
            AutoSize     = false;
            Padding      = new Padding(0, 4, 0, 4);

            // Service picker
            _cboService.Items.AddRange(Services);
            _cboService.Location      = new Point(0, 4);
            _cboService.SelectedIndex = 0;
            _cboService.SelectedIndexChanged += OnServiceChanged;

            // Port input
            var portWrap = UiFactory.StyledTextBox(_txtPort, 140, 4, 90, 28);
            _txtPort.PlaceholderText = "Port";
            _txtPort.TextChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);

            // Prefix (Other)
            var prefixWrap = UiFactory.StyledTextBox(_txtPrefix, 240, 4, 70, 28);
            _txtPrefix.PlaceholderText = "abc";
            _txtPrefix.MaxLength = 3;
            _txtPrefix.TextChanged += (_, _) => { _txtPrefix.Text = _txtPrefix.Text.ToLower(); Changed?.Invoke(this, EventArgs.Empty); };

            // Domain (Other)
            _cboDomain.Items.Add("bepozcloud.com");
            _cboDomain.Items.Add("bepozconnect.com");
            _cboDomain.SelectedIndex = 0;
            _cboDomain.Location      = new Point(320, 4);
            _cboDomain.SelectedIndexChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);

            // YourOrder paths panel — hidden by default, appears below row
            _pathsPanel.BackColor  = PageBg;
            _pathsPanel.Dock       = DockStyle.None;
            _pathsPanel.Visible    = false;
            _pathsPanel.Height     = 0;

            // Remove button
            var btnRemove = new Label
            {
                Text      = "\u2715",
                Font      = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(200, 80, 80),
                Cursor    = Cursors.Hand,
                AutoSize  = true,
                Location  = new Point(0, 8)  // positioned in Resize
            };
            btnRemove.Click += (_, _) => Removed?.Invoke(this, EventArgs.Empty);

            Controls.Add(_cboService);
            Controls.Add(portWrap);
            Controls.Add(prefixWrap);
            Controls.Add(_cboDomain);
            Controls.Add(_pathsPanel);
            Controls.Add(btnRemove);

            Resize += (_, _) => {
                btnRemove.Location = new Point(Width - 20, 8);
                _cboDomain.Location = new Point(Width - 190, 4);
            };

            RefreshLayout();
        }

        private void OnServiceChanged(object? s, EventArgs e)
        {
            RefreshLayout();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void RefreshLayout()
        {
            string svc = _cboService.SelectedItem?.ToString() ?? "";
            bool isOther    = svc == "Other";
            bool isYourOrder = svc == "YourOrder";

            // Show/hide Other fields
            Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.Contains(_txtPrefix))!.Visible = isOther;
            _cboDomain.Visible = isOther;

            // YourOrder paths
            if (isYourOrder && _paths.Count == 0) EnsureYourOrderPaths();
            _pathsPanel.Visible = isYourOrder;

            Height = isYourOrder ? 36 + _pathsPanel.Height + 4 : 36;
        }

        private void EnsureYourOrderPaths()
        {
            _pathsPanel.Controls.Clear(); _paths.Clear();
            int y = 2;
            for (int i = 0; i < 5; i++)
            {
                var lblN = UiFactory.MakeLabel("Path " + (i + 1) + " name:");
                var txtN = new TextBox(); var txtP = new TextBox();
                var wN = UiFactory.StyledTextBox(txtN, 0,  y, 120, 24);
                var wP = UiFactory.StyledTextBox(txtP, 128, y, 180, 24);
                lblN.Location = new Point(0, y - 16);
                txtN.PlaceholderText = "e.g. demo";
                txtP.PlaceholderText = "/path";
                txtN.TextChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
                txtP.TextChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
                _pathsPanel.Controls.Add(wN); _pathsPanel.Controls.Add(wP);
                _paths.Add((txtN, txtP));
                y += 28;
            }
            _pathsPanel.Height = y + 4;
        }

        public string GetServiceKey() => _cboService.SelectedItem?.ToString() ?? "";

        // Returns (hostname, path, localService) tuples for this row
        public List<(string host, string path, string svc)> BuildRoutes(string groupPart)
        {
            string port   = _txtPort.Text.Trim();
            if (string.IsNullOrEmpty(port)) return new();
            string local  = "http://localhost:" + port;
            string svcKey = GetServiceKey();
            var result    = new List<(string, string, string)>();

            switch (svcKey)
            {
                case "TSPlus":
                    result.Add(("bo-" + groupPart + ".bepozcloud.com", "", local));
                    break;
                case "MyPlace":
                    result.Add(("bo-" + groupPart + ".bepozconnect.com", "", local));
                    break;
                case "YourOrder":
                    // Base route
                    result.Add(("bo-" + groupPart + ".bepozconnect.com", "", local));
                    // Named paths
                    foreach (var (n, p) in _paths)
                    {
                        string pname = n.Text.Trim(); string ppath = p.Text.Trim();
                        if (!string.IsNullOrEmpty(pname) && !string.IsNullOrEmpty(ppath))
                            result.Add(("bo-" + groupPart + ".bepozconnect.com", ppath.StartsWith("/") ? ppath : "/" + ppath, local));
                    }
                    break;
                case "Other":
                    string pfx = _txtPrefix.Text.Trim(); string dom = _cboDomain.SelectedItem?.ToString() ?? "bepozcloud.com";
                    if (!string.IsNullOrEmpty(pfx))
                        result.Add((pfx + "-" + groupPart + "." + dom, "", local));
                    break;
            }
            return result;
        }

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(_txtPort.Text.Trim())) return false;
            if (GetServiceKey() == "Other" && string.IsNullOrEmpty(_txtPrefix.Text.Trim())) return false;
            return true;
        }
    }

    // ── Inline install panel ──────────────────────────────────────────────────
    internal sealed class InstallPanel : Panel
    {
        public event Action<InstallSpec>? InstallRequested;
        public event Action?              Cancelled;

        private static readonly Color PageBg   = Color.FromArgb(226, 232, 240);
        private static readonly Color Purple700 = Color.FromArgb(109, 40, 217);
        private static readonly Color Lavender  = Color.FromArgb(237, 233, 254);

        // Card 1 — Tunnel Identity
        private readonly RoundedPanel _cardIdentity = new();
        private readonly TextBox  _txtNsId      = new();
        private readonly TextBox  _txtGroup     = new();
        private readonly TextBox  _txtVenue     = new();
        private readonly CheckBox _chkCustom    = new();
        private readonly TextBox  _txtCustom    = new();
        private readonly Label    _lblPreview   = new();

        // Card 2 — Routes
        private readonly RoundedPanel _cardRoutes = new();
        private readonly Panel        _routesList = new();
        private readonly PillButton   _btnAddRoute = new();
        private readonly List<RouteRow> _rows = new();

        // Card 3 — Review
        private readonly RoundedPanel _cardReview  = new();
        private readonly Label        _lblReview   = new();
        private readonly PillButton   _btnInstall  = new();
        private readonly PillButton   _btnCancel   = new();

        private string _apiToken = "";

        public InstallPanel(string apiToken) { _apiToken = apiToken; BackColor = PageBg; DoubleBuffered = true; Build(); }

        public void Reset(string apiToken) { _apiToken = apiToken; _txtNsId.Text = ""; _txtGroup.Text = ""; _txtVenue.Text = ""; _txtCustom.Text = ""; _chkCustom.Checked = false; _rows.Clear(); _routesList.Controls.Clear(); UpdatePreview(); UpdateReview(); }

        private void Build()
        {
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = PageBg };
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, BackColor = Color.Transparent,
                ColumnCount = 1, RowCount = 4, Padding = new Padding(10)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            scroll.Controls.Add(tbl);
            Controls.Add(scroll);

            // ── Card 1: Tunnel Identity ───────────────────────────────────────
            _cardIdentity.Dock   = DockStyle.Fill;
            _cardIdentity.Margin = new Padding(0, 0, 0, 8);
            _cardIdentity.AutoSize = true;
            _cardIdentity.Padding  = new Padding(16, 8, 16, 12);
            AddCardTitle(_cardIdentity, "1  —  Tunnel Identity");

            int cy = 30;

            // NetSuite ID
            var lblNs = UiFactory.MakeLabel("NetSuite ID");
            lblNs.Location = new Point(16, cy); _cardIdentity.Controls.Add(lblNs); cy += 18;
            var wNs = UiFactory.StyledTextBox(_txtNsId, 16, cy, 160, 28);
            _txtNsId.PlaceholderText = "e.g. 12345";
            _txtNsId.TextChanged += (_, _) => UpdatePreview();
            _cardIdentity.Controls.Add(wNs); cy += 34;

            // Group Name
            var lblGrp = UiFactory.MakeLabel("Group Name (blank for standalone venue)");
            lblGrp.Location = new Point(16, cy); _cardIdentity.Controls.Add(lblGrp); cy += 18;
            var wGrp = UiFactory.StyledTextBox(_txtGroup, 16, cy, 300, 28);
            _txtGroup.PlaceholderText = "e.g. Muzz Buzz";
            _txtGroup.TextChanged += (_, _) => UpdatePreview();
            _cardIdentity.Controls.Add(wGrp); cy += 34;

            // Venue Name
            var lblVen = UiFactory.MakeLabel("Venue Name");
            lblVen.Location = new Point(16, cy); _cardIdentity.Controls.Add(lblVen); cy += 18;
            var wVen = UiFactory.StyledTextBox(_txtVenue, 16, cy, 300, 28);
            _txtVenue.PlaceholderText = "e.g. Perth CBD";
            _txtVenue.TextChanged += (_, _) => UpdatePreview();
            _cardIdentity.Controls.Add(wVen); cy += 34;

            // Custom toggle
            _chkCustom.Text      = "Custom name";
            _chkCustom.Font      = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
            _chkCustom.ForeColor = UiFactory.SlateKey;
            _chkCustom.BackColor = Color.Transparent;
            _chkCustom.Location  = new Point(16, cy);
            _chkCustom.AutoSize  = true;
            _chkCustom.CheckedChanged += (_, _) => { _txtCustom.Visible = _chkCustom.Checked; UpdatePreview(); };
            _cardIdentity.Controls.Add(_chkCustom); cy += 24;

            var wCust = UiFactory.StyledTextBox(_txtCustom, 16, cy, 400, 28);
            _txtCustom.PlaceholderText = "Enter full custom tunnel name";
            _txtCustom.TextChanged += (_, _) => UpdatePreview();
            _txtCustom.Visible = false;
            _cardIdentity.Controls.Add(wCust); cy += 34;

            // Preview label
            _lblPreview.Location  = new Point(16, cy);
            _lblPreview.Size      = new Size(500, 20);
            _lblPreview.Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic);
            _lblPreview.ForeColor = Purple700;
            _lblPreview.BackColor = Color.Transparent;
            _lblPreview.AutoSize  = false;
            _cardIdentity.Controls.Add(_lblPreview);

            tbl.Controls.Add(_cardIdentity, 0, 0);

            // ── Card 2: Routes ────────────────────────────────────────────────
            _cardRoutes.Dock    = DockStyle.Fill;
            _cardRoutes.Margin  = new Padding(0, 0, 0, 8);
            _cardRoutes.AutoSize = true;
            _cardRoutes.Padding  = new Padding(16, 8, 16, 48);
            AddCardTitle(_cardRoutes, "2  —  Published Routes");

            // Column headers
            var hdrSvc  = UiFactory.MakeLabel("Service");    hdrSvc.Location  = new Point(16, 30); _cardRoutes.Controls.Add(hdrSvc);
            var hdrPort = UiFactory.MakeLabel("Port");        hdrPort.Location = new Point(156, 30); _cardRoutes.Controls.Add(hdrPort);
            var hdrPfx  = UiFactory.MakeLabel("Prefix");     hdrPfx.Location  = new Point(256, 30); _cardRoutes.Controls.Add(hdrPfx);
            var hdrDom  = UiFactory.MakeLabel("Domain");     hdrDom.Location  = new Point(336, 30); _cardRoutes.Controls.Add(hdrDom);

            _routesList.BackColor  = PageBg;
            _routesList.Location   = new Point(16, 50);
            _routesList.AutoSize   = true;
            _routesList.Width      = 600;
            _routesList.Anchor     = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            _cardRoutes.Controls.Add(_routesList);

            _btnAddRoute.Text   = "+  Add Route";
            _btnAddRoute.Size   = new Size(110, 28);
            _btnAddRoute.Click += (_, _) => AddRoute();
            _btnAddRoute.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _cardRoutes.Controls.Add(_btnAddRoute);
            _cardRoutes.Resize += (_, _) => {
                _btnAddRoute.Location = new Point(16, _cardRoutes.Height - 38);
                _routesList.Width = _cardRoutes.Width - 32;
            };
            tbl.Controls.Add(_cardRoutes, 0, 1);

            // ── Card 3: Review ────────────────────────────────────────────────
            _cardReview.Dock    = DockStyle.Fill;
            _cardReview.Margin  = new Padding(0, 0, 0, 0);
            _cardReview.Padding = new Padding(16, 8, 16, 16);
            _cardReview.AutoSize = true;
            AddCardTitle(_cardReview, "3  —  Review & Install");

            _lblReview.Location  = new Point(16, 30);
            _lblReview.Size      = new Size(620, 60);
            _lblReview.Font      = new Font("Segoe UI", 8.5f);
            _lblReview.ForeColor = UiFactory.SlateKey;
            _lblReview.BackColor = Color.Transparent;
            _lblReview.AutoSize  = false;

            _btnInstall.Text     = "\u2193  Install Tunnel";
            _btnInstall.Size     = new Size(140, 34);
            _btnInstall.Location = new Point(16, 96);
            _btnInstall.Click   += OnInstallClick;

            _btnCancel.Text     = "Cancel";
            _btnCancel.Size     = new Size(90, 34);
            _btnCancel.Location = new Point(164, 96);
            _btnCancel.Click   += (_, _) => Cancelled?.Invoke();

            _cardReview.Controls.Add(_lblReview);
            _cardReview.Controls.Add(_btnInstall);
            _cardReview.Controls.Add(_btnCancel);
            tbl.Controls.Add(_cardReview, 0, 2);

            UpdatePreview();
            UpdateReview();
        }

        private void AddRoute()
        {
            var row = new RouteRow();
            row.Changed += (_, _) => UpdateReview();
            row.Removed += (_, _) => { _rows.Remove(row); _routesList.Controls.Remove(row); UpdateReview(); };
            _rows.Add(row);
            _routesList.Controls.Add(row);
            UpdateReview();
        }

        private string GetTunnelName()
        {
            if (_chkCustom.Checked) return _txtCustom.Text.Trim();
            string ns    = _txtNsId.Text.Trim();
            string group = _txtGroup.Text.Trim();
            string venue = _txtVenue.Text.Trim();
            if (string.IsNullOrEmpty(venue) && string.IsNullOrEmpty(ns)) return "";
            string name = string.IsNullOrEmpty(group) ? venue : group + " - " + venue;
            if (!string.IsNullOrEmpty(ns)) name += " [" + ns + "]";
            return name;
        }

        // The URL-slug part used when building hostnames (lowercase, no spaces)
        private string GetGroupSlug()
        {
            string ns    = _txtNsId.Text.Trim();
            string venue = _txtVenue.Text.Trim().ToLower().Replace(" ", "-");
            return string.IsNullOrEmpty(venue) ? ns : (string.IsNullOrEmpty(ns) ? venue : venue + "-" + ns);
        }

        private void UpdatePreview()
        {
            string name = GetTunnelName();
            _lblPreview.Text = string.IsNullOrEmpty(name) ? "Complete fields above to see preview" : "Preview: " + name;
        }

        private void UpdateReview()
        {
            UpdatePreview();
            string name  = GetTunnelName();
            string slug  = GetGroupSlug();
            bool nameOk  = !string.IsNullOrEmpty(name);
            bool routesOk = _rows.Count > 0 && _rows.All(r => r.IsValid());
            bool ready   = nameOk && routesOk;

            if (!nameOk) { _lblReview.Text = "Complete the tunnel identity above."; }
            else if (_rows.Count == 0) { _lblReview.Text = "Add at least one route."; }
            else if (!routesOk) { _lblReview.Text = "Complete all route fields (port required for each)."; }
            else
            {
                var lines = new List<string>();
                foreach (var row in _rows)
                    foreach (var (host, path, svc) in row.BuildRoutes(slug))
                        lines.Add("https://" + host + path + "  \u2192  " + svc);
                _lblReview.Text = string.Join("\n", lines.Take(4)) + (lines.Count > 4 ? "\n..." : "");
                _lblReview.Size = new Size(620, Math.Max(60, lines.Count * 18 + 8));
                _btnInstall.Location = new Point(16, _lblReview.Bottom + 8);
                _btnCancel.Location  = new Point(164, _lblReview.Bottom + 8);
            }
            _btnInstall.Enabled = ready;
        }

        private void OnInstallClick(object? sender, EventArgs e)
        {
            string slug = GetGroupSlug();
            var routes  = new List<RouteSpec>();
            foreach (var row in _rows)
                foreach (var (host, path, svc) in row.BuildRoutes(slug))
                    routes.Add(new RouteSpec { Hostname = host, Path = path, Service = svc });
            // Catch-all rule
            routes.Add(new RouteSpec { Hostname = "", Path = "", Service = "http_status:404" });
            InstallRequested?.Invoke(new InstallSpec { TunnelName = GetTunnelName(), Routes = routes });
        }

        private static void AddCardTitle(RoundedPanel card, string title)
        {
            card.Controls.Add(new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location  = new Point(16, 6),
                Size      = new Size(500, 20),
                BackColor = Color.Transparent
            });
        }
    }
}
