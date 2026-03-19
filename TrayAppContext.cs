using System;
using System.Windows.Forms;

namespace CloudflaredMonitor
{
    internal sealed class TrayAppContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly MainForm   _mainForm;

        public TrayAppContext()
        {
            _mainForm = new MainForm();

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Open",                null, (_, _) => ShowMainForm());
            contextMenu.Items.Add("Check Tunnel Status", null, async (_, _) => await _mainForm.CheckTunnelStatusAsync());
            contextMenu.Items.Add("Repair Tunnel",       null, async (_, _) => await _mainForm.RepairAsync());
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit",                null, (_, _) => ExitThread());

            _trayIcon = new NotifyIcon
            {
                Text             = "Oolio ZeroTrust Tunnel Monitor",
                Icon             = System.Drawing.SystemIcons.Shield,
                Visible          = true,
                ContextMenuStrip = contextMenu
            };

            _trayIcon.DoubleClick += (_, _) => ShowMainForm();
            _ = _mainForm.CheckTunnelStatusAsync();
        }

        private void ShowMainForm()
        {
            if (_mainForm.Visible)
            {
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
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _mainForm.Dispose();
            base.ExitThreadCore();
        }
    }
}
