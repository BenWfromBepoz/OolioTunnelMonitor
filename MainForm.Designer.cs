using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace OolioTunnelMonitor
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
            this.oolioLogo        = new OolioSidebarLogo();
            this.btnCreateTunnel  = new ModernButton();
            this.btnTunnelStatus  = new ModernButton();
            this.btnRepair        = new ModernButton();
            this.btnCheckUpdates  = new ModernButton();
            this.lblVersion       = new Label();
            this.contentPanel     = new ContentPanel();
            this.tblMain          = new TableLayoutPanel();
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
            this.pnlTokenCard     = new RoundedPanel();
            this.lblTokenTitle    = new Label();
            this.tokenBorder      = new BorderPanel();
            this.txtApiToken      = new TextBox();
            this.btnTestToken     = new PillButton();
            this.pnlLogCard       = new RoundedPanel();
            this.lblLogTitle      = new Label();
            this.txtLog           = new RichTextBox();

            this.pnlSidebar.SuspendLayout();
            this.contentPanel.SuspendLayout();
            this.tblMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)this.dgvIngress).BeginInit();
            this.SuspendLayout();

            this.toolTip.AutoPopDelay = 8000;
            this.toolTip.InitialDelay = 400;
            this.toolTip.ReshowDelay  = 200;

            // ── Sidebar ────────────────────────────────────────────────────
            this.pnlSidebar.BackColor = System.Drawing.Color.FromArgb(39, 46, 63);
            this.pnlSidebar.Dock  = DockStyle.Left;
            this.pnlSidebar.Width = 224;

            // Logo: OolioSidebarLogo — full sidebar width, taskbar 256x256 icon
            this.oolioLogo.Location  = new System.Drawing.Point(0, 0);
            this.oolioLogo.Size      = new System.Drawing.Size(224, 160);
            this.oolioLogo.BackColor = System.Drawing.Color.Transparent;

            this.btnCreateTunnel.Text     = "+  Install New Tunnel";
            this.btnCreateTunnel.Location = new System.Drawing.Point(12, 130);
            this.btnCreateTunnel.Size     = new System.Drawing.Size(200, 40);
            this.btnCreateTunnel.Click   += new EventHandler(this.btnCreateTunnel_Click);

            this.btnTunnelStatus.Text     = "\u25cb  Check Tunnel Status";
            this.btnTunnelStatus.Location = new System.Drawing.Point(12, 178);
            this.btnTunnelStatus.Size     = new System.Drawing.Size(200, 40);
            this.btnTunnelStatus.Click   += new EventHandler(this.btnTunnelStatus_Click);

            this.btnRepair.Text     = "\u2699  Repair Tunnel";
            this.btnRepair.Location = new System.Drawing.Point(12, 322);
            this.btnRepair.Size     = new System.Drawing.Size(200, 40);
            this.btnRepair.Click   += new EventHandler(this.btnRepair_Click);

            this.btnCheckUpdates.Text     = "\u21bb  Check for Updates";
            this.btnCheckUpdates.Location = new System.Drawing.Point(12, 404);
            this.btnCheckUpdates.Size     = new System.Drawing.Size(200, 36);
            this.btnCheckUpdates.Click   += new EventHandler(this.btnCheckUpdates_Click);

            this.lblVersion.Text      = "v1.2.1.0";
            this.lblVersion.Font      = new System.Drawing.Font("Segoe UI", 7.5f);
            this.lblVersion.ForeColor = System.Drawing.Color.FromArgb(90, 105, 130);
            this.lblVersion.Location  = new System.Drawing.Point(14, 446);
            this.lblVersion.Size      = new System.Drawing.Size(196, 16);
            this.lblVersion.BackColor = System.Drawing.Color.Transparent;

            this.pnlSidebar.Controls.Add(this.oolioLogo);
            this.pnlSidebar.Controls.Add(this.btnCreateTunnel);
            this.pnlSidebar.Controls.Add(this.btnTunnelStatus);
            this.pnlSidebar.Controls.Add(this.btnRepair);
            this.pnlSidebar.Controls.Add(this.btnCheckUpdates);
            this.pnlSidebar.Controls.Add(this.lblVersion);

            // ── ContentPanel (floating, all-corners rounded) ───────────────
            this.contentPanel.Visible = false;
            this.contentPanel.Controls.Add(this.tblMain);

            // ── Main layout (inside contentPanel) ──────────────────────────
            this.tblMain.Dock        = DockStyle.Fill;
            this.tblMain.BackColor   = System.Drawing.Color.FromArgb(226, 232, 240);
            this.tblMain.Padding     = new Padding(10, 10, 10, 10);
            this.tblMain.ColumnCount = 1;
            this.tblMain.RowCount    = 4;
            this.tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            this.tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 114));
            this.tblMain.RowStyles.Add(new RowStyle(SizeType.Percent,   35));
            this.tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute,  68));
            this.tblMain.RowStyles.Add(new RowStyle(SizeType.Percent,   65));

            // ── Status card ────────────────────────────────────────────────
            this.pnlStatusCard.Dock    = DockStyle.Fill;
            this.pnlStatusCard.Margin  = new Padding(0, 0, 0, 8);
            this.pnlStatusCard.Padding = new Padding(0, 0, 12, 0);  // Fix #3: right padding
            this.pnlStatusCard.Controls.Add(this.lblCardTitle);
            this.pnlStatusCard.Controls.Add(this.tblStatus);
            this.tblMain.Controls.Add(this.pnlStatusCard, 0, 0);

            this.lblCardTitle.Text      = "Tunnel Status";
            this.lblCardTitle.Font      = new System.Drawing.Font("Segoe UI Semibold", 10f, System.Drawing.FontStyle.Bold);
            this.lblCardTitle.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblCardTitle.Location  = new System.Drawing.Point(16, 6);
            this.lblCardTitle.Size      = new System.Drawing.Size(200, 20);
            this.lblCardTitle.BackColor = System.Drawing.Color.Transparent;

            this.tblStatus.Location    = new System.Drawing.Point(16, 30);
            this.tblStatus.Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.tblStatus.Size        = new System.Drawing.Size(200, 68);
            this.tblStatus.BackColor   = System.Drawing.Color.Transparent;
            this.tblStatus.ColumnCount = 4;
            this.tblStatus.RowCount    = 2;
            this.tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,  90));
            this.tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
            this.tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,  90));
            this.tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
            this.tblStatus.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            this.tblStatus.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            System.Action<Label, string, bool> styleLabel = (lbl, text, isKey) => {
                lbl.Text      = text;
                lbl.Font      = new System.Drawing.Font("Segoe UI", 8.5f);
                lbl.ForeColor = isKey ? System.Drawing.Color.FromArgb(100, 116, 139) : System.Drawing.Color.FromArgb(15, 23, 42);
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

            this.lblService.Dock      = DockStyle.Fill;
            this.lblService.BackColor = System.Drawing.Color.Transparent;
            this.lblService.Cursor    = Cursors.Help;
            this.lblRemoteStatus.Dock      = DockStyle.Fill;
            this.lblRemoteStatus.BackColor = System.Drawing.Color.Transparent;
            this.lblRemoteStatus.Cursor    = Cursors.Help;

            this.tblStatus.Controls.Add(this.lblIdLabel,      0, 0);
            this.tblStatus.Controls.Add(this.lblTunnelId,     1, 0);
            this.tblStatus.Controls.Add(this.lblServiceLabel, 2, 0);
            this.tblStatus.Controls.Add(this.lblService,      3, 0);
            this.tblStatus.Controls.Add(this.lblNameLabel,    0, 1);
            this.tblStatus.Controls.Add(this.lblTunnelName,   1, 1);
            this.tblStatus.Controls.Add(this.lblRemoteLabel,  2, 1);
            this.tblStatus.Controls.Add(this.lblRemoteStatus, 3, 1);

            // ── Ingress card ── Fix #4: right padding inside rounded corners
            this.pnlIngressCard.Dock    = DockStyle.Fill;
            this.pnlIngressCard.Margin  = new Padding(0, 0, 0, 8);
            this.pnlIngressCard.Padding = new Padding(12, 10, 12, 10);
            this.pnlIngressCard.Controls.Add(this.lblIngressTitle);
            this.pnlIngressCard.Controls.Add(this.dgvIngress);
            this.tblMain.Controls.Add(this.pnlIngressCard, 0, 1);

            // Fix #10: "Routes" instead of "Published Routes"
            this.lblIngressTitle.Text      = "Routes";
            this.lblIngressTitle.Font      = new System.Drawing.Font("Segoe UI Semibold", 10f, System.Drawing.FontStyle.Bold);
            this.lblIngressTitle.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblIngressTitle.Location  = new System.Drawing.Point(12, 8);
            this.lblIngressTitle.Size      = new System.Drawing.Size(200, 20);
            this.lblIngressTitle.BackColor = System.Drawing.Color.Transparent;

            this.colCloud.HeaderText   = "Cloud Endpoint";
            this.colCloud.Name         = "colCloud";
            this.colCloud.Width        = 390;
            this.colCloud.MinimumWidth = 200;
            this.colCloud.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            this.colCloud.ReadOnly     = true;

            this.colLocal.HeaderText   = "Local Endpoint";
            this.colLocal.Name         = "colLocal";
            this.colLocal.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.colLocal.ReadOnly     = true;

            this.dgvIngress.Location   = new System.Drawing.Point(12, 32);
            this.dgvIngress.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvIngress.Size       = new System.Drawing.Size(200, 80);
            this.dgvIngress.Font       = new System.Drawing.Font("Segoe UI", 8.5f);
            this.dgvIngress.EnableHeadersVisualStyles   = false;
            this.dgvIngress.ColumnHeadersBorderStyle    = DataGridViewHeaderBorderStyle.Single;
            this.dgvIngress.ColumnHeadersHeight         = 26;
            this.dgvIngress.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvIngress.ColumnHeadersDefaultCellStyle.BackColor          = System.Drawing.Color.FromArgb(237, 233, 254);
            this.dgvIngress.ColumnHeadersDefaultCellStyle.ForeColor          = System.Drawing.Color.FromArgb(76, 29, 149);
            this.dgvIngress.ColumnHeadersDefaultCellStyle.SelectionBackColor = this.dgvIngress.ColumnHeadersDefaultCellStyle.BackColor;
            this.dgvIngress.ColumnHeadersDefaultCellStyle.SelectionForeColor = this.dgvIngress.ColumnHeadersDefaultCellStyle.ForeColor;
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

            // ── Token card ── Fix #5: BorderPanel with rounded purple border
            this.pnlTokenCard.Dock   = DockStyle.Fill;
            this.pnlTokenCard.Margin = new Padding(0, 0, 0, 8);

            this.lblTokenTitle.Text      = "Cloudflare API Token";
            this.lblTokenTitle.Font      = new System.Drawing.Font("Segoe UI Semibold", 9f, System.Drawing.FontStyle.Bold);
            this.lblTokenTitle.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblTokenTitle.Location  = new System.Drawing.Point(14, 6);
            this.lblTokenTitle.Size      = new System.Drawing.Size(175, 18);
            this.lblTokenTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblTokenTitle.Cursor    = Cursors.Help;
            this.toolTip.SetToolTip(this.lblTokenTitle, "Found in LastPass or the HubSpot Company Record under Network & Environment");

            // BorderPanel wraps the token textbox for rounded purple look
            this.tokenBorder.Location = new System.Drawing.Point(14, 26);
            this.tokenBorder.Size     = new System.Drawing.Size(490, 30);
            this.tokenBorder.Anchor   = AnchorStyles.Top | AnchorStyles.Left;

            this.txtApiToken.Location              = new System.Drawing.Point(4, 3);
            this.txtApiToken.Size                  = new System.Drawing.Size(480, 22);
            this.txtApiToken.UseSystemPasswordChar = true;
            this.txtApiToken.Font                  = new System.Drawing.Font("Cascadia Mono", 8.5f);
            this.txtApiToken.BorderStyle           = BorderStyle.None;
            this.txtApiToken.BackColor             = System.Drawing.Color.FromArgb(245, 243, 255);
            this.tokenBorder.Controls.Add(this.txtApiToken);

            this.btnTestToken.Text     = "Test Token";
            this.btnTestToken.Location = new System.Drawing.Point(516, 24);
            this.btnTestToken.Size     = new System.Drawing.Size(106, 30);
            this.btnTestToken.Click   += new EventHandler(this.btnTestToken_Click);

            this.pnlTokenCard.Controls.Add(this.lblTokenTitle);
            this.pnlTokenCard.Controls.Add(this.tokenBorder);
            this.pnlTokenCard.Controls.Add(this.btnTestToken);
            this.tblMain.Controls.Add(this.pnlTokenCard, 0, 2);

            // ── Log card ───────────────────────────────────────────────────
            this.pnlLogCard.Dock    = DockStyle.Fill;
            this.pnlLogCard.Margin  = new Padding(0, 0, 0, 0);
            this.pnlLogCard.Padding = new Padding(12, 30, 12, 12);
            this.pnlLogCard.Controls.Add(this.lblLogTitle);
            this.pnlLogCard.Controls.Add(this.txtLog);
            this.tblMain.Controls.Add(this.pnlLogCard, 0, 3);

            this.lblLogTitle.Text      = "Activity Log";
            this.lblLogTitle.Font      = new System.Drawing.Font("Segoe UI Semibold", 10f, System.Drawing.FontStyle.Bold);
            this.lblLogTitle.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblLogTitle.Location  = new System.Drawing.Point(16, 6);
            this.lblLogTitle.Size      = new System.Drawing.Size(200, 20);
            this.lblLogTitle.BackColor = System.Drawing.Color.Transparent;

            this.txtLog.Dock        = DockStyle.Fill;
            this.txtLog.ReadOnly    = true;
            this.txtLog.ScrollBars  = RichTextBoxScrollBars.Vertical;
            this.txtLog.Font        = new System.Drawing.Font("Cascadia Mono", 8.5f);
            this.txtLog.BorderStyle = BorderStyle.None;
            this.txtLog.BackColor   = System.Drawing.Color.FromArgb(15, 23, 42);
            this.txtLog.ForeColor   = System.Drawing.Color.FromArgb(203, 213, 225);
            this.txtLog.WordWrap    = false;

            // ── Form ───────────────────────────────────────────────────────
            this.AutoScaleDimensions = new System.Drawing.SizeF(7f, 15f);
            this.AutoScaleMode       = AutoScaleMode.Font;
            this.ClientSize          = new System.Drawing.Size(1040, 700);
            this.MinimumSize         = new System.Drawing.Size(1000, 620);
            this.Controls.Add(this.contentPanel);
            this.Controls.Add(this.pnlSidebar);
            this.Name          = "MainForm";
            this.Text          = "Oolio Tunnel Monitor";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = System.Drawing.Color.FromArgb(39, 46, 63);

            this.pnlSidebar.ResumeLayout(false);
            this.contentPanel.ResumeLayout(false);
            this.tblMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)this.dgvIngress).EndInit();
            this.ResumeLayout(false);
        }

        // ── Field declarations ─────────────────────────────────────────────
        private ToolTip          toolTip;
        private Panel            pnlSidebar;
        private OolioSidebarLogo oolioLogo;
        private ModernButton     btnCreateTunnel;
        private ModernButton     btnTunnelStatus;
        private ModernButton     btnRepair;
        private ModernButton     btnCheckUpdates;
        private Label            lblVersion;
        private ContentPanel     contentPanel;
        private TableLayoutPanel tblMain;
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
        private RoundedPanel     pnlTokenCard;
        private Label            lblTokenTitle;
        private BorderPanel      tokenBorder;
        private TextBox          txtApiToken;
        private PillButton       btnTestToken;
        private RoundedPanel     pnlLogCard;
        private Label            lblLogTitle;
        private RichTextBox      txtLog;
    }
}
