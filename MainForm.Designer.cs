using System;
using System.Drawing;
using System.Windows.Forms;

namespace CloudflaredMonitor
{
    public partial class MainForm : Form
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null!
;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblServiceLabel = new Label();
            this.lblService = new Label();
            this.lblNameLabel = new Label();
            this.lblTunnelName = new Label();
            this.lblIdLabel = new Label();
            this.lblTunnelId = new Label();
            this.lblRemoteLabel = new Label();
            this.lblRemoteStatus = new Label();
            this.btnRefresh = new Button();
            this.btnRepair = new Button();
            this.btnExport = new Button();
            this.chkReinstall = new CheckBox();
            this.lstIngress = new ListBox();
            this.txtLog = new TextBox();
            this.SuspendLayout();
            // 
            // lblServiceLabel
            // 
            this.lblServiceLabel.AutoSize = true;
            this.lblServiceLabel.Location = new Point(12, 15);
            this.lblServiceLabel.Name = "lblServiceLabel";
            this.lblServiceLabel.Size = new Size(45, 15);
            this.lblServiceLabel.TabIndex = 0;
            this.lblServiceLabel.Text = "Service:";
            // 
            // lblService
            // 
            this.lblService.AutoSize = true;
            this.lblService.Location = new Point(120, 15);
            this.lblService.Name = "lblService";
            this.lblService.Size = new Size(12, 15);
            this.lblService.TabIndex = 1;
            this.lblService.Text = "-";
            // 
            // lblNameLabel
            // 
            this.lblNameLabel.AutoSize = true;
            this.lblNameLabel.Location = new Point(12, 40);
            this.lblNameLabel.Name = "lblNameLabel";
            this.lblNameLabel.Size = new Size(80, 15);
            this.lblNameLabel.TabIndex = 2;
            this.lblNameLabel.Text = "Tunnel Name:";
            // 
            // lblTunnelName
            // 
            this.lblTunnelName.AutoSize = true;
            this.lblTunnelName.Location = new Point(120, 40);
            this.lblTunnelName.Name = "lblTunnelName";
            this.lblTunnelName.Size = new Size(12, 15);
            this.lblTunnelName.TabIndex = 3;
            this.lblTunnelName.Text = "-";
            // 
            // lblIdLabel
            // 
            this.lblIdLabel.AutoSize = true;
            this.lblIdLabel.Location = new Point(12, 65);
            this.lblIdLabel.Name = "lblIdLabel";
            this.lblIdLabel.Size = new Size(58, 15);
            this.lblIdLabel.TabIndex = 4;
            this.lblIdLabel.Text = "Tunnel ID:";
            // 
            // lblTunnelId
            // 
            this.lblTunnelId.AutoSize = true;
            this.lblTunnelId.Location = new Point(120, 65);
            this.lblTunnelId.Name = "lblTunnelId";
            this.lblTunnelId.Size = new Size(12, 15);
            this.lblTunnelId.TabIndex = 5;
            this.lblTunnelId.Text = "-";
            // 
            // lblRemoteLabel
            // 
            this.lblRemoteLabel.AutoSize = true;
            this.lblRemoteLabel.Location = new Point(12, 90);
            this.lblRemoteLabel.Name = "lblRemoteLabel";
            this.lblRemoteLabel.Size = new Size(88, 15);
            this.lblRemoteLabel.TabIndex = 6;
            this.lblRemoteLabel.Text = "Remote Status:";
            // 
            // lblRemoteStatus
            // 
            this.lblRemoteStatus.AutoSize = true;
            this.lblRemoteStatus.Location = new Point(120, 90);
            this.lblRemoteStatus.Name = "lblRemoteStatus";
            this.lblRemoteStatus.Size = new Size(12, 15);
            this.lblRemoteStatus.TabIndex = 7;
            this.lblRemoteStatus.Text = "-";
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new Point(12, 120);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new Size(120, 27);
            this.btnRefresh.TabIndex = 8;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new EventHandler(this.btnRefresh_Click);
            // 
            // btnRepair
            // 
            this.btnRepair.Location = new Point(142, 120);
            this.btnRepair.Name = "btnRepair";
            this.btnRepair.Size = new Size(180, 27);
            this.btnRepair.TabIndex = 9;
            this.btnRepair.Text = "Repair Existing Tunnel";
            this.btnRepair.UseVisualStyleBackColor = true;
            this.btnRepair.Click += new EventHandler(this.btnRepair_Click);
            // 
            // btnExport
            // 
            this.btnExport.Location = new Point(328, 120);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new Size(150, 27);
            this.btnExport.TabIndex = 10;
            this.btnExport.Text = "Export Diagnostics";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new EventHandler(this.btnExport_Click);
            // 
            // chkReinstall
            // 
            this.chkReinstall.AutoSize = true;
            this.chkReinstall.Location = new Point(490, 125);
            this.chkReinstall.Name = "chkReinstall";
            this.chkReinstall.Size = new Size(155, 19);
            this.chkReinstall.TabIndex = 11;
            this.chkReinstall.Text = "Reinstall MSI (latest)";
            this.chkReinstall.UseVisualStyleBackColor = true;
            this.chkReinstall.Checked = true;
            // 
            // lstIngress
            // 
            this.lstIngress.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.lstIngress.FormattingEnabled = true;
            this.lstIngress.ItemHeight = 14;
            this.lstIngress.Location = new Point(12, 160);
            this.lstIngress.Name = "lstIngress";
            this.lstIngress.Size = new Size(760, 160);
            this.lstIngress.TabIndex = 12;
            // 
            // txtLog
            // 
            this.txtLog.Location = new Point(12, 335);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = ScrollBars.Vertical;
            this.txtLog.Size = new Size(760, 160);
            this.txtLog.TabIndex = 13;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(784, 511);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.lstIngress);
            this.Controls.Add(this.chkReinstall);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.btnRepair);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.lblRemoteStatus);
            this.Controls.Add(this.lblRemoteLabel);
            this.Controls.Add(this.lblTunnelId);
            this.Controls.Add(this.lblIdLabel);
            this.Controls.Add(this.lblTunnelName);
            this.Controls.Add(this.lblNameLabel);
            this.Controls.Add(this.lblService);
            this.Controls.Add(this.lblServiceLabel);
            this.Name = "MainForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Cloudflared Monitor";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Label lblServiceLabel;
        private Label lblService;
        private Label lblNameLabel;
        private Label lblTunnelName;
        private Label lblIdLabel;
        private Label lblTunnelId;
        private Label lblRemoteLabel;
        private Label lblRemoteStatus;
        private Button btnRefresh;
        private Button btnRepair;
        private Button btnExport;
        private CheckBox chkReinstall;
        private ListBox lstIngress;
        private TextBox txtLog;
    }
}