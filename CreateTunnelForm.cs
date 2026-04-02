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
        private readonly TextBox _groupBox     = new() { PlaceholderText = "Leave blank for standalone venue" };
        private readonly TextBox _venueBox     = new() { PlaceholderText = "e.g. Moon Bar" };
        private readonly TextBox _customBox    = new() { PlaceholderText = "Your tunnel name will preview here" };
        private readonly Label   _previewLabel = new();


        private readonly Panel          _routesPanel = new() { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        private readonly List<RouteRow> _rows        = new();
        private ToggleSwitch _tglCustom = null!;
        private readonly Label          _reviewLabel = new();

        // Buttons
        private readonly PillButton _installBtn = new() { Text = "Create Tunnel", DialogResult = DialogResult.OK, Width = 140, Height = 34, Style = PillButtonStyle.Normal };
        private readonly PillButton _cancelBtn  = new() { Style = PillButtonStyle.Muted, Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90, Height = 34 };

        private readonly TableLayoutPanel _cardContainer = new TableLayoutPanel();

        public InstallSpec? Result { get; private set; }

        public CreateTunnelForm()
        {
            Text            = "Install Tunnel";
            Size            = new Size(820, 700);
            MinimumSize     = new Size(700, 550);
            BackColor       = UiFactory.PageBg;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition   = FormStartPosition.CenterParent;

            _cardContainer.Dock        = DockStyle.Fill;
            _cardContainer.Padding     = new Padding(10, 10, 10, 10);
            _cardContainer.ColumnCount = 1;
            _cardContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _cardContainer.RowCount    = 3;
            _cardContainer.RowStyles.Add(new RowStyle(SizeType.Percent,  25f));
            _cardContainer.RowStyles.Add(new RowStyle(SizeType.Percent,  44f));
            _cardContainer.RowStyles.Add(new RowStyle(SizeType.Percent,  23f));

            Controls.Add(_cardContainer);

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
            var card1 = MakeCard("1 | Tunnel Name");
            int avail = card1.Width - 20;
            int col1x=20, col1w=160, col2x=205, col2w=200, col3x=420, col3w=200;
            card1.Controls.Add(UiFactory.MakeLabel("NetSuite ID", col1x, 44, col1w));
            card1.Controls.Add(UiFactory.StyledTextBox(_netSuiteBox, col1x, 64, col1w));
            card1.Controls.Add(UiFactory.MakeLabel("Group Name",  col2x, 44, col2w));
            card1.Controls.Add(UiFactory.StyledTextBox(_groupBox, col2x, 64, col2w));  
            card1.Controls.Add(UiFactory.MakeLabel("Venue Name",  col3x, 44, col3w));
            card1.Controls.Add(UiFactory.StyledTextBox(_venueBox, col3x, 64, col3w));  
            _venueBox.Location = new Point(col3x, 66); _venueBox.Size = new Size(col3w, 28);
            _tglCustom = new ToggleSwitch { Location = new Point(col1x, 108), Size = new Size(44, 22) };
            var lblCustom = new Label { Text="Custom Name", Location=new Point(col1x+50,111), AutoSize=true,
                Font=new Font("Segoe UI",9f), ForeColor=UiFactory.SlateKey, BackColor=Color.Transparent };
            int previewX=col2x, previewW=400;
            _customBox.Location    = new Point(previewX, 104);
            _customBox.Size        = new Size(previewW, 28);
            _customBox.ReadOnly    = true;
            _customBox.BackColor   = Color.FromArgb(240, 240, 240);
            _customBox.ForeColor   = Color.FromArgb(40, 40, 40);
            _customBox.Font        = new Font("Segoe UI", 9f);
            _customBox.BorderStyle = BorderStyle.FixedSingle;
            _tglCustom.CheckedChanged += (_,__) => { ApplyCustomToggle(); RefreshPreview(); };
            card1.Controls.Add(_tglCustom); card1.Controls.Add(lblCustom);
            card1.Controls.Add(UiFactory.StyledReadOnlyBox(_customBox, col2x, 108, col2w + col3w + 15));
            // Resize col2/col3 once card has real width
            card1.SizeChanged += (_,__) => {
                if (card1.Width < 100) return;
                int c2x  = col1x + col1w + 15;
                int rest = card1.Width - c2x - 20;
                int half = (rest - 10) / 2;
                int c3x  = c2x + half + 10;
                foreach (Control c in card1.Controls) {
                    if (c is Panel p && p.Controls.Count > 0) {
                        if (p.Controls[0] == _groupBox)  { p.Location = new Point(c2x, 64); p.Size = new Size(half, 28); }
                        if (p.Controls[0] == _venueBox)  { p.Location = new Point(c3x, 64); p.Size = new Size(half, 28); }
                        if (p.Controls[0] == _customBox) { p.Location = new Point(c2x, 108); p.Size = new Size(rest, 28); }
                    }
                    if (c is Label lbl2 && lbl2.Text == "Group Name") lbl2.Location = new Point(c2x, 44);
                    if (c is Label lbl3 && lbl3.Text == "Venue Name")  lbl3.Location = new Point(c3x, 44);
                }
            };
            _cardContainer.Controls.Add(card1, 0, 0);

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
            _cardContainer.Controls.Add(card2, 0, 1);

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
                int bw = 160, bh = 36, bx = card3.Width - bw - 20;
                _installBtn.Size     = new Size(bw, bh);
                _cancelBtn.Size      = new Size(bw, bh);
                _installBtn.Location = new Point(bx, card3.Height - bh*2 - 18);
                _cancelBtn.Location  = new Point(bx, card3.Height - bh   - 10);
            };
            card3.Controls.Add(_installBtn); card3.Controls.Add(_cancelBtn);
            _cardContainer.Controls.Add(card3, 0, 2);
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


        private void ApplyCustomToggle()
        {
            bool on = _tglCustom.Checked;
            _customBox.ReadOnly  = !off;
            // _customBox.BackColor = on ? Color.FromArgb(237,233,254) : Color.FromArgb(240,240,240);
            // _customBox.ForeColor = on ? Color.FromArgb(109,40,217)  : Color.FromArgb(40,40,40);
            _netSuiteBox.ReadOnly=on; UiFactory.StyledTextBox(_customBox);
            // var dis = Color.FromArgb(235,235,235); var disfg = Color.FromArgb(130,130,130);
            _netSuiteBox.ReadOnly=on; UiFactory.StyledReadOnlyBox(_customBox); // _netSuiteBox.BackColor=on?dis:Color.FromArgb(40,40,40); _netSuiteBox.ForeColor=on?disfg:Color.FromArgb(40,40,40);
            _groupBox.ReadOnly   =on; UiFactory.StyledReadOnlyBox(_groupBox); // _groupBox.BackColor   =on?dis:Color.FromArgb(40,40,40); _groupBox.ForeColor   =on?disfg:Color.FromArgb(40,40,40);
            _venueBox.ReadOnly   =on; UiFactory.StyledReadOnlyBox(_venueBox); // _venueBox.BackColor   =on?dis:Color.FromArgb(40,40,40); _venueBox.ForeColor   =on?disfg:Color.FromArgb(40,40,40);
        },
        {
            bool off = _tglCustom.Unchecked;
            // _customBox.ReadOnly  = !on;
            // _customBox.BackColor = on ? Color.FromArgb(237,233,254) : Color.FromArgb(240,240,240);
            // _customBox.ForeColor = on ? Color.FromArgb(109,40,217)  : Color.FromArgb(40,40,40);
            _netSuiteBox.ReadOnly=on; UiFactory.StyledReadOnlyBox(_customBox);
            // var dis = Color.FromArgb(235,235,235); var disfg = Color.FromArgb(130,130,130);
            _netSuiteBox.ReadOnly=off; UiFactory.StyledTextBox(_customBox); // _netSuiteBox.BackColor=on?dis:Color.FromArgb(40,40,40); _netSuiteBox.ForeColor=on?disfg:Color.FromArgb(40,40,40);
            _groupBox.ReadOnly   =off; UiFactory.StyledTextBox(_groupBox); // _groupBox.BackColor   =on?dis:Color.FromArgb(40,40,40); _groupBox.ForeColor   =on?disfg:Color.FromArgb(40,40,40);
            _venueBox.ReadOnly   =off; UiFactory.StyledTextBox(_venueBox); // _venueBox.BackColor   =on?dis:Color.FromArgb(40,40,40); _venueBox.ForeColor   =on?disfg:Color.FromArgb(40,40,40);
        }

        private void RefreshPreview()
        {
            var spec = BuildSpec();
            var tunnelname = BuildTunnelName(spec);
            _previewLabel.Text = "Preview: " + tunnelname;

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
