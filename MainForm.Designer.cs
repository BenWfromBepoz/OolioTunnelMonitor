using System;
using System.Drawing;
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
            this.pnlSidebar      = new Panel();
            this.lblAppTitle     = new Label();
            this.lblAppSubtitle  = new Label();
            this.btnRefresh      = new ModernButton();
            this.btnRepair       = new ModernButton();
            this.btnExport       = new ModernButton();
            this.chkReinstall    = new CheckBox();
            this.pnlMain         = new Panel();
            this.pnlStatusCard   = new RoundedPanel();
            this.lblCardTitle    = new Label();
            this.tblStatus       = new TableLayoutPanel();
            this.lblServiceLabel = new Label();
            this.lblService      = new Label();
            this.lblNameLabel    = new Label();
            this.lblTunnelName   = new Label();
            this.lblIdLabel      = new Label();
            this.lblTunnelId     = new Label();
            this.lblRemoteLabel  = new Label();
            this.lblRemoteStatus = new Label();
            this.pnlIngressCard  = new RoundedPanel();
            this.lblIngressTitle = new Label();
            this.lstIngress      = new ListBox();
            this.pnlLogCard      = new RoundedPanel();
            this.lblLogTitle     = new Label();
            this.txtLog          = new TextBox();
            this.pnlSidebar.SuspendLayout();
            this.pnlMain.SuspendLayout();
            this.pnlStatusCard.SuspendLayout();
            this.tblStatus.SuspendLayout();
            this.pnlIngressCard.SuspendLayout();
            this.pnlLogCard.SuspendLayout();
            this.SuspendLayout();

            // ── Sidebar ──────────────────────────────────────────
            this.pnlSidebar.BackColor = Color.FromArgb(15, 23, 42);
            this.pnlSidebar.Dock = DockStyle.Left;
            this.pnlSidebar.Width = 200;
            this.pnlSidebar.Padding = new Padding(16, 24, 16, 24);
            this.pnlSidebar.Controls.Add(this.lblAppSubtitle);
            this.pnlSidebar.Controls.Add(this.lblAppTitle);
            this.pnlSidebar.Controls.Add(this.chkReinstall);
            this.pnlSidebar.Controls.Add(this.btnExport);
            this.pnlSidebar.Controls.Add(this.btnRepair);
            this.pnlSidebar.Controls.Add(this.btnRefresh);

            this.lblAppTitle.Text = "Cloudflared";
            this.lblAppTitle.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            this.lblAppTitle.ForeColor = Color.White;
            this.lblAppTitle.Location = new Point(16, 24);
            this.lblAppTitle.Size = new Size(168, 28);

            this.lblAppSubtitle.Text = "Tunnel Monitor";
            this.lblAppSubtitle.Font = new Font("Segoe UI", 9f);
            this.lblAppSubtitle.ForeColor = Color.FromArgb(100, 116, 139);
            this.lblAppSubtitle.Location = new Point(16, 52);
            this.lblAppSubtitle.Size = new Size(168, 18);

            this.btnRefresh.Text = "⟳  Refresh";
            this.btnRefresh.Location = new Point(16, 100);
            this.btnRefresh.Size = new Size(168, 38);
            this.btnRefresh.Click += new EventHandler(this.btnRefresh_Click);

            this.btnRepair.Text = "⚙  Repair Tunnel";
            this.btnRepair.Location = new Point(16, 148);
            this.btnRepair.Size = new Size(168, 38);
            this.btnRepair.Click += new EventHandler(this.btnRepair_Click);

            this.btnExport.Text = "↓  Export Diagnostics";
            this.btnExport.Location = new Point(16, 196);
            this.btnExport.Size = new Size(168, 38);
            this.btnExport.Click += new EventHandler(this.btnExport_Click);

            this.chkReinstall.Text = "Reinstall MSI";
            this.chkReinstall.Font = new Font("Segoe UI", 9f);
            this.chkReinstall.ForeColor = Color.FromArgb(148, 163, 184);
            this.chkReinstall.Location = new Point(16, 248);
            this.chkReinstall.Size = new Size(168, 22);
            this.chkReinstall.Checked = true;
            this.chkReinstall.FlatStyle = FlatStyle.Flat;

            // ── Main panel ───────────────────────────────────────
            this.pnlMain.Dock = DockStyle.Fill;
            this.pnlMain.BackColor = Color.FromArgb(241, 245, 249);
            this.pnlMain.Padding = new Padding(20, 20, 20, 20);
            this.pnlMain.Controls.Add(this.pnlLogCard);
            this.pnlMain.Controls.Add(this.pnlIngressCard);
            this.pnlMain.Controls.Add(this.pnlStatusCard);

            // ── Status card ──────────────────────────────────────
            this.pnlStatusCard.Location = new Point(20, 20);
            this.pnlStatusCard.Size = new Size(760, 168);
            this.pnlStatusCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.pnlStatusCard.Padding = new Padding(20, 16, 20, 16);
            this.pnlStatusCard.Controls.Add(this.lblCardTitle);
            this.pnlStatusCard.Controls.Add(this.tblStatus);

            this.lblCardTitle.Text = "Tunnel Status";
            this.lblCardTitle.Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
            this.lblCardTitle.ForeColor = Color.FromArgb(15, 23, 42);
            this.lblCardTitle.Location = new Point(20, 16);
            this.lblCardTitle.Size = new Size(200, 22);

            this.tblStatus.Location = new Point(20, 44);
            this.tblStatus.Size = new Size(720, 108);
            this.tblStatus.ColumnCount = 4;
            this.tblStatus.RowCount = 2;
            this.tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            this.tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            this.tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            this.tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            this.tblStatus.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            this.tblStatus.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            Action<Label, string, bool> styleLabel = (lbl, text, isKey) => {
                lbl.Text = text;
                lbl.Font = isKey ? new Font("Segoe UI", 9f) : new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
                lbl.ForeColor = isKey ? Color.FromArgb(100,116,139) : Color.FromArgb(15,23,42);
                lbl.Dock = DockStyle.Fill;
                lbl.TextAlign = ContentAlignment.MiddleLeft;
                lbl.AutoSize = false;
            };
            styleLabel(this.lblServiceLabel, "Service", true);
            styleLabel(this.lblService, "-", false);
            styleLabel(this.lblNameLabel, "Tunnel Name", true);
            styleLabel(this.lblTunnelName, "-", false);
            styleLabel(this.lblIdLabel, "Tunnel ID", true);
            styleLabel(this.lblTunnelId, "-", false);
            styleLabel(this.lblRemoteLabel, "Remote Status", true);
            styleLabel(this.lblRemoteStatus, "-", false);

            this.tblStatus.Controls.Add(this.lblServiceLabel, 0, 0);
            this.tblStatus.Controls.Add(this.lblService, 1, 0);
            this.tblStatus.Controls.Add(this.lblNameLabel, 2, 0);
            this.tblStatus.Controls.Add(this.lblTunnelName, 3, 0);
            this.tblStatus.Controls.Add(this.lblIdLabel, 0, 1);
            this.tblStatus.Controls.Add(this.lblTunnelId, 1, 1);
            this.tblStatus.Controls.Add(this.lblRemoteLabel, 2, 1);
            this.tblStatus.Controls.Add(this.lblRemoteStatus, 3, 1);

            // ── Ingress card ─────────────────────────────────────
            this.pnlIngressCard.Location = new Point(20, 204);
            this.pnlIngressCard.Size = new Size(760, 180);
            this.pnlIngressCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.pnlIngressCard.Padding = new Padding(20, 16, 20, 16);
            this.pnlIngressCard.Controls.Add(this.lblIngressTitle);
            this.pnlIngressCard.Controls.Add(this.lstIngress);

            this.lblIngressTitle.Text = "Ingress Rules";
            this.lblIngressTitle.Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
            this.lblIngressTitle.ForeColor = Color.FromArgb(15, 23, 42);
            this.lblIngressTitle.Location = new Point(20, 16);
            this.lblIngressTitle.Size = new Size(200, 22);

            this.lstIngress.Location = new Point(20, 44);
            this.lstIngress.Size = new Size(720, 118);
            this.lstIngress.Font = new Font("Cascadia Mono", 9f);
            this.lstIngress.BorderStyle = BorderStyle.None;
            this.lstIngress.BackColor = Color.FromArgb(248, 250, 252);
            this.lstIngress.ForeColor = Color.FromArgb(30, 41, 59);
            this.lstIngress.ItemHeight = 20;

            // ── Log card ─────────────────────────────────────────
            this.pnlLogCard.Location = new Point(20, 400);
            this.pnlLogCard.Size = new Size(760, 180);
            this.pnlLogCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.pnlLogCard.Padding = new Padding(20, 16, 20, 16);
            this.pnlLogCard.Controls.Add(this.lblLogTitle);
            this.pnlLogCard.Controls.Add(this.txtLog);

            this.lblLogTitle.Text = "Activity Log";
            this.lblLogTitle.Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
            this.lblLogTitle.ForeColor = Color.FromArgb(15, 23, 42);
            this.lblLogTitle.Location = new Point(20, 16);
            this.lblLogTitle.Size = new Size(200, 22);

            this.txtLog.Location = new Point(20, 44);
            this.txtLog.Size = new Size(720, 118);
            this.txtLog.Multiline = true;
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = ScrollBars.Vertical;
            this.txtLog.Font = new Font("Cascadia Mono", 8.5f);
            this.txtLog.BorderStyle = BorderStyle.None;
            this.txtLog.BackColor = Color.FromArgb(15, 23, 42);
            this.txtLog.ForeColor = Color.FromArgb(148, 163, 184);

            // ── Form ─────────────────────────────────────────────
            this.AutoScaleDimensions = new SizeF(7f, 15f);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1000, 620);
            this.MinimumSize = new Size(860, 580);
            this.Controls.Add(this.pnlMain);
            this.Controls.Add(this.pnlSidebar);
            this.Name = "MainForm";
            this.Text = "Cloudflared Monitor";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(241, 245, 249);

            this.pnlSidebar.ResumeLayout(false);
            this.pnlMain.ResumeLayout(false);
            this.pnlStatusCard.ResumeLayout(false);
            this.tblStatus.ResumeLayout(false);
            this.pnlIngressCard.ResumeLayout(false);
            this.pnlLogCard.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private Panel pnlSidebar;
        private Label lblAppTitle;
        private Label lblAppSubtitle;
        private ModernButton btnRefresh;
        private ModernButton btnRepair;
        private ModernButton btnExport;
        private CheckBox chkReinstall;
        private Panel pnlMain;
        private RoundedPanel pnlStatusCard;
        private Label lblCardTitle;
        private TableLayoutPanel tblStatus;
        private Label lblServiceLabel;
        private Label lblService;
        private Label lblNameLabel;
        private Label lblTunnelName;
        private Label lblIdLabel;
        private Label lblTunnelId;
        private Label lblRemoteLabel;
        private Label lblRemoteStatus;
        private RoundedPanel pnlIngressCard;
        private Label lblIngressTitle;
        private ListBox lstIngress;
        private RoundedPanel pnlLogCard;
        private Label lblLogTitle;
        private TextBox txtLog;
    }
}