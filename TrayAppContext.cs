using System;
using System.Windows.Forms;

namespace CloudflaredMonitor
{
    /// <summary>
    ///  Provides a system tray icon and context menu for the Cloudflared
    ///  monitoring tool.  The tray context manages the main form and allows
    ///  the user to hide/show the window or exit the application entirely.
    /// </summary>
    internal sealed class TrayAppContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly MainForm _mainForm;

        public TrayAppContext()
        {
            _mainForm = new MainForm();

            // Build the context menu for the tray icon
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Open", null, (_, _) => ShowMainForm());
            contextMenu.Items.Add("Refresh", null, async (_, _) => await _mainForm.RefreshStatusAsync());
            contextMenu.Items.Add("Repair Existing Tunnel", null, async (_, _) => await _mainForm.RepairAsync());
            contextMenu.Items.Add("Export Diagnostics", null, (_, _) => _mainForm.ExportDiagnostics());
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit", null, (_, _) => ExitThread());

            _trayIcon = new NotifyIcon
            {
                Text = "Cloudflared Monitor",
                Icon = System.Drawing.SystemIcons.Shield,
                Visible = true,
                ContextMenuStrip = contextMenu
            };

            _trayIcon.DoubleClick += (_, _) => ShowMainForm();

            // Perform an initial status refresh on startup
            _ = _mainForm.RefreshStatusAsync();
        }

        private void ShowMainForm()
        {
            if (_mainForm.Visible)
            {
                // Bring existing window to front
                _mainForm.WindowState = FormWindowState.Normal;
                _mainForm.Activate();
            }
            else
            {
                _mainForm.Show();
                _mainForm.WindowState = FormWindowState.Normal;
            }
        }

        protected override void ExitThreadCore()
        {
            // Hide the tray icon and dispose resources
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _mainForm.Dispose();
            base.ExitThreadCore();
        }
    }
}