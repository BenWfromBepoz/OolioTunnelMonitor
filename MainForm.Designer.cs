using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CloudflaredMonitor
{
    public partial class MainForm : Form
    {
        private System.ComponentModel.IContainer components = null!;

        private ToolTip toolTip;
        private Panel pnlSidebar;
        private TableLayoutPanel tblMain;

        private RoundedPanel pnlStatusCard;
        private RoundedPanel pnlIngressCard;
        private RoundedPanel pnlTokenCard;
        private RoundedPanel pnlLogCard;

        private Label lblCardTitle;
        private TableLayoutPanel tblStatus;

        private Label lblIngressTitle;
        private DataGridView dgvIngress;
        private DataGridViewTextBoxColumn colCloud;
        private DataGridViewTextBoxColumn colLocal;

        private Label lblTokenTitle;
        private TextBox txtApiToken;
        private CheckBox chkShowToken;
        private Button btnTestToken;

        private Label lblLogTitle;
        private RichTextBox txtLog;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.toolTip = new ToolTip();
            this.pnlSidebar = new Panel();
            this.tblMain = new TableLayoutPanel();

            this.pnlStatusCard = new RoundedPanel();
            this.pnlIngressCard = new RoundedPanel();
            this.pnlTokenCard = new RoundedPanel();
            this.pnlLogCard = new RoundedPanel();

            this.lblCardTitle = new Label();
            this.tblStatus = new TableLayoutPanel();

            this.lblIngressTitle = new Label();
            this.dgvIngress = new DataGridView();
            this.colCloud = new DataGridViewTextBoxColumn();
            this.colLocal = new DataGridViewTextBoxColumn();

            this.lblTokenTitle = new Label();
            this.txtApiToken = new TextBox();
            this.chkShowToken = new CheckBox();
            this.btnTestToken = new Button();

            this.lblLogTitle = new Label();
            this.txtLog = new RichTextBox();

            this.SuspendLayout();

            // ── Main layout ─────────────────────────────────────────────
            this.tblMain.Dock = DockStyle.Fill;
            this.tblMain.BackColor = Color.FromArgb(226, 232, 240);
            this.tblMain.Padding = new Padding(8);
            this.tblMain.ColumnCount = 1;
            this.tblMain.RowCount = 4;

            this.tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            this.tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
            this.tblMain.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
            this.tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            this.tblMain.RowStyles.Add(new RowStyle(SizeType.Percent, 65));

            // ── Status Card ─────────────────────────────────────────────
            this.pnlStatusCard.Dock = DockStyle.Fill;
            this.pnlStatusCard.Margin = new Padding(0, 0, 0, 8);
            this.pnlStatusCard.Padding = new Padding(12, 28, 12, 12);
            this.pnlStatusCard.BackColor = Color.White;

            this.lblCardTitle.Text = "Tunnel Status";
            this.lblCardTitle.Dock = DockStyle.Top;
            this.lblCardTitle.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            this.tblStatus.Dock = DockStyle.Fill;

            this.pnlStatusCard.Controls.Add(this.tblStatus);
            this.pnlStatusCard.Controls.Add(this.lblCardTitle);

            this.tblMain.Controls.Add(this.pnlStatusCard, 0, 0);

            // ── Ingress Card ────────────────────────────────────────────
            this.pnlIngressCard.Dock = DockStyle.Fill;
            this.pnlIngressCard.Margin = new Padding(0, 0, 0, 8);
            this.pnlIngressCard.Padding = new Padding(12, 32, 12, 12);
            this.pnlIngressCard.BackColor = Color.White;

            this.lblIngressTitle.Text = "Published Routes";
            this.lblIngressTitle.Dock = DockStyle.Top;
            this.lblIngressTitle.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            this.dgvIngress.Dock = DockStyle.Fill;
            this.dgvIngress.Columns.Add(this.colCloud);
            this.dgvIngress.Columns.Add(this.colLocal);

            this.colCloud.HeaderText = "Cloud Endpoint";
            this.colCloud.Width = 300;

            this.colLocal.HeaderText = "Local Endpoint";
            this.colLocal.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            this.pnlIngressCard.Controls.Add(this.dgvIngress);
            this.pnlIngressCard.Controls.Add(this.lblIngressTitle);

            this.tblMain.Controls.Add(this.pnlIngressCard, 0, 1);

            // ── Token Card ──────────────────────────────────────────────
            this.pnlTokenCard.Dock = DockStyle.Fill;
            this.pnlTokenCard.Margin = new Padding(0, 0, 0, 8);
            this.pnlTokenCard.Padding = new Padding(12, 28, 12, 12);
            this.pnlTokenCard.BackColor = Color.White;

            this.lblTokenTitle.Text = "API Token";
            this.lblTokenTitle.Dock = DockStyle.Top;

            this.txtApiToken.Dock = DockStyle.Top;

            this.btnTestToken.Text = "Test";
            this.btnTestToken.Dock = DockStyle.Top;

            this.pnlTokenCard.Controls.Add(this.btnTestToken);
            this.pnlTokenCard.Controls.Add(this.txtApiToken);
            this.pnlTokenCard.Controls.Add(this.lblTokenTitle);

            this.tblMain.Controls.Add(this.pnlTokenCard, 0, 2);

            // ── Log Card ────────────────────────────────────────────────
            this.pnlLogCard.Dock = DockStyle.Fill;
            this.pnlLogCard.Margin = new Padding(0);
            this.pnlLogCard.Padding = new Padding(12, 28, 12, 12);
            this.pnlLogCard.BackColor = Color.White;

            this.lblLogTitle.Text = "Log";
            this.lblLogTitle.Dock = DockStyle.Top;

            this.txtLog.Dock = DockStyle.Fill;

            this.pnlLogCard.Controls.Add(this.txtLog);
            this.pnlLogCard.Controls.Add(this.lblLogTitle);

            this.tblMain.Controls.Add(this.pnlLogCard, 0, 3);

            // ── Form ────────────────────────────────────────────────────
            this.ClientSize = new Size(1000, 700);
            this.Controls.Add(this.tblMain);
            this.Text = "Monitor";

            this.ResumeLayout(false);
        }
    }
}
