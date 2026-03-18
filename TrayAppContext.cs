using System;
using System.Windows.Forms;

namespace CloudflaredMonitor
{
    /// <summary>
    /// Hosts the system tray icon. The MainForm is created on demand and
    /// recreated if it has been disposed (e.g. after a repair that closes it).
    /// This prevents the ObjectDisposedException when the user clicks the tray
    /// icon after closing the window.
    /// </summary>
    internal sealed class TrayAppContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private MainForm? _mainForm;

        public TrayAppContext()
        {
            // Build the initial form and kick off the startup refresh
            _mainForm = CreateForm();

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Open",                    null, (_, _) => ShowMainForm());
            contextMenu.Items.Add("Check Service Status",    null, async (_, _) => { EnsureForm(); await _mainForm!.RefreshStatusAsync(); });
            contextMenu.Items.Add("Check Tunnel Status",     null, async (_, _) => { EnsureForm(); await _mainForm!.CheckTunnelStatusAsync(); });
            contextMenu.Items.Add("Retrieve Tunnel Details", null, async (_, _) => { EnsureForm(); await _mainForm!.RetrieveTunnelDetailsAsync(); });
            contextMenu.Items.Add("Open Logfile Folder",     null, (_, _) => { EnsureForm(); _mainForm!.OpenLogFolder(); });
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit", null, (_, _) => ExitThread());

            _trayIcon = new NotifyIcon
            {
                Text             = "Oolio ZeroTrust Tunnel Monitor",
                Icon             = TrayIconGenerator.CreateOolioIcon(),
                Visible          = true,
                ContextMenuStrip = contextMenu
            };

            _trayIcon.DoubleClick += (_, _) => ShowMainForm();

            // Initial status check on startup
            _ = _mainForm.RefreshStatusAsync();
        }

        private MainForm CreateForm()
        {
            var form = new MainForm();
            // When the user closes the window, hide it rather than dispose it.
            // This keeps the form alive for tray interactions.
            form.FormClosing += (_, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    form.Hide();
                }
            };
            return form;
        }

        /// <summary>
        /// Ensures _mainForm is alive. If it has been disposed (shouldn't happen
        /// now with the FormClosing override, but kept as safety net) recreate it.
        /// </summary>
        private void EnsureForm()
        {
            if (_mainForm == null || _mainForm.IsDisposed)
            {
                _mainForm = CreateForm();
            }
        }

        private void ShowMainForm()
        {
            EnsureForm();
            if (_mainForm!.Visible)
            {
                _mainForm.WindowState = FormWindowState.Normal;
                _mainForm.Activate();
            }
            else
            {
                _mainForm.Show();
                _mainForm.WindowState = FormWindowState.Normal;
                _mainForm.Activate();
            }
        }

        protected override void ExitThreadCore()
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            // Allow the form to actually close now
            if (_mainForm != null && !_mainForm.IsDisposed)
            {
                _mainForm.FormClosing -= null;  // remove our cancel-close handler
                _mainForm.Dispose();
            }
            base.ExitThreadCore();
        }
    }
}
