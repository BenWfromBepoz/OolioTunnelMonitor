using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace OolioTunnelMonitor
{
    // -- Shared styling helpers

    // FIX: public so CreateTunnelForm.Result property is accessible from MainForm
    public class RouteSpec
    {
        public string Service { get; set; } = "TSPlus";
        public int    Port    { get; set; }
        public string Prefix  { get; set; } = "";
        public string Domain  { get; set; } = "";
    }

    public class InstallSpec
    {
        public string NetSuiteId { get; set; } = "";
        public string GroupName  { get; set; } = "";
        public string VenueName  { get; set; } = "";
        public string CustomName { get; set; } = "";
        public bool   UseCustom  { get; set; }
        public List<RouteSpec> Routes { get; set; } = new();
        public string TunnelName => UseCustom && !string.IsNullOrWhiteSpace(CustomName) ? CustomName : string.Join("-", new[]{GroupName, VenueName, NetSuiteId}.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.ToLower().Replace(" ", "-")));
    }

    public class CreateTunnelForm : Form
    {
        private readonly TextBox _netSuiteBox  = new() { PlaceholderText = "e.g. 12345" };
        private readonly TextBox _groupBox     = new() { PlaceholderText = "blank for standalone venue" };
        private readonly TextBox _venueBox     = new() { PlaceholderText = "e.g. Moon Bar" };
        private readonly TextBox _customBox    = new() { PlaceholderText = "Custom tunnel name" };
        private readonly Label   _previewLabel = new();


        private readonly Panel          _routesPanel = new() { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        private readonly List<RouteRow> _rows        = new();
        private ToggleSwitch _tglCustom = null!;
        private readonly Label          _reviewLabel = new();

        // Buttons
        private readonly PillButton _installBtn = new() { Text = "Create Tunnel", DialogResult = DialogResult.OK, Width = 140, Height = 34, Style = PillButtonStyle.Normal };
        private readonly PillButton _cancelBtn  = new() { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90, Height = 34, Style = PillButtonStyle.Active };

        private readonly TableLayoutPanel _scrollContainer = new TableLayoutPanel();

        public InstallSpec? Result { get; private set; }

        public CreateTunnelForm()
        {
            Text            = "Install Tunnel";
            Size            = new Size(820, 700);
            MinimumSize     = new Size(700, 550);
            BackColor       = UiFactory.PageBg;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition   = FormStartPosition.CenterParent;

            _scrollContainer.Dock        = DockStyle.Fill;
            _scrollContainer.Padding     = new Padding(10);
            _scrollContainer.ColumnCount = 1;
            _scrollContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _scrollContainer.RowCount    = 4;
            _scrollContainer.RowStyles.Add(new RowStyle(SizeType.Percent,  25f));
            _scrollContainer.RowStyles.Add(new RowStyle(SizeType.Percent,  44f));
            _scrollContainer.RowStyles.Add(new RowStyle(SizeType.Percent,  21f));
            _scrollContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));

            Controls.Add(_scrollContainer);

            _netSuiteBox.TextChanged += (_, _) => RefreshPreview();
            _groupBox.TextChanged    += (_, _) => RefreshPreview();
            _venueBox.TextChanged    += (_, _) => RefreshPreview();
            _customBox.TextChanged   += (_, _) => RefreshPreview();
            RefreshPreview();
            _installBtn.Text = "Install Tunnel";
            _installBtn.BackColor = Color.FromArgb(109, 40, 217);

            _cancelBtn.Text = "Cancel";
            _cancelBtn.BackColor = Color.FromArgb(108, 117, 125);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            BuildUI();
        }

        private void BuildUI()
        {
            var card1 = MakeCard("1 - Tunnel Identity");
            int cardInner = _scrollContainer.Width - 96; // available width inside card
            int lw = 200;        // left column width
            int rx = 240;        // right column x
            int rw = cardInner - rx; // right column width

            // Left column: NetSuite ID
            card1.Controls.Add(UiFactory.MakeLabel("NetSuite ID", 20, 44));
            card1.Controls.Add(UiFactory.StyledTextBox(_netSuiteBox, 20, 64, lw));

            // Left column: Custom name toggle
            _tglCustom = new ToggleSwitch { Location = new Point(20, 110), Size = new Size(44, 22) };
            var lblCustom = new Label {
                Text = "Custom name", Location = new Point(70, 113),
                AutoSize = true, Font = new Font("Segoe UI", 9f),
                ForeColor = UiFactory.SlateKey, BackColor = Color.Transparent
            };
            _tglCustom.CheckedChanged += (_, __) => { RefreshPreview(); };
            card1.Controls.Add(_tglCustom);
            card1.Controls.Add(lblCustom);

            // Right column: Group Name
            card1.Controls.Add(UiFactory.MakeLabel("Group Name (blank for standalone venue)", rx, 44, rw));
            card1.Controls.Add(UiFactory.StyledTextBox(_groupBox, rx, 64, rw));

            // Right column: Venue Name
            card1.Controls.Add(UiFactory.MakeLabel("Venue Name", rx, 108, rw));
            card1.Controls.Add(UiFactory.StyledTextBox(_venueBox, rx, 128, rw));

            // Preview label - full width at bottom
            _previewLabel.Location  = new Point(20, 178);
            _previewLabel.Size      = new Size(cardInner - 20, 18);
            _previewLabel.Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic);
            _previewLabel.ForeColor = UiFactory.Purple700;
            _previewLabel.BackColor = Color.Transparent;
            card1.Controls.Add(_previewLabel);

            _scrollContainer.Controls.Add(card1, 0, 0);
            var card2 = MakeCard("2 - Published Routes");

            int hx = 20;
            foreach (var (col, w) in new[] { ("Service", 130), ("Port", 80), ("Prefix", 100), ("Domain", 260) })
            {
                card2.Controls.Add(new Label
                {
                    Text      = col,
                    Location  = new Point(hx, 42),
                    Size      = new Size(w, 16),
                    Font      = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
                    ForeColor = UiFactory.SlateKey,
                    BackColor = Color.Transparent
                });
                hx += w + 8;
            }

            _routesPanel.Location  = new Point(14, 62);
            _routesPanel.Width     = card2.Width - 28;
            _routesPanel.BackColor = Color.Transparent;
            card2.Controls.Add(_routesPanel);

            var addBtn = new Label
            {
                Text      = "+ Add Route",
                AutoSize  = true,
                Location  = new Point(20, 0),
                Font      = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                ForeColor = UiFactory.Purple700,
                BackColor = Color.Transparent,
                Cursor    = Cursors.Hand
            };
            addBtn.Click += (_, _) => AddRoute(card2, addBtn);
            card2.Controls.Add(addBtn);
            AddRoute(card2, addBtn);

            y += card2.Height + 14;

            var card3 = MakeCard("3 - Review & Install");
            _reviewLabel.Location  = new Point(20, 42);
            _reviewLabel.Size      = new Size(card3.Width - 40, 60);
            _reviewLabel.Font      = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            _reviewLabel.ForeColor = UiFactory.Slate900;
            _reviewLabel.BackColor = Color.Transparent;
            card3.Controls.Add(_reviewLabel);

            y += card3.Height + 14;

            var btnPanel = new Panel
            {
                Location  = new Point(28, y),
                Size      = new Size(_scrollContainer.Width - 56, 44),
                BackColor = Color.FromArgb(226, 232, 240),
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };

            _installBtn.Size = new Size(160, 38);
            _cancelBtn.Size  = new Size(100, 38);

            _cancelBtn.Location  = new Point(btnPanel.Width - _cancelBtn.Width, 3);
            _installBtn.Location = new Point(_cancelBtn.Left - _installBtn.Width - 10, 3);

            _installBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _cancelBtn.Anchor  = AnchorStyles.Top | AnchorStyles.Right;

            _installBtn.Click += OnInstall;
            _cancelBtn.Click  += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

            btnPanel.Controls.AddRange(new Control[] { _installBtn, _cancelBtn });

            _scrollContainer.Controls.Add(card1);
            _scrollContainer.Controls.Add(card2, 0, 1);
            _scrollContainer.Controls.Add(card3, 0, 2);
            _scrollContainer.Controls.Add(btnPanel, 0, 3);
        }

        private RoundedCardPanel MakeCard(string title)
        {
            var card = new RoundedCardPanel
            {
                Dock = DockStyle.Fill,
                Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            card.Controls.Add(new Label
            {
                Text      = title,
                Location  = new Point(20, 14),
                Size      = new Size(500, 22),
                Font      = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                ForeColor = UiFactory.Slate900,
                BackColor = Color.Transparent
            });
            return card;
        }

        private void AddRoute(Panel card, Label addBtn)
        {
            var row = new RouteRow();
            row.RemoveClicked += (s, _) =>
            {
                _rows.Remove((RouteRow)s!);
                _routesPanel.Controls.Remove((RouteRow)s!);
                RefreshRouteLayout(card, addBtn);
                RefreshPreview();
            };
            _rows.Add(row);
            _routesPanel.Controls.Add(row);
            RefreshRouteLayout(card, addBtn);
            RefreshPreview();
        }

        private void RefreshRouteLayout(Panel card, Label addBtn)
        {
            int ry = 0;
            foreach (var row in _rows)
            {
                row.Location = new Point(0, ry);
                ry += row.Height + 4;
            }
            _routesPanel.Height = Math.Max(ry, 4);
            addBtn.Location     = new Point(20, _routesPanel.Bottom + 72);
            card.Height         = addBtn.Bottom + 20;
        }

        private void RefreshPreview()
        {
            var spec = BuildSpec();
            _previewLabel.Text = "Preview: " + BuildTunnelName(spec);

            var lines = new List<string>();
            foreach (var r in spec.Routes)
            {
                string host = BuildHostname(r, spec);
                if (!string.IsNullOrEmpty(host))
                    lines.Add("https://" + host + "  ->  http://localhost:" + r.Port);
            }
            _reviewLabel.Text = string.Join(Environment.NewLine, lines);
        }

        private string BuildTunnelName(InstallSpec spec)
        {
            if (spec.UseCustom && !string.IsNullOrWhiteSpace(spec.CustomName))
                return spec.CustomName;
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(spec.GroupName))  parts.Add(spec.GroupName.ToLower().Replace(" ", "-"));
            if (!string.IsNullOrWhiteSpace(spec.VenueName))  parts.Add(spec.VenueName.ToLower().Replace(" ", "-"));
            if (!string.IsNullOrWhiteSpace(spec.NetSuiteId)) parts.Add(spec.NetSuiteId);
            return parts.Count > 0 ? string.Join("-", parts) : "(pending)";
        }

        internal static string BuildHostname(RouteSpec r, InstallSpec spec)
        {
            if (string.IsNullOrEmpty(r.Domain)) return "";
            string slug = "";
            if (!string.IsNullOrWhiteSpace(spec.GroupName))
                slug += spec.GroupName.ToLower().Replace(" ", "") + "-";
            if (!string.IsNullOrWhiteSpace(spec.VenueName))
                slug += spec.VenueName.ToLower().Replace(" ", "");
            if (!string.IsNullOrWhiteSpace(spec.NetSuiteId))
                slug += "-" + spec.NetSuiteId;
            string prefix = string.IsNullOrWhiteSpace(r.Prefix) ? "" : r.Prefix + "-";
            return prefix + slug + "." + r.Domain;
        }

        private InstallSpec BuildSpec() => new InstallSpec
        {
            NetSuiteId = _netSuiteBox.Text.Trim(),
            GroupName  = _groupBox.Text.Trim(),
            VenueName  = _venueBox.Text.Trim(),
            CustomName = _customBox.Text.Trim(),
            UseCustom  = _tglCustom?.Checked ?? false,
            Routes     = _rows.Select(r => r.Spec).ToList()
        };

        private void OnInstall(object? sender, EventArgs e)
        {
            var spec = BuildSpec();
            if (string.IsNullOrWhiteSpace(spec.NetSuiteId))
            {
                MessageBox.Show("Please enter a NetSuite ID.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(spec.VenueName))
            {
                MessageBox.Show("Please enter a Venue Name.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!spec.Routes.Any())
            {
                MessageBox.Show("Please add at least one route.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Result       = spec;
            DialogResult = DialogResult.OK;
            Close();
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
}
