using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace CloudflaredMonitor
{
    // ── Data models ───────────────────────────────────────────────────────────
    internal sealed class RouteSpec
    {
        public string Hostname { get; set; } = "";
        public string Path     { get; set; } = "";
        public string Service  { get; set; } = "";
    }

    internal sealed class InstallSpec
    {
        public string            TunnelName { get; set; } = "";
        public List<RouteSpec>   Routes     { get; set; } = new();
    }

    // ── Inline install panel (3 cards: Identity, Routes, Review) ─────────────
    internal sealed class InstallPanel : Panel
    {
        public event Action<InstallSpec>? InstallRequested;
        public event Action?              Cancelled;

        private static readonly Color _pageBg = Color.FromArgb(226, 232, 240);
        private static readonly Color _accent = Color.FromArgb(103, 58, 182);

        // Card 1 – Tunnel Identity
        private readonly RoundedPanel _cardIdentity  = new();
        private readonly TextBox      _txtName        = new();

        // Card 2 – Routes
        private readonly RoundedPanel       _cardRoutes = new();
        private readonly DataGridView       _dgvRoutes  = new();
        private readonly PillButton         _btnAddRoute = new();

        // Card 3 – Review & Install
        private readonly RoundedPanel _cardReview   = new();
        private readonly Label        _lblReview    = new();
        private readonly PillButton   _btnInstall   = new();
        private readonly PillButton   _btnCancel    = new();

        private string _apiToken = "";

        public InstallPanel(string apiToken)
        {
            _apiToken = apiToken;
            BackColor = _pageBg;
            DoubleBuffered = true;
            Build();
        }

        public void Reset(string apiToken)
        {
            _apiToken    = apiToken;
            _txtName.Text = "";
            _dgvRoutes.Rows.Clear();
            UpdateReview();
        }

        private void Build()
        {
            // ── Layout: TableLayoutPanel with 3 row sections ─────────────────
            var tbl = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                BackColor   = Color.Transparent,
                ColumnCount = 1,
                RowCount    = 4,
                Padding     = new Padding(10)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute,  90));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent,   55));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute,   0)); // spacer
            Controls.Add(tbl);

            // ── Card 1: Tunnel Identity ───────────────────────────────────────
            _cardIdentity.Dock   = DockStyle.Fill;
            _cardIdentity.Margin = new Padding(0, 0, 0, 8);
            AddCardTitle(_cardIdentity, "1  —  Tunnel Identity");

            var lblName = MakeLabel("Tunnel Name", new Point(16, 30));
            _txtName.Location    = new Point(16, 50);
            _txtName.Size        = new Size(400, 26);
            _txtName.Font        = new Font("Segoe UI", 9.5f);
            _txtName.BorderStyle = BorderStyle.FixedSingle;
            _txtName.BackColor   = Color.FromArgb(248, 250, 252);
            _txtName.PlaceholderText = "e.g. Bepoz - Acme Corp [Site]";
            _txtName.TextChanged += (_, _) => UpdateReview();

            _cardIdentity.Controls.Add(lblName);
            _cardIdentity.Controls.Add(_txtName);
            tbl.Controls.Add(_cardIdentity, 0, 0);

            // ── Card 2: Routes ────────────────────────────────────────────────
            _cardRoutes.Dock    = DockStyle.Fill;
            _cardRoutes.Margin  = new Padding(0, 0, 0, 8);
            _cardRoutes.Padding = new Padding(10, 32, 10, 44);
            AddCardTitle(_cardRoutes, "2  —  Published Routes");

            // Routes grid
            var colHost = new DataGridViewTextBoxColumn { HeaderText = "Hostname",      Name = "host",    Width = 320, MinimumWidth = 160 };
            var colPath = new DataGridViewTextBoxColumn { HeaderText = "Path (opt.)",   Name = "path",    Width = 120, MinimumWidth = 60  };
            var colSvc  = new DataGridViewTextBoxColumn { HeaderText = "Local Service", Name = "service", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
            _dgvRoutes.Dock                  = DockStyle.Fill;
            _dgvRoutes.Font                  = new Font("Segoe UI", 8.5f);
            _dgvRoutes.BorderStyle           = BorderStyle.None;
            _dgvRoutes.GridColor             = Color.FromArgb(226, 232, 240);
            _dgvRoutes.CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal;
            _dgvRoutes.RowHeadersVisible     = false;
            _dgvRoutes.AllowUserToAddRows    = false;
            _dgvRoutes.AllowUserToDeleteRows = false;
            _dgvRoutes.SelectionMode         = DataGridViewSelectionMode.FullRowSelect;
            _dgvRoutes.BackgroundColor       = Color.White;
            _dgvRoutes.RowTemplate.Height    = 26;
            _dgvRoutes.EnableHeadersVisualStyles = false;
            _dgvRoutes.ColumnHeadersDefaultCellStyle.BackColor          = Color.FromArgb(237, 233, 254);
            _dgvRoutes.ColumnHeadersDefaultCellStyle.ForeColor          = Color.FromArgb(76, 29, 149);
            _dgvRoutes.ColumnHeadersDefaultCellStyle.Font               = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            _dgvRoutes.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(237, 233, 254);
            _dgvRoutes.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(76, 29, 149);
            _dgvRoutes.ColumnHeadersHeight         = 26;
            _dgvRoutes.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _dgvRoutes.DefaultCellStyle.BackColor          = Color.White;
            _dgvRoutes.DefaultCellStyle.SelectionBackColor = Color.FromArgb(237, 233, 254);
            _dgvRoutes.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 41, 59);
            _dgvRoutes.Columns.Add(colHost); _dgvRoutes.Columns.Add(colPath); _dgvRoutes.Columns.Add(colSvc);
            _dgvRoutes.CellEndEdit += (_, _) => UpdateReview();

            // Add / Delete row buttons
            _btnAddRoute.Text     = "+  Add Route";
            _btnAddRoute.Size     = new Size(110, 28);
            _btnAddRoute.Anchor   = AnchorStyles.Bottom | AnchorStyles.Left;
            _btnAddRoute.Location = new Point(10, 0); // positioned in Resize
            _btnAddRoute.Click   += (_, _) => { _dgvRoutes.Rows.Add("", "", ""); UpdateReview(); };

            var btnDelRoute = new PillButton { Text = "−  Remove", Size = new Size(110, 28), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            btnDelRoute.Click += (_, _) =>
            {
                if (_dgvRoutes.SelectedRows.Count > 0) { _dgvRoutes.Rows.Remove(_dgvRoutes.SelectedRows[0]); UpdateReview(); }
            };

            _cardRoutes.Controls.Add(_dgvRoutes);
            _cardRoutes.Controls.Add(_btnAddRoute);
            _cardRoutes.Controls.Add(btnDelRoute);
            _cardRoutes.Resize += (_, _) =>
            {
                _btnAddRoute.Location  = new Point(10,  _cardRoutes.Height - 38);
                btnDelRoute.Location   = new Point(128, _cardRoutes.Height - 38);
            };
            tbl.Controls.Add(_cardRoutes, 0, 1);

            // ── Card 3: Review & Install ──────────────────────────────────────
            _cardReview.Dock   = DockStyle.Fill;
            _cardReview.Margin = new Padding(0, 0, 0, 0);
            AddCardTitle(_cardReview, "3  —  Review & Install");

            _lblReview.Location  = new Point(16, 30);
            _lblReview.Size      = new Size(580, 36);
            _lblReview.Font      = new Font("Segoe UI", 8.5f);
            _lblReview.ForeColor = Color.FromArgb(71, 85, 105);
            _lblReview.BackColor = Color.Transparent;
            _lblReview.AutoSize  = false;

            _btnInstall.Text     = "\u2193  Install Tunnel";
            _btnInstall.Size     = new Size(140, 34);
            _btnInstall.Location = new Point(16, 58);
            _btnInstall.Click   += OnInstallClick;

            _btnCancel.Text     = "Cancel";
            _btnCancel.Size     = new Size(90, 34);
            _btnCancel.Location = new Point(164, 58);
            _btnCancel.Click   += (_, _) => Cancelled?.Invoke();

            _cardReview.Controls.Add(_lblReview);
            _cardReview.Controls.Add(_btnInstall);
            _cardReview.Controls.Add(_btnCancel);
            tbl.Controls.Add(_cardReview, 0, 2);

            UpdateReview();
        }

        private static Label MakeLabel(string text, Point loc) => new()
        {
            Text      = text,
            Font      = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location  = loc,
            AutoSize  = true,
            BackColor = Color.Transparent
        };

        private static void AddCardTitle(RoundedPanel card, string title)
        {
            card.Controls.Add(new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location  = new Point(16, 6),
                Size      = new Size(400, 20),
                BackColor = Color.Transparent
            });
        }

        private void UpdateReview()
        {
            string name   = _txtName.Text.Trim();
            int    routes = _dgvRoutes.Rows.Cast<DataGridViewRow>().Count(r => !string.IsNullOrWhiteSpace(r.Cells["host"].Value?.ToString()));
            bool   ready  = name.Length > 0 && routes > 0;
            _lblReview.Text  = ready
                ? $"Ready to install: \u201c{name}\u201d with {routes} route{(routes == 1 ? "" : "s")}."
                : "Complete the tunnel name and add at least one route to proceed.";
            _btnInstall.Enabled = ready;
        }

        private void OnInstallClick(object? sender, EventArgs e)
        {
            var routes = new List<RouteSpec>();
            foreach (DataGridViewRow row in _dgvRoutes.Rows)
            {
                string host = row.Cells["host"].Value?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(host)) continue;
                routes.Add(new RouteSpec
                {
                    Hostname = host,
                    Path     = row.Cells["path"].Value?.ToString()?.Trim() ?? "",
                    Service  = row.Cells["service"].Value?.ToString()?.Trim() ?? ""
                });
            }
            // Add catch-all
            routes.Add(new RouteSpec { Hostname = "", Path = "", Service = "http_status:404" });

            InstallRequested?.Invoke(new InstallSpec
            {
                TunnelName = _txtName.Text.Trim(),
                Routes     = routes
            });
        }
    }
}
