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
        private readonly PillButton _cancelBtn  = new() { Style = PillButtonStyle.Muted, Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90, Height = 34, Style = PillButtonStyle.Active };

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
            _scrollContainer.RowCount    = 3;
            _scrollContainer.RowStyles.Add(new RowStyle(SizeType.Percent,  25f));
            _scrollContainer.RowStyles.Add(new RowStyle(SizeType.Percent,  44f));
            _scrollContainer.RowStyles.Add(new RowStyle(SizeType.Percent,  21f));

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
            // ── Card 1: Tunnel Identity ──────────────────────────────────
            var card1 = MakeCard("1 - Tunnel Identity");
            int col1x=20, colW=175, col2x=210, col3x=400;
            card1.Controls.Add(UiFactory.MakeLabel("NetSuite ID", col1x, 44, colW));
            card1.Controls.Add(UiFactory.MakeLabel("Group Name",  col2x, 44, colW));
            card1.Controls.Add(UiFactory.MakeLabel("Venue Name",  col3x, 44, colW));
            card1.Controls.Add(UiFactory.StyledTextBox(_netSuiteBox, col1x, 64, colW));
            card1.Controls.Add(UiFactory.StyledTextBox(_groupBox,    col2x, 64, colW));
            card1.Controls.Add(UiFactory.StyledTextBox(_venueBox,    col3x, 64, colW));
            _tglCustom = new ToggleSwitch { Location = new Point(col1x, 108), Size = new Size(44, 22) };
            var lblCustom = new Label { Text="Custom name", Location=new Point(col1x+50,111), AutoSize=true,
                Font=new Font("Segoe UI",9f), ForeColor=UiFactory.SlateKey, BackColor=Color.Transparent };
            int previewX=col2x, previewW=col3x+colW-col2x;
            _customBox.Location    = new Point(previewX, 104);
            _customBox.Size        = new Size(previewW, 28);
            _customBox.ReadOnly    = true;
            _customBox.BackColor   = Color.FromArgb(240, 240, 240);
            _customBox.ForeColor   = Color.FromArgb(40, 40, 40);
            _customBox.Font        = new Font("Segoe UI", 9f);
            _customBox.BorderStyle = BorderStyle.FixedSingle;
            _tglCustom.CheckedChanged += (_,__) => { ApplyCustomToggle(); RefreshPreview(); };
            card1.Controls.Add(_tglCustom); card1.Controls.Add(lblCustom); card1.Controls.Add(_customBox);
            _scrollContainer.Controls.Add(card1, 0, 0);

            // ── Card 2: Published Routes ──────────────────────────────────
            var card2 = MakeCard("2 - Published Routes");
            int hx=20;
            foreach (var (col,w) in new[]{("Service",130),("Port",80),("Prefix",100),("Domain",260)})
            {
                card2.Controls.Add(new Label { Text=col, Location=new Point(hx,42), Size=new Size(w,16),
                    Font=new Font("Segoe UI Semibold",8f,FontStyle.Bold),
                    ForeColor=UiFactory.SlateKey, BackColor=Color.Transparent });
                hx += w;
            }
            card2.Controls.Add(_routesPanel);
            var addBtn = new Label { Text="+ Add Route", AutoSize=true,
                Font=new Font("Segoe UI Semibold",9f,FontStyle.Bold),
                ForeColor=UiFactory.Purple700, BackColor=Color.Transparent, Cursor=Cursors.Hand };
            card2.SizeChanged += (_,__) => {
                addBtn.Location = new Point(card2.Width - addBtn.Width - 20, card2.Height - addBtn.Height - 14);
            };
            addBtn.MouseClick += (_,__) => AddRoute(card2, addBtn);
            card2.Controls.Add(addBtn);
            AddRoute(card2, addBtn);
            _scrollContainer.Controls.Add(card2, 0, 1);

            // ── Card 3: Review & Install (buttons inside) ────────────────
            var card3 = MakeCard("3 - Review & Install");
            _reviewLabel.Location=new Point(20,44); _reviewLabel.Size=new Size(500,100);
            _reviewLabel.Font=new Font("Segoe UI",8.5f); _reviewLabel.ForeColor=UiFactory.SlateKey;
            _reviewLabel.BackColor=Color.Transparent;
            card3.Controls.Add(_reviewLabel);
            _installBtn.Size=new Size(150,38); _cancelBtn.Size=new Size(110,38);
            _installBtn.Anchor=AnchorStyles.Bottom|AnchorStyles.Right;
            _cancelBtn.Anchor=AnchorStyles.Bottom|AnchorStyles.Right;
            card3.SizeChanged += (_,__) => {
                _installBtn.Location = new Point(card3.Width-_installBtn.Width-20, card3.Height-_installBtn.Height-14);
                _cancelBtn.Location  = new Point(card3.Width-_installBtn.Width-_cancelBtn.Width-28, card3.Height-_cancelBtn.Height-14);
            };
            card3.Controls.Add(_installBtn); card3.Controls.Add(_cancelBtn);
            _scrollContainer.Controls.Add(card3, 0, 2);
        }

        private RoundedCardPanel MakeCard(string title)
        {
            var card = new RoundedCardPanel
            {
                Dock   = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8)
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

        pr

        private void ApplyCustomToggle()
        {
            bool on = _tglCustom.Checked;
            _customBox.ReadOnly  = !on;
            _customBox.BackColor = on ? Color.FromArgb(237,233,254) : Color.FromArgb(240,240,240);
            _customBox.ForeColor = on ? Color.FromArgb(109,40,217)  : Color.FromArgb(40,40,40);
            var dis = Color.FromArgb(235,235,235); var disfg = Color.FromArgb(130,130,130);
            _netSuiteBox.ReadOnly=on; _netSuiteBox.BackColor=on?dis:Color.White; _netSuiteBox.ForeColor=on?disfg:Color.FromArgb(40,40,40);
            _groupBox.ReadOnly   =on; _groupBox.BackColor   =on?dis:Color.White; _groupBox.ForeColor   =on?disfg:Color.FromArgb(40,40,40);
            _venueBox.ReadOnly   =on; _venueBox.BackColor   =on?dis:Color.White; _venueBox.ForeColor   =on?disfg:Color.FromArgb(40,40,40);
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
