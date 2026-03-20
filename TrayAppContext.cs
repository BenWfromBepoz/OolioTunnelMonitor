using System;
using System.Drawing;
using System.Drawing.Drawing2D;
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
                Icon             = CreateTrayIcon(),
                Visible          = true,
                ContextMenuStrip = contextMenu
            };

            _trayIcon.DoubleClick += (_, _) => ShowMainForm();
        }

        // Two white hollow circles on a dark background - matches the OO in the Oolio logo
        private static System.Drawing.Icon CreateTrayIcon()
        {
            using var bmp = new Bitmap(32, 32);
            using var g   = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Dark background matching sidebar colour
            g.Clear(Color.FromArgb(39, 46, 63));

            using var pen = new Pen(Color.White, 3f);

            // Left O: centre at (9, 16), radius 7
            g.DrawEllipse(pen, 2, 9, 14, 14);
            // Right O: centre at (23, 16), radius 7
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
