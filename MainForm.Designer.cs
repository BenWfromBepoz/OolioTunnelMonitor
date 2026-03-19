using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CloudflaredMonitor
{
    public partial class MainForm : Form
    {
        private System.ComponentModel.IContainer components = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pnlSidebar       = new Panel();
            this.oolioLogo        = new OolioLogoBrand();
            this.btnRefresh       = new ModernButton();
            this.btnTunnelStatus  = new ModernButton();
            this.btnRetrieve      = new ModernButton();
            this.btnOpenLogs      = new ModernButton();
            this.btnRepair        = new ModernButton();
            this.btnCreateTunnel  = new ModernButton();
            this.chkReinstall     = new CheckBox();
            this.lblVersion       = new Label();
            this.pnlMain          = new Panel();
            this.pnlTokenCard     = new RoundedPanel();
            this.lblTokenTitle    = new Label();
            this.lblTokenHint     = new Label();
            this.txtApiToken      = new TextBox();
            this.chkShowToken     = new CheckBox();
            this.btnTestToken     = new Button();
            this.pnlStatusCard    = new RoundedPanel();
            this.lblCardTitle     = new Label();
            this.tblStatus        = new TableLayoutPanel();
            this.lblServiceLabel  = new Label();
            this.lblService       = new Label();
            this.lblNameLabel     = new Label();
            this.lblTunnelName    = new Label();
            this.lblIdLabel       = new Label();
            this.lblTunnelId      = new Label();
            this.lblRemoteLabel   = new Label();
            this.lblRemoteStatus  = new Label();
            this.pnlIngressCard   = new RoundedPanel();
            this.lblIngressTitle  = new Label();
            this.lstIngress       = new ListView();
            this.colCloud         = new ColumnHeader();
            this.colLocal         = new ColumnHeader();
            this.pnlLogCard       = new RoundedPanel();
            this.lblLogTitle      = new Label();
            this.txtLog           = new TextBox();

            this.pnlSidebar.SuspendLayout();
            this.pnlMain.SuspendLayout();
            this.SuspendLayout();

            // Sidebar
            this.pnlSidebar.BackColor = System.Drawing.Color.FromArgb(39, 46, 63);
            this.pnlSidebar.Dock = DockStyle.Left;
            this.pnlSidebar.Width = 224;

            this.oolioLogo.Location = new System.Drawing.Point(12, 12);
            this.oolioLogo.Size = new System.Drawing.Size(200, 106);
            this.oolioLogo.BackColor = System.Drawing.Color.Transparent;

            // --- Manage Tunnel group ---
            this.btnRefresh.Text = "⟳  Check Service Status";
            this.btnRefresh.Location = new System.Drawing.Point(12, 130);
            this.btnRefresh.Size = new System.Drawing.Size(200, 40);
            this.btnRefresh.Click += new EventHandler(this.btnRefresh_Click);

            this.btnTunnelStatus.Text = "○  Check Tunnel Status";
            this.btnTunnelStatus.Location = new System.Drawing.Point(12, 178);
            this.btnTunnelStatus.Size = new System.Drawing.Size(200, 40);
            this.btnTunnelStatus.Click += new EventHandler(this.btnTunnelStatus_Click);

            this.btnRetrieve.Text = "↓  Retrieve Tunnel Details";
            this.btnRetrieve.Location = new System.Drawing.Point(12, 226);
            this.btnRetrieve.Size = new System.Drawing.Size(200, 40);
            this.btnRetrieve.Click += new EventHandler(this.btnRetrieve_Click);

            this.btnOpenLogs.Text = "≡  Open Logfile Folder";
            this.btnOpenLogs.Location = new System.Drawing.Point(12, 274);
            this.btnOpenLogs.Size = new System.Drawing.Size(200, 40);
            this.btnOpenLogs.Click += new EventHandler(this.btnOpenLogs_Click);

            this.btnRepair.Text = "⚙  Repair Tunnel";
            this.btnRepair.Location = new System.Drawing.Point(12, 322);
            this.btnRepair.Size = new System.Drawing.Size(200, 40);
            this.btnRepair.Click += new EventHandler(this.btnRepair_Click);

            this.chkReinstall.Text = "Reinstall MSI on repair";
            this.chkReinstall.Font = new System.Drawing.Font("Segoe UI", 8.5f);
            this.chkReinstall.ForeColor = System.Drawing.Color.FromArgb(180, 190, 210);
            this.chkReinstall.Location = new System.Drawing.Point(20, 368);
            this.chkReinstall.Size = new System.Drawing.Size(196, 20);
            this.chkReinstall.Checked = true;
            this.chkReinstall.FlatStyle = FlatStyle.Flat;

            // --- Install New Tunnel (separated by gap) ---
            this.btnCreateTunnel.Text = "+  Install New Tunnel";
            this.btnCreateTunnel.Location = new System.Drawing.Point(12, 402);
            this.btnCreateTunnel.Size = new System.Drawing.Size(200, 40);
            this.btnCreateTunnel.Click += new EventHandler(this.btnCreateTunnel_Click);

            // Version label - bottom of sidebar
            this.lblVersion.Text = "v1.1.0.1";
            this.lblVersion.Font = new System.Drawing.Font("Segoe UI", 7.5f);
            this.lblVersion.ForeColor = System.Drawing.Color.FromArgb(100, 115, 140);
            this.lblVersion.Location = new System.Drawing.Point(14, 452);
            this.lblVersion.Size = new System.Drawing.Size(196, 16);
            this.lblVersion.BackColor = System.Drawing.Color.Transparent;
            this.lblVersion.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            this.pnlSidebar.Controls.Add(this.oolioLogo);
            this.pnlSidebar.Controls.Add(this.btnRefresh);
            this.pnlSidebar.Controls.Add(this.btnTunnelStatus);
            this.pnlSidebar.Controls.Add(this.btnRetrieve);
            this.pnlSidebar.Controls.Add(this.btnOpenLogs);
            this.pnlSidebar.Controls.Add(this.btnRepair);
            this.pnlSidebar.Controls.Add(this.chkReinstall);
            this.pnlSidebar.Controls.Add(this.btnCreateTunnel);
            this.pnlSidebar.Controls.Add(this.lblVersion);

            // Main panel
            this.pnlMain.Dock = DockStyle.Fill;
            this.pnlMain.BackColor = System.Drawing.Color.FromArgb(226, 232, 240);
            this.pnlMain.Padding = new Padding(12, 12, 12, 12);
            this.pnlMain.Controls.Add(this.pnlLogCard);
            this.pnlMain.Controls.Add(this.pnlIngressCard);
            this.pnlMain.Controls.Add(this.pnlStatusCard);
            this.pnlMain.Controls.Add(this.pnlTokenCard);

            // Token card
            this.pnlTokenCard.Location = new System.Drawing.Point(12, 12);
            this.pnlTokenCard.Size = new System.Drawing.Size(780, 76);
            this.pnlTokenCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            this.lblTokenTitle.Text = "Cloudflare API Token";
            this.lblTokenTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 9f, System.Drawing.FontStyle.Bold);
            this.lblTokenTitle.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblTokenTitle.Location = new System.Drawing.Point(14, 10);
            this.lblTokenTitle.Size = new System.Drawing.Size(160, 18);
            this.lblTokenTitle.BackColor = System.Drawing.Color.Transparent;

            this.lblTokenHint.Text = "ⓘ  Found in LastPass or HubSpot → Company Record → Network & Environment";
            this.lblTokenHint.Font = new System.Drawing.Font("Segoe UI", 7.5f);
            this.lblTokenHint.ForeColor = System.Drawing.Color.FromArgb(103, 58, 182);
            this.lblTokenHint.Location = new System.Drawing.Point(178, 12);
            this.lblTokenHint.Size = new System.Drawing.Size(380, 16);
            this.lblTokenHint.BackColor = System.Drawing.Color.Transparent;

            this.txtApiToken.Location = new System.Drawing.Point(14, 36);
            this.txtApiToken.Size = new System.Drawing.Size(500, 24);
            this.txtApiToken.UseSystemPasswordChar = true;
            this.txtApiToken.Font = new System.Drawing.Font("Cascadia Mono", 8.5f);
            this.txtApiToken.BorderStyle = BorderStyle.FixedSingle;
            this.txtApiToken.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);

            this.chkShowToken.Text = "Show";
            this.chkShowToken.Font = new System.Drawing.Font("Segoe UI", 8f);
            this.chkShowToken.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.chkShowToken.Location = new System.Drawing.Point(520, 38);
            this.chkShowToken.Size = new System.Drawing.Size(52, 18);
            this.chkShowToken.BackColor = System.Drawing.Color.Transparent;
            this.chkShowToken.FlatStyle = FlatStyle.Flat;
            this.chkShowToken.CheckedChanged += (_, _) => { txtApiToken.UseSystemPasswordChar = !chkShowToken.Checked; };

            this.btnTestToken.Text = "Test Token";
            this.btnTestToken.Location = new System.Drawing.Point(578, 32);
            this.btnTestToken.Size = new System.Drawing.Size(100, 30);
            this.btnTestToken.FlatStyle = FlatStyle.Flat;
            this.btnTestToken.BackColor = System.Drawing.Color.FromArgb(103, 58, 182);
            this.btnTestToken.ForeColor = System.Drawing.Color.White;
            this.btnTestToken.Font = new System.Drawing.Font("Segoe UI", 8.5f);
            this.btnTestToken.FlatAppearance.BorderSize = 0;
            this.btnTestToken.Cursor = Cursors.Hand;
            this.btnTestToken.Region = RoundedRegion(100, 30, 6);
            this.btnTestToken.Click += new EventHandler(this.btnTestToken_Click);

            this.pnlTokenCard.Controls.Add(this.lblTokenTitle);
            this.pnlTokenCard.Controls.Add(this.lblTokenHint);
            this.pnlTokenCard.Controls.Add(this.txtApiToken);
            this.pnlTokenCard.Controls.Add(this.chkShowToken);
            this.pnlTokenCard.Controls.Add(this.btnTestToken);

            // Status card
            this.pnlStatusCard.Location = new System.Drawing.Point(12, 100);
            this.pnlStatusCard.Size = new System.Drawing.Size(780, 148);
            this.pnlStatusCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.pnlStatusCard.Controls.Add(this.lblCardTitle);
            this.pnlStatusCard.Controls.Add(this.tblStatus);

            this.lblCardTitle.Text = "Tunnel Status";
            this.lblCardTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 10f, System.Drawing.FontStyle.Bold);
            this.lblCardTitle.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblCardTitle.Location = new System.Drawing.Point(16, 12);
            this.lblCardTitle.Size = new System.Drawing.Size(200, 22);
            this.lblCardTitle.BackColor = System.Drawing.Color.Transparent;

            this.tblStatus.Location = new System.Drawing.Point(16, 40);
            this.tblStatus.Size = new System.Drawing.Size(748, 92);
            this.tblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.tblStatus.ColumnCount = 4;
            this.tblStatus.RowCount = 2;
            this.tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
            this.tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310));
            this.tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            this.tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            this.tblStatus.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            this.tblStatus.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            System.Action<Label, string, bool> styleLabel = (lbl, text, isKey) => {
                lbl.Text = text;
                lbl.Font = isKey ? new System.Drawing.Font("Segoe UI", 8.5f) : new System.Drawing.Font("Segoe UI Semibold", 9.5f, System.Drawing.FontStyle.Bold);
                lbl.ForeColor = isKey ? System.Drawing.Color.FromArgb(71, 85, 105) : System.Drawing.Color.FromArgb(15, 23, 42);
                lbl.Dock = DockStyle.Fill;
                lbl.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                lbl.AutoSize = false;
                lbl.BackColor = System.Drawing.Color.Transparent;
            };
            styleLabel(this.lblServiceLabel, "Service",       true);
            styleLabel(this.lblService,      "-",             false);
            styleLabel(this.lblNameLabel,    "Tunnel Name",   true);
            styleLabel(this.lblTunnelName,   "-",             false);
            styleLabel(this.lblIdLabel,      "Tunnel ID",     true);
            styleLabel(this.lblTunnelId,     "-",             false);
            styleLabel(this.lblRemoteLabel,  "Remote Status", true);
            styleLabel(this.lblRemoteStatus, "-",             false);

            this.tblStatus.Controls.Add(this.lblServiceLabel, 0, 0);
            this.tblStatus.Controls.Add(this.lblService,      1, 0);
            this.tblStatus.Controls.Add(this.lblNameLabel,    2, 0);
            this.tblStatus.Controls.Add(this.lblTunnelName,   3, 0);
            this.tblStatus.Controls.Add(this.lblIdLabel,      0, 1);
            this.tblStatus.Controls.Add(this.lblTunnelId,     1, 1);
            this.tblStatus.Controls.Add(this.lblRemoteLabel,  2, 1);
            this.tblStatus.Controls.Add(this.lblRemoteStatus, 3, 1);

            // Ingress card
            this.pnlIngressCard.Location = new System.Drawing.Point(12, 260);
            this.pnlIngressCard.Size = new System.Drawing.Size(780, 148);
            this.pnlIngressCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.pnlIngressCard.Controls.Add(this.lblIngressTitle);
            this.pnlIngressCard.Controls.Add(this.lstIngress);

            this.lblIngressTitle.Text = "Published Routes";
            this.lblIngressTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 10f, System.Drawing.FontStyle.Bold);
            this.lblIngressTitle.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblIngressTitle.Location = new System.Drawing.Point(16, 12);
            this.lblIngressTitle.Size = new System.Drawing.Size(200, 22);
            this.lblIngressTitle.BackColor = System.Drawing.Color.Transparent;

            this.colCloud.Text  = "Cloud Endpoint";
            this.colCloud.Width = 380;
            this.colLocal.Text  = "Local Endpoint";
            this.colLocal.Width = 340;

            this.lstIngress.Location = new System.Drawing.Point(16, 40);
            this.lstIngress.Size = new System.Drawing.Size(748, 92);
            this.lstIngress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.lstIngress.View = View.Details;
            this.lstIngress.FullRowSelect = true;
            this.lstIngress.GridLines = false;
            this.lstIngress.BorderStyle = BorderStyle.None;
            this.lstIngress.BackColor = System.Drawing.Color.White;
            this.lstIngress.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.lstIngress.Font = new System.Drawing.Font("Cascadia Mono", 8.5f);
            this.lstIngress.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            this.lstIngress.Columns.Add(this.colCloud);
            this.lstIngress.Columns.Add(this.colLocal);

            // Log card
            this.pnlLogCard.Location = new System.Drawing.Point(12, 420);
            this.pnlLogCard.Size = new System.Drawing.Size(780, 200);
            this.pnlLogCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.pnlLogCard.Controls.Add(this.lblLogTitle);
            this.pnlLogCard.Controls.Add(this.txtLog);

            this.lblLogTitle.Text = "Activity Log";
            this.lblLogTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 10f, System.Drawing.FontStyle.Bold);
            this.lblLogTitle.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblLogTitle.Location = new System.Drawing.Point(16, 12);
            this.lblLogTitle.Size = new System.Drawing.Size(200, 22);
            this.lblLogTitle.BackColor = System.Drawing.Color.Transparent;

            this.txtLog.Location = new System.Drawing.Point(16, 40);
            this.txtLog.Size = new System.Drawing.Size(748, 144);
            this.txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.txtLog.Multiline = true;
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = ScrollBars.Vertical;
            this.txtLog.Font = new System.Drawing.Font("Cascadia Mono", 8.5f);
            this.txtLog.BorderStyle = BorderStyle.None;
            this.txtLog.BackColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.txtLog.ForeColor = System.Drawing.Color.FromArgb(203, 213, 225);

            // Form
            this.AutoScaleDimensions = new System.Drawing.SizeF(7f, 15f);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1040, 700);
            this.MinimumSize = new System.Drawing.Size(900, 640);
            this.Controls.Add(this.pnlMain);
            this.Controls.Add(this.pnlSidebar);
            this.Name = "MainForm";
            this.Text = "Oolio ZeroTrust Tunnel Monitor";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = System.Drawing.Color.FromArgb(226, 232, 240);

            this.pnlSidebar.ResumeLayout(false);
            this.pnlMain.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private static System.Drawing.Region RoundedRegion(int w, int h, int r)
        {
            int d = r * 2;
            var path = new GraphicsPath();
            path.AddArc(0,     0,     d, d, 180, 90);
            path.AddArc(w - d, 0,     d, d, 270, 90);
            path.AddArc(w - d, h - d, d, d,   0, 90);
            path.AddArc(0,     h - d, d, d,  90, 90);
            path.CloseFigure();
            return new System.Drawing.Region(path);
        }

        private Panel            pnlSidebar;
        private OolioLogoBrand   oolioLogo;
        private ModernButton     btnRefresh;
        private ModernButton     btnTunnelStatus;
        private ModernButton     btnRetrieve;
        private ModernButton     btnOpenLogs;
        private ModernButton     btnRepair;
        private ModernButton     btnCreateTunnel;
        private CheckBox         chkReinstall;
        private Label            lblVersion;
        private Panel            pnlMain;
        private RoundedPanel     pnlTokenCard;
        private Label            lblTokenTitle;
        private Label            lblTokenHint;
        private TextBox          txtApiToken;
        private CheckBox         chkShowToken;
        private Button           btnTestToken;
        private RoundedPanel     pnlStatusCard;
        private Label            lblCardTitle;
        private TableLayoutPanel tblStatus;
        private Label            lblServiceLabel;
        private Label            lblService;
        private Label            lblNameLabel;
        private Label            lblTunnelName;
        private Label            lblIdLabel;
        private Label            lblTunnelId;
        private Label            lblRemoteLabel;
        private Label            lblRemoteStatus;
        private RoundedPanel     pnlIngressCard;
        private Label            lblIngressTitle;
        private ListView         lstIngress;
        private ColumnHeader     colCloud;
        private ColumnHeader     colLocal;
        private RoundedPanel     pnlLogCard;
        private Label            lblLogTitle;
        private TextBox          txtLog;
    }
}
