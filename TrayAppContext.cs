using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;

namespace CloudflaredMonitor
{
    internal sealed class TrayAppContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly MainForm   _mainForm;
        private readonly Thread     _showWindowThread;

        public TrayAppContext(bool startMinimised = false)
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
                Icon             = CreateTrayIcon(),
                Visible          = true,
                ContextMenuStrip = contextMenu
            };
            _trayIcon.DoubleClick += (_, _) => ShowMainForm();

            if (!startMinimised)
                ShowMainForm();

            // Background thread listens for a second instance signalling us to show
            _showWindowThread = new Thread(() =>
            {
                try
                {
                    using var evt = new EventWaitHandle(false, EventResetMode.AutoReset, "CloudflaredMonitor_ShowWindow");
                    while (true)
                    {
                        evt.WaitOne();
                        if (_mainForm.IsDisposed) break;
                        _mainForm.BeginInvoke(ShowMainForm);
                    }
                }
                catch { }
            }) { IsBackground = true, Name = "ShowWindowListener" };
            _showWindowThread.Start();
        }

        private static System.Drawing.Icon CreateTrayIcon()
        {
            using var bmp = new System.Drawing.Bitmap(32, 32);
            using var g   = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(39, 46, 63));
            using var pen = new Pen(Color.White, 3f);
            g.DrawEllipse(pen, 2,  9, 14, 14);
            g.DrawEllipse(pen, 16, 9, 14, 14);
            return System.Drawing.Icon.FromHandle(bmp.GetHicon());
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
                _mainForm.Activate();
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
