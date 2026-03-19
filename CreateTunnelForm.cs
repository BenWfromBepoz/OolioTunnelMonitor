using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace CloudflaredMonitor
{
    /// <summary>
    /// Collects all the information needed to create a new Cloudflare tunnel
    /// and its associated published routes.
    /// </summary>
    public sealed class CreateTunnelForm : Form
    {
        // ── Result exposed after OK ────────────────────────────────────────────────

        public NewTunnelSpec? Result { get; private set; }

        // ── Fields ──────────────────────────────────────────────────────────────────

        private readonly TextBox  _txtNetSuiteId  = new() { Width = 160 };
        private readonly TextBox  _txtGroupName   = new() { Width = 220 };
        private readonly TextBox  _txtVenueName   = new() { Width = 220 };
        private readonly Label    _lblTunnelPreview = new() { ForeColor = Color.FromArgb(103, 58, 182), Font = new Font("Segoe UI", 9f, FontStyle.Italic) };

        // YourOrder
        private readonly CheckBox  _chkYO    = new() { Text = "YourOrder",   AutoSize = true };
        private readonly NumericUpDown _nudYOCount = new() { Minimum = 1, Maximum = 10, Value = 1, Width = 55 };
        private readonly Panel     _pnlYO    = new() { AutoSize = true };
        private readonly List<(TextBox path, TextBox port)> _yoRows = new();

        // MyPlace
        private readonly CheckBox _chkMP   = new() { Text = "MyPlace",    AutoSize = true };
        private readonly TextBox  _txtMPPort = new() { Width = 80, Text = "8080" };

        // EzeOffice
        private readonly CheckBox _chkEze    = new() { Text = "EzeOffice",  AutoSize = true };
        private readonly TextBox  _txtEzePort = new() { Width = 80, Text = "8081" };

        // TS Plus
        private readonly CheckBox _chkTSP    = new() { Text = "TS Plus",    AutoSize = true };
        private readonly TextBox  _txtTSPPort = new() { Width = 80, Text = "3389" };

        // Other
        private readonly CheckBox _chkOther    = new() { Text = "Other",      AutoSize = true };
        private readonly TextBox  _txtOtherPrefix = new() { Width = 100, PlaceholderText = "prefix" };
        private readonly TextBox  _txtOtherPath   = new() { Width = 100, PlaceholderText = "path (opt)" };
        private readonly TextBox  _txtOtherPort   = new() { Width = 80,  PlaceholderText = "port" };

        private readonly Button _btnOk     = new() { Text = "Create Tunnel", DialogResult = DialogResult.OK, Width = 140, Height = 34 };
        private readonly Button _btnCancel = new() { Text = "Cancel",        DialogResult = DialogResult.Cancel, Width = 90,  Height = 34 };

        // ── Constructor ─────────────────────────────────────────────────────────────────

        public CreateTunnelForm()
        {
            Text            = "Create New Tunnel";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = MinimizeBox = false;
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.FromArgb(241, 245, 249);
            Font            = new Font("Segoe UI", 9.5f);
            AutoScroll      = true;
            ClientSize      = new Size(640, 640);

            var flow = new FlowLayoutPanel
            {
                Dock            = DockStyle.Fill,
                FlowDirection   = FlowDirection.TopDown,
                WrapContents    = false,
                AutoScroll      = true,
                Padding         = new Padding(20, 16, 20, 16),
            };
            Controls.Add(flow);

            // Title
            flow.Controls.Add(MakeHeader("Tunnel Identity"));

            // NetSuite ID
            flow.Controls.Add(MakeRow("NetSuite Company ID *", _txtNetSuiteId,
                "Found on the HubSpot Company Record"));

            // Group Name
            flow.Controls.Add(MakeRow("Group Name", _txtGroupName,
                "Leave blank for sole-trader venues"));

            // Venue Name
            flow.Controls.Add(MakeRow("Venue Name *", _txtVenueName));

            // Preview
            _lblTunnelPreview.AutoSize  = true;
            _lblTunnelPreview.MaximumSize = new Size(580, 0);
            _lblTunnelPreview.Margin    = new Padding(0, 0, 0, 8);
            flow.Controls.Add(_lblTunnelPreview);

            // Wire up preview update
            _txtNetSuiteId.TextChanged  += (_, _) => UpdatePreview();
            _txtGroupName.TextChanged   += (_, _) => UpdatePreview();
            _txtVenueName.TextChanged   += (_, _) => UpdatePreview();

            // Services header
            flow.Controls.Add(MakeHeader("Services"));

            // YourOrder
            var yoPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 4, 0, 0) };
            yoPanel.Controls.Add(_chkYO);
            yoPanel.Controls.Add(MakeSmallLabel("Number of APIs:"));
            yoPanel.Controls.Add(_nudYOCount);
            flow.Controls.Add(yoPanel);

            _nudYOCount.Enabled = false;
            _pnlYO.FlowDirection = System.Windows.Forms.FlowDirection.TopDown; // dynamic rows added below
            _pnlYO.Visible = false;
            _pnlYO.Margin  = new Padding(24, 0, 0, 4);
            flow.Controls.Add(_pnlYO);

            _chkYO.CheckedChanged += (_, _) =>
            {
                _nudYOCount.Enabled = _chkYO.Checked;
                RebuildYORows((int)_nudYOCount.Value);
                _pnlYO.Visible = _chkYO.Checked;
                UpdatePreview();
            };
            _nudYOCount.ValueChanged += (_, _) =>
            {
                if (_chkYO.Checked) RebuildYORows((int)_nudYOCount.Value);
            };

            // MyPlace
            flow.Controls.Add(MakeServiceRow(_chkMP, "Port:", _txtMPPort));

            // EzeOffice
            flow.Controls.Add(MakeServiceRow(_chkEze, "Port:", _txtEzePort));

            // TS Plus
            flow.Controls.Add(MakeServiceRow(_chkTSP, "Port:", _txtTSPPort));

            // Other
            var otherPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 4, 0, 0) };
            otherPanel.Controls.Add(_chkOther);
            otherPanel.Controls.Add(MakeSmallLabel("Prefix:"));
            otherPanel.Controls.Add(_txtOtherPrefix);
            otherPanel.Controls.Add(MakeSmallLabel("Path:"));
            otherPanel.Controls.Add(_txtOtherPath);
            otherPanel.Controls.Add(MakeSmallLabel("Port:"));
            otherPanel.Controls.Add(_txtOtherPort);
            flow.Controls.Add(otherPanel);

            _txtMPPort.Enabled = _txtEzePort.Enabled = _txtTSPPort.Enabled = false;
            _txtOtherPrefix.Enabled = _txtOtherPath.Enabled = _txtOtherPort.Enabled = false;

            _chkMP.CheckedChanged    += (_, _) => { _txtMPPort.Enabled    = _chkMP.Checked;    UpdatePreview(); };
            _chkEze.CheckedChanged   += (_, _) => { _txtEzePort.Enabled   = _chkEze.Checked;   UpdatePreview(); };
            _chkTSP.CheckedChanged   += (_, _) => { _txtTSPPort.Enabled   = _chkTSP.Checked;   UpdatePreview(); };
            _chkOther.CheckedChanged += (_, _) =>
            {
                _txtOtherPrefix.Enabled = _txtOtherPath.Enabled = _txtOtherPort.Enabled = _chkOther.Checked;
                UpdatePreview();
            };

            // Spacer + buttons
            flow.Controls.Add(new Panel { Height = 12 });
            flow.Controls.Add(MakeHeader("Review"));
            var preview = new Label { AutoSize = false, Size = new Size(580, 0), Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(71, 85, 105) };
            preview.Name = "lblRoutePreview";
            preview.AutoSize = true;
            preview.MaximumSize = new Size(580, 0);
            flow.Controls.Add(preview);

            flow.Controls.Add(new Panel { Height = 8 });
            var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            StyleButton(_btnOk,     Color.FromArgb(103, 58, 182), Color.White);
            StyleButton(_btnCancel, Color.FromArgb(226, 232, 240), Color.FromArgb(30, 41, 59));
            btnPanel.Controls.Add(_btnOk);
            btnPanel.Controls.Add(_btnCancel);
            flow.Controls.Add(btnPanel);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            _btnOk.Click += BtnOk_Click;

            // Initial route preview wiring
            foreach (var ctrl in new System.Windows.Forms.Control[] { _txtMPPort, _txtEzePort, _txtTSPPort, _txtOtherPrefix, _txtOtherPath, _txtOtherPort, _txtVenueName })
                ctrl.TextChanged += (_, _) => UpdateRoutePreview(preview);

            UpdatePreview();
        }

        // ── Dynamic YO rows ─────────────────────────────────────────────────────────

        private void RebuildYORows(int count)
        {
            _pnlYO.Controls.Clear();
            _yoRows.Clear();
            for (int i = 0; i < count; i++)
            {
                var row = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 2, 0, 2) };
                row.Controls.Add(new Label { Text = $"API {i + 1}:", AutoSize = true, Width = 48, TextAlign = System.Drawing.ContentAlignment.MiddleLeft });
                var path = new TextBox { Width = 140, PlaceholderText = "pathname (e.g. api)" };
                var port = new TextBox { Width = 80,  PlaceholderText = "port" };
                row.Controls.Add(new Label { Text = "Path:", AutoSize = true, TextAlign = System.Drawing.ContentAlignment.MiddleLeft });
                row.Controls.Add(path);
                row.Controls.Add(new Label { Text = "Port:", AutoSize = true, TextAlign = System.Drawing.ContentAlignment.MiddleLeft });
                row.Controls.Add(port);
                _pnlYO.Controls.Add(row);
                _yoRows.Add((path, port));
                path.TextChanged += (_, _) => UpdatePreview();
                port.TextChanged += (_, _) => UpdatePreview();
            }
        }

        // ── Preview ───────────────────────────────────────────────────────────────────

        private void UpdatePreview()
        {
            _lblTunnelPreview.Text = "Tunnel name: " + BuildTunnelName();
            UpdateRoutePreview(Controls.Find("lblRoutePreview", true).FirstOrDefault() as Label);
        }

        private void UpdateRoutePreview(Label? lbl)
        {
            if (lbl == null) return;
            var routes = BuildRoutes();
            if (routes.Count == 0) { lbl.Text = "No routes configured yet."; return; }
            lbl.Text = string.Join("\n", routes.Select(r => $"  {r.CloudUrl}  →  http://localhost:{r.Port}"));
        }

        private string BuildTunnelName()
        {
            string venue = _txtVenueName.Text.Trim();
            string group = _txtGroupName.Text.Trim();
            string ns    = _txtNetSuiteId.Text.Trim();
            if (string.IsNullOrEmpty(venue)) return "(enter venue name)";
            string name  = string.IsNullOrEmpty(group) ? venue : $"{group} - {venue}";
            return string.IsNullOrEmpty(ns) ? name : $"{name} [{ns}]";
        }

        // slug: lowercase, spaces to hyphens, remove non-alphanumeric except hyphen
        private static string Slugify(string s) =>
            System.Text.RegularExpressions.Regex.Replace(s.ToLowerInvariant().Replace(' ', '-'), @"[^a-z0-9\-]", "");

        private List<RouteSpec> BuildRoutes()
        {
            var routes  = new List<RouteSpec>();
            string slug = Slugify(_txtVenueName.Text.Trim());
            if (string.IsNullOrEmpty(slug)) return routes;

            // YourOrder
            if (_chkYO.Checked)
                foreach (var (path, port) in _yoRows)
                {
                    string p = path.Text.Trim().Trim('/');
                    string portVal = port.Text.Trim();
                    if (!string.IsNullOrEmpty(portVal))
                        routes.Add(new RouteSpec($"yo-{slug}.bepozconnect.com", p, portVal));
                }

            // MyPlace
            if (_chkMP.Checked && !string.IsNullOrWhiteSpace(_txtMPPort.Text))
                routes.Add(new RouteSpec($"mp-{slug}.bepozconnect.com", "", _txtMPPort.Text.Trim()));

            // EzeOffice
            if (_chkEze.Checked && !string.IsNullOrWhiteSpace(_txtEzePort.Text))
                routes.Add(new RouteSpec($"eze-{slug}.bepozconnect.com", "", _txtEzePort.Text.Trim()));

            // TS Plus
            if (_chkTSP.Checked && !string.IsNullOrWhiteSpace(_txtTSPPort.Text))
                routes.Add(new RouteSpec($"tsp-{slug}.bepozconnect.com", "", _txtTSPPort.Text.Trim()));

            // Other
            if (_chkOther.Checked && !string.IsNullOrWhiteSpace(_txtOtherPort.Text) && !string.IsNullOrWhiteSpace(_txtOtherPrefix.Text))
            {
                string prefix = Slugify(_txtOtherPrefix.Text.Trim());
                string path   = _txtOtherPath.Text.Trim().Trim('/');
                routes.Add(new RouteSpec($"{prefix}-{slug}.bepozconnect.com", path, _txtOtherPort.Text.Trim()));
            }

            return routes;
        }

        // ── OK handler ─────────────────────────────────────────────────────────────────

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(_txtNetSuiteId.Text))
            { MessageBox.Show("NetSuite Company ID is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); DialogResult = DialogResult.None; return; }
            if (string.IsNullOrWhiteSpace(_txtVenueName.Text))
            { MessageBox.Show("Venue Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); DialogResult = DialogResult.None; return; }

            var routes = BuildRoutes();
            if (routes.Count == 0)
            { MessageBox.Show("Please configure at least one service.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); DialogResult = DialogResult.None; return; }

            Result = new NewTunnelSpec
            {
                TunnelName  = BuildTunnelName(),
                NetSuiteId  = _txtNetSuiteId.Text.Trim(),
                GroupName   = _txtGroupName.Text.Trim(),
                VenueName   = _txtVenueName.Text.Trim(),
                Routes      = routes
            };
        }

        // ── UI helpers ─────────────────────────────────────────────────────────────────

        private static Label MakeHeader(string text) => new()
        {
            Text      = text,
            Font      = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(39, 46, 63),
            AutoSize  = true,
            Margin    = new Padding(0, 8, 0, 4)
        };

        private static Label MakeSmallLabel(string text) => new()
        {
            Text      = text,
            AutoSize  = true,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            Margin    = new Padding(4, 0, 2, 0)
        };

        private static Panel MakeRow(string labelText, System.Windows.Forms.Control input, string? hint = null)
        {
            var row = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 3, 0, 3) };
            row.Controls.Add(new Label { Text = labelText, Width = 170, TextAlign = System.Drawing.ContentAlignment.MiddleLeft, AutoSize = false });
            row.Controls.Add(input);
            if (hint != null)
                row.Controls.Add(new Label { Text = hint, ForeColor = Color.FromArgb(100, 116, 139), Font = new Font("Segoe UI", 8f), AutoSize = true, TextAlign = System.Drawing.ContentAlignment.MiddleLeft, Margin = new Padding(6, 0, 0, 0) });
            return row;
        }

        private static Panel MakeServiceRow(CheckBox chk, string portLabel, TextBox portBox)
        {
            var row = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 4, 0, 0) };
            row.Controls.Add(chk);
            row.Controls.Add(MakeSmallLabel(portLabel));
            row.Controls.Add(portBox);
            return row;
        }

        private static void StyleButton(Button btn, Color back, Color fore)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor  = back;
            btn.ForeColor  = fore;
            btn.Font       = new Font("Segoe UI", 9.5f);
            btn.Cursor     = Cursors.Hand;
            btn.Margin     = new Padding(0, 0, 8, 0);
        }
    }

    // ── Data models ─────────────────────────────────────────────────────────────────

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
        public string Path     { get; }   // empty string = no path
        public string Port     { get; }
        public string CloudUrl => string.IsNullOrEmpty(Path) ? Hostname : $"{Hostname}/{Path}";
        public string Service  => $"http://localhost:{Port}";

        public RouteSpec(string hostname, string path, string port)
        { Hostname = hostname; Path = path.Trim('/'); Port = port; }
    }
}
