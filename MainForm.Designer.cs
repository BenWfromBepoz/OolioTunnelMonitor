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
            this.toolTip          = new ToolTip();
            this.pnlSidebar       = new Panel();
            this.oolioLogo        = new OolioLogoBrand();
            this.btnCreateTunnel  = new ModernButton();
            this.btnTunnelStatus  = new ModernButton();
            this.btnOpenLogs      = new ModernButton();
            this.btnOpenConfig    = new ModernButton();
            this.btnRepair        = new ModernButton();
            this.chkReinstall     = new CheckBox();
            this.lblVersion       = new Label();
            this.tblMain          = new TableLayoutPanel();
            this.pnlTokenCard     = new RoundedPanel();
            this.lblTokenTitle    = new Label();
            this.txtApiToken      = new TextBox();
            this.chkShowToken     = new CheckBox();
            this.btnTestToken     = new Button();
            this.pnlStatusCard    = new RoundedPanel();
            this.lblCardTitle     = new Label();
            this.tblStatus        = new TableLayoutPanel();
            this.lblServiceLabel  = new Label();
            this.lblService       = new PillLabel();
            this.lblNameLabel     = new Label();
            this.lblTunnelName    = new Label();
            this.lblIdLabel       = new Label();
            this.lblTunnelId      = new Label();
            this.lblRemoteLabel   = new Label();
            this.lblRemoteStatus  = new PillLabel();
            this.pnlIngressCard   = new RoundedPanel();
            this.lblIngressTitle  = new Label();
            this.dgvIngress       = new DataGridView();
            this.colCloud         = new DataGridViewTextBoxColumn();
            this.colLocal         = new DataGridViewTextBoxColumn();
            this.pnlLogCard       = new RoundedPanel();
            this.lblLogTitle      = new Label();
            this.txtLog           = new RichTextBox();

            this.pnlSidebar.SuspendLayout();
            this.tblMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)this.dgvIngress).BeginInit();
            this.SuspendLayout();

            // ToolTip
            this.toolTip.AutoPopDelay = 8000;
            this.toolTip.InitialDelay = 400;
            this.toolTip.ReshowDelay  = 200;

            // Sidebar
            this.pnlSidebar.BackColor = System.Drawing.Color.FromArgb(39, 46, 63);
            this.pnlSidebar.Dock  = DockStyle.Left;
            this.pnlSidebar.Width = 224;
            this.oolioLogo.Location  = new System.Drawing.Point(12, 12);
            this.oolioLogo.Size      = new System.Drawing.Size(200, 106);
            this.oolioLogo.BackColor = System.Drawing.Color.Transparent;
            this.btnCreateTunnel.Text     = "+  Install New Tunnel";
            this.btnCreateTunnel.Location = new System.Drawing.Point(12, 130);
            this.btnCreateTunnel.Size     = new System.Drawing.Size(200, 40);
            this.btnCreateTunnel.Click   += new EventHandler(this.btnCreateTunnel_Click);
            this.btnTunnelStatus.Text     = "\u25cb  Check Tunnel Status";
            this.btnTunnelStatus.Location = new System.Drawing.Point(12, 178);
            this.btnTunnelStatus.Size     = new System.Drawing.Size(200, 40);
            this.btnTunnelStatus.Click   += new EventHandler(this.btnTunnelStatus_Click);
            this.btnOpenLogs.Text     = "\u2261  Open Logfile Folder";
            this.btnOpenLogs.Location = new System.Drawing.Point(12, 226);
            this.btnOpenLogs.Size     = new System.Drawing.Size(200, 40);
            this.btnOpenLogs.Click   += new EventHandler(this.btnOpenLogs_Click);
            this.btnOpenConfig.Text     = "\u25a4  Open Config Folder";
            this.btnOpenConfig.Location = new System.Drawing.Point(12, 274);
            this.btnOpenConfig.Size     = new System.Drawing.Size(200, 40);
            this.btnOpenConfig.Click   += new EventHandler(this.btnOpenConfig_Click);
            this.btnRepair.Text     = "\u2699  Repair Tunnel";
            this.btnRepair.Location = new System.Drawing.Point(12, 322);
            this.btnRepair.Size     = new System.Drawing.Size(200, 40);
            this.btnRepair.Click   += new EventHandler(this.btnRepair_Click);
            this.chkReinstall.Text      = "Reinstall MSI on repair";
            this.chkReinstall.Font      = new System.Drawing.Font("Segoe UI", 8.5f);
            this.chkReinstall.ForeColor = System.Drawing.Color.FromArgb(180, 190, 210);
            this.chkReinstall.Location  = new System.Drawing.Point(20, 368);
            this.chkReinstall.Size      = new System.Drawing.Size(196, 20);
            this.chkReinstall.Checked   = true;
            this.chkReinstall.FlatStyle = FlatStyle.Flat;
            this.lblVersion.Text      = "v1.1.0.2";
            this.lblVersion.Font      = new System.Drawing.Font("Segoe UI", 7.5f);
            this.lblVersion.ForeColor = System.Drawing.Color.FromArgb(100, 115, 140);
            this.lblVersion.Location  = new System.Drawing.Point(14, 500);
            this.lblVersion.Size      = new System.Drawing.Size(196, 16);
            this.lblVersion.BackColor = System.Drawing.Color.Transparent;
            this.lblVersion.Anchor    = AnchorStyles.Bottom | AnchorStyles.Left;
            this.pnlSidebar.Controls.Add(this.oolioLogo);
            this.pnlSidebar.Controls.Add(this.btnCreateTunnel);
            this.pnlSidebar.Controls.Add(this.btnTunnelStatus);
            this.pnlSidebar.Controls.Add(this.btnOpenLogs);
            this.pnlSidebar.Controls.Add(this.btnOpenConfig);
            this.pnlSidebar.Controls.Add(this.btnRepair);
            this.pnlSidebar.Controls.Add(this.chkReinstall);
            this.pnlSidebar.Controls.Add(this.lblVersion);

            // Main TableLayoutPanel
            this.tblMain.Dock        = DockStyle.Fill;
            this.tblMain.BackColor   = System.Drawing.Color.FromArgb(226, 232, 240);
            this.tblMain.Padding     = new Padding(10, 10, 10, 10);
            this.tblMain.ColumnCount = 1;
            this.tblMain.RowCount    = 4;
            this.tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            this.tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute,  68));
            this.tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            this.tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
            this.tblMain.RowStyles.Add(new RowStyle(SizeType.Percent,  100));

            // Token card
            this.pnlTokenCard.Dock   = DockStyle.Fill;
            this.pnlTokenCard.Margin = new Padding(0, 0, 0, 10);
            this.lblTokenTitle.Text      = "Cloudflare API Token";
            this.lblTokenTitle.Font      = new System.Drawing.Font("Segoe UI Semibold", 9f, System.Drawing.FontStyle.Bold);
            this.lblTokenTitle.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblTokenTitle.Location  = new System.Drawing.Point(14, 8);
            this.lblTokenTitle.Size      = new System.Drawing.Size(175, 18);
            this.lblTokenTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblTokenTitle.Cursor    = Cursors.Help;
            this.toolTip.SetToolTip(this.lblTokenTitle, "Found in LastPass or the HubSpot Company Record under Network & Environment");
            this.txtApiToken.Location              = new System.Drawing.Point(14, 30);
            this.txtApiToken.Size                  = new System.Drawing.Size(500, 24);
            this.txtApiToken.Anchor                = AnchorStyles.Top | AnchorStyles.Left;
            this.txtApiToken.UseSystemPasswordChar = true;
            this.txtApiToken.Font                  = new System.Drawing.Font("Cascadia Mono", 8.5f);
            this.txtApiToken.BorderStyle           = BorderStyle.FixedSingle;
            this.txtApiToken.BackColor             = System.Drawing.Color.FromArgb(248, 250, 252);
            this.chkShowToken.Text      = "Show";
            this.chkShowToken.Font      = new System.Drawing.Font("Segoe UI", 8f);
            this.chkShowToken.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.chkShowToken.Location  = new System.Drawing.Point(520, 32);
            this.chkShowToken.Size      = new System.Drawing.Size(52, 18);
            this.chkShowToken.BackColor = System.Drawing.Color.Transparent;
            this.chkShowToken.FlatStyle = FlatStyle.Flat;
            this.chkShowToken.CheckedChanged += (_, _) => { txtApiToken.UseSystemPasswordChar = !chkShowToken.Checked; };
            // Test Token: size 100x28, radius 6 applied uniformly to all 4 corners
            this.btnTestToken.Text                      = "Test Token";
            this.btnTestToken.Location                  = new System.Drawing.Point(578, 20);
            this.btnTestToken.Size                      = new System.Drawing.Size(100, 28);
            this.btnTestToken.FlatStyle                 = FlatStyle.Flat;
            this.btnTestToken.BackColor                 = System.Drawing.Color.FromArgb(103, 58, 182);
            this.btnTestToken.ForeColor                 = System.Drawing.Color.White;
            this.btnTestToken.Font                      = new System.Drawing.Font("Segoe UI", 8.5f);
            this.btnTestToken.FlatAppearance.BorderSize = 0;
            this.btnTestToken.Cursor                    = Cursors.Hand;
            this.btnTestToken.Region                    = RoundedRegion(100, 28, 6);
            this.btnTestToken.Click                    += new EventHandler(this.btnTestToken_Click);
            this.pnlTokenCard.Controls.Add(this.lblTokenTitle);
            this.pnlTokenCard.Controls.Add(this.txtApiToken);
            this.pnlTokenCard.Controls.Add(this.chkShowToken);
            this.pnlTokenCard.Controls.Add(this.btnTestToken);
            this.tblMain.Controls.Add(this.pnlTokenCard, 0, 0);

            // Status card
            this.pnlStatusCard.Dock   = DockStyle.Fill;
            this.pnlStatusCard.Margin = new Padding(0, 0, 0, 10);
            this.pnlStatusCard.Controls.Add(this.lblCardTitle);
            this.pnlStatusCard.Controls.Add(this.tblStatus);
            this.tblMain.Controls.Add(this.pnlStatusCard, 0, 1);
            this.lblCardTitle.Text      = "Tunnel Status";
            this.lblCardTitle.Font      = new System.Drawing.Font("Segoe UI Semibold", 10f, System.Drawing.FontStyle.Bold);
            this.lblCardTitle.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblCardTitle.Location  = new System.Drawing.Point(16, 8);
            this.lblCardTitle.Size      = new System.Drawing.Size(200, 20);
            this.lblCardTitle.BackColor = System.Drawing.Color.Transparent;
            this.tblStatus.Location    = new System.Drawing.Point(16, 32);
            this.tblStatus.Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.tblStatus.Size        = new System.Drawing.Size(200, 72);
            this.tblStatus.BackColor   = System.Drawing.Color.Transparent;
            this.tblStatus.ColumnCount = 4;
            this.tblStatus.RowCount    = 2;
            this.tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            this.tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,   50));
            this.tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            this.tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,   50));
            this.tblStatus.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            this.tblStatus.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            System.Action<Label, string, bool> styleLabel = (lbl, text, isKey) => {
                lbl.Text      = text;
                lbl.Font      = isKey ? new System.Drawing.Font("Segoe UI", 8.5f)
                                      : new System.Drawing.Font("Segoe UI", 8.5f);
                lbl.ForeColor = isKey ? System.Drawing.Color.FromArgb(100, 116, 139)
                                      : System.Drawing.Color.FromArgb(15, 23, 42);
                lbl.Dock      = DockStyle.Fill;
                lbl.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                lbl.AutoSize  = false;
                lbl.BackColor = System.Drawing.Color.Transparent;
            };
            styleLabel(this.lblServiceLabel, "Service",       true);
            styleLabel(this.lblNameLabel,    "Tunnel Name",   true);
            styleLabel(this.lblTunnelName,   "-",             false);
            styleLabel(this.lblIdLabel,      "Tunnel ID",     true);
            styleLabel(this.lblTunnelId,     "-",             false);
            styleLabel(this.lblRemoteLabel,  "Tunnel Status", true);
            // PillLabels - initialised by their own constructor, just set Dock and BackColor
            this.lblService.Dock      = DockStyle.Fill;
            this.lblService.BackColor = System.Drawing.Color.Transparent;
            this.lblService.Cursor    = Cursors.Help;
            this.lblRemoteStatus.Dock      = DockStyle.Fill;
            this.lblRemoteStatus.BackColor = System.Drawing.Color.Transparent;
            this.lblRemoteStatus.Cursor    = Cursors.Help;
            this.tblStatus.Controls.Add(this.lblServiceLabel, 0, 0);
            this.tblStatus.Controls.Add(this.lblService,      1, 0);
            this.tblStatus.Controls.Add(this.lblNameLabel,    2, 0);
            this.tblStatus.Controls.Add(this.lblTunnelName,   3, 0);
            this.tblStatus.Controls.Add(this.lblIdLabel,      0, 1);
            this.tblStatus.Controls.Add(this.lblTunnelId,     1, 1);
            this.tblStatus.Controls.Add(this.lblRemoteLabel,  2, 1);
            this.tblStatus.Controls.Add(this.lblRemoteStatus, 3, 1);

            // Ingress card
            this.pnlIngressCard.Dock   = DockStyle.Fill;
            this.pnlIngressCard.Margin = new Padding(0, 0, 0, 10);
            this.pnlIngressCard.Controls.Add(this.lblIngressTitle);
            this.pnlIngressCard.Controls.Add(this.dgvIngress);
            this.tblMain.Controls.Add(this.pnlIngressCard, 0, 2);
            this.lblIngressTitle.Text      = "Published Routes";
            this.lblIngressTitle.Font      = new System.Drawing.Font("Segoe UI Semibold", 10f, System.Drawing.FontStyle.Bold);
            this.lblIngressTitle.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblIngressTitle.Location  = new System.Drawing.Point(16, 8);
            this.lblIngressTitle.Size      = new System.Drawing.Size(200, 20);
            this.lblIngressTitle.BackColor = System.Drawing.Color.Transparent;
            this.colCloud.HeaderText   = "Cloud Endpoint";
            this.colCloud.Name         = "colCloud";
            this.colCloud.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.colCloud.FillWeight   = 55;
            this.colCloud.ReadOnly     = true;
            this.colLocal.HeaderText   = "Local Endpoint";
            this.colLocal.Name         = "colLocal";
            this.colLocal.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.colLocal.FillWeight   = 45;
            this.colLocal.ReadOnly     = true;
            this.dgvIngress.Location   = new System.Drawing.Point(16, 32);
            this.dgvIngress.Anchor     = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.dgvIngress.Size       = new System.Drawing.Size(200, 92);
            this.dgvIngress.Font       = new System.Drawing.Font("Cascadia Mono", 8.5f);
            this.dgvIngress.EnableHeadersVisualStyles  = false;
            this.dgvIngress.ColumnHeadersBorderStyle   = DataGridViewHeaderBorderStyle.Single;
            this.dgvIngress.ColumnHeadersHeight        = 26;
            this.dgvIngress.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvIngress.DefaultCellStyle.BackColor          = System.Drawing.Color.White;
            this.dgvIngress.DefaultCellStyle.ForeColor          = System.Drawing.Color.FromArgb(30, 41, 59);
            this.dgvIngress.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.White;
            this.dgvIngress.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.dgvIngress.AlternatingRowsDefaultCellStyle.BackColor          = System.Drawing.Color.FromArgb(249, 250, 251);
            this.dgvIngress.AlternatingRowsDefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(249, 250, 251);
            this.dgvIngress.AlternatingRowsDefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.dgvIngress.GridColor             = System.Drawing.Color.FromArgb(226, 232, 240);
            this.dgvIngress.BorderStyle           = BorderStyle.None;
            this.dgvIngress.CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvIngress.SelectionMode         = DataGridViewSelectionMode.FullRowSelect;
            this.dgvIngress.MultiSelect           = false;
            this.dgvIngress.ReadOnly              = true;
            this.dgvIngress.AllowUserToAddRows    = false;
            this.dgvIngress.AllowUserToDeleteRows = false;
            this.dgvIngress.AllowUserToResizeRows = false;
            this.dgvIngress.RowHeadersVisible     = false;
            this.dgvIngress.AutoSizeRowsMode      = DataGridViewAutoSizeRowsMode.None;
            this.dgvIngress.RowTemplate.Height    = 24;
            this.dgvIngress.BackgroundColor       = System.Drawing.Color.White;
            this.dgvIngress.Columns.Add(this.colCloud);
            this.dgvIngress.Columns.Add(this.colLocal);
            this.dgvIngress.Columns["colCloud"].HeaderCell.Style.BackColor = System.Drawing.Color.FromArgb(237, 233, 254);
            this.dgvIngress.Columns["colCloud"].HeaderCell.Style.ForeColor = System.Drawing.Color.FromArgb(76, 29, 149);
            this.dgvIngress.Columns["colCloud"].HeaderCell.Style.Font      = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Bold);
            this.dgvIngress.Columns["colCloud"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
            this.dgvIngress.Columns["colCloud"].HeaderCell.Style.Padding   = new Padding(6, 0, 0, 0);
            this.dgvIngress.Columns["colLocal"].HeaderCell.Style.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.dgvIngress.Columns["colLocal"].HeaderCell.Style.ForeColor = System.Drawing.Color.FromArgb(51, 65, 85);
            this.dgvIngress.Columns["colLocal"].HeaderCell.Style.Font      = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Bold);
            this.dgvIngress.Columns["colLocal"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
            this.dgvIngress.Columns["colLocal"].HeaderCell.Style.Padding   = new Padding(6, 0, 0, 0);

            // Log card - txtLog inset 16px from all card edges for uniform gap
            this.pnlLogCard.Dock   = DockStyle.Fill;
            this.pnlLogCard.Margin = new Padding(0, 0, 0, 0);
            this.pnlLogCard.Controls.Add(this.lblLogTitle);
            this.pnlLogCard.Controls.Add(this.txtLog);
            this.tblMain.Controls.Add(this.pnlLogCard, 0, 3);
            this.lblLogTitle.Text      = "Activity Log";
            this.lblLogTitle.Font      = new System.Drawing.Font("Segoe UI Semibold", 10f, System.Drawing.FontStyle.Bold);
            this.lblLogTitle.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblLogTitle.Location  = new System.Drawing.Point(16, 8);
            this.lblLogTitle.Size      = new System.Drawing.Size(200, 20);
            this.lblLogTitle.BackColor = System.Drawing.Color.Transparent;
            // Anchor all 4 sides with insets: Left=16, Top=32, Right=16 from right edge, Bottom=16 from bottom
            this.txtLog.Location    = new System.Drawing.Point(16, 32);
            this.txtLog.Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.txtLog.Size        = new System.Drawing.Size(200, 100);
            this.txtLog.ReadOnly    = true;
            this.txtLog.ScrollBars  = RichTextBoxScrollBars.Vertical;
            this.txtLog.Font        = new System.Drawing.Font("Cascadia Mono", 8.5f);
            this.txtLog.BorderStyle = BorderStyle.None;
            this.txtLog.BackColor   = System.Drawing.Color.FromArgb(15, 23, 42);
            this.txtLog.ForeColor   = System.Drawing.Color.FromArgb(203, 213, 225);
            this.txtLog.WordWrap    = false;

            // Form
            this.AutoScaleDimensions = new System.Drawing.SizeF(7f, 15f);
            this.AutoScaleMode       = AutoScaleMode.Font;
            this.ClientSize          = new System.Drawing.Size(1040, 700);
            this.MinimumSize         = new System.Drawing.Size(860, 580);
            this.Controls.Add(this.tblMain);
            this.Controls.Add(this.pnlSidebar);
            this.Name          = "MainForm";
            this.Text          = "Oolio ZeroTrust Tunnel Monitor";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = System.Drawing.Color.FromArgb(226, 232, 240);
            this.Shown        += new EventHandler(this.MainForm_Shown);

            this.pnlSidebar.ResumeLayout(false);
            this.tblMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)this.dgvIngress).EndInit();
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

        private ToolTip          toolTip;
        private Panel            pnlSidebar;
        private OolioLogoBrand   oolioLogo;
        private ModernButton     btnCreateTunnel;
        private ModernButton     btnTunnelStatus;
        private ModernButton     btnOpenLogs;
        private ModernButton     btnOpenConfig;
        private ModernButton     btnRepair;
        private CheckBox         chkReinstall;
        private Label            lblVersion;
        private TableLayoutPanel tblMain;
        private RoundedPanel     pnlTokenCard;
        private Label            lblTokenTitle;
        private TextBox          txtApiToken;
        private CheckBox         chkShowToken;
        private Button           btnTestToken;
        private RoundedPanel     pnlStatusCard;
        private Label            lblCardTitle;
        private TableLayoutPanel tblStatus;
        private Label            lblServiceLabel;
        private PillLabel        lblService;
        private Label            lblNameLabel;
        private Label            lblTunnelName;
        private Label            lblIdLabel;
        private Label            lblTunnelId;
        private Label            lblRemoteLabel;
        private PillLabel        lblRemoteStatus;
        private RoundedPanel     pnlIngressCard;
        private Label            lblIngressTitle;
        private DataGridView     dgvIngress;
        private DataGridViewTextBoxColumn colCloud;
        private DataGridViewTextBoxColumn colLocal;
        private RoundedPanel     pnlLogCard;
        private Label            lblLogTitle;
        private RichTextBox      txtLog;
    }
}
