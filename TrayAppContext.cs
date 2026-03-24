using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace CloudflaredMonitor
{
    internal sealed class TrayAppContext : ApplicationContext
    {
        // ── DWM title-bar colouring (Windows 11 build 22000+) ────────────────
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
        private const int DWMWA_CAPTION_COLOR = 35;
        private static readonly int _captionColour = ColorToAbgr(Color.FromArgb(39, 46, 63));
        private static int ColorToAbgr(Color c) => c.R | (c.G << 8) | (c.B << 16);
        private static void ApplyCaptionColour(IntPtr hwnd)
        {
            try { int col = _captionColour; DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref col, sizeof(int)); }
            catch { }
        }

        // ── Icon loading ──────────────────────────────────────────────────────
        /// <summary>
        ///  Loads an embedded .ico and requests a specific pixel size.
        ///  Windows picks the closest match from the multi-resolution container.
        /// </summary>
        private static Icon LoadIcoFromResource(string resourceName, int size)
        {
            try
            {
                var asm    = System.Reflection.Assembly.GetExecutingAssembly();
                var stream = asm.GetManifestResourceStream(resourceName);
                if (stream != null) return new Icon(stream, size, size);
            }
            catch { }
            return FallbackIcon(size);
        }

        private static Icon FallbackIcon(int size)
        {
            using var bmp   = new System.Drawing.Bitmap(size, size);
            using var g     = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(Color.FromArgb(103, 58, 182));
            g.FillEllipse(brush, 1, 1, size - 2, size - 2);
            using var font = new Font("Segoe UI", size * 0.4f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var wb   = new SolidBrush(Color.White);
            g.DrawString("O", font, wb, new RectangleF(0, 0, size, size),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            return Icon.FromHandle(bmp.GetHicon());
        }

        // ── Fields ────────────────────────────────────────────────────────────
        private readonly NotifyIcon _trayIcon;
        private readonly MainForm   _mainForm;
        private readonly Icon       _trayIco;     // 16px - system tray
        private readonly Icon       _taskbarIco;  // 48px - taskbar + title bar

        public TrayAppContext(bool startMinimised = false)
        {
            _trayIco    = LoadIcoFromResource("CloudflaredMonitor.Resources.IconTray.ico",    16);
            _taskbarIco = LoadIcoFromResource("CloudflaredMonitor.Resources.IconTaskbar.ico", 48);

            _mainForm      = new MainForm();
            _mainForm.Icon = _taskbarIco;

            _mainForm.HandleCreated += (_, _) => ApplyCaptionColour(_mainForm.Handle);

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Open",                null, (_, _) => ShowMainForm());
            contextMenu.Items.Add("Check Tunnel Status", null, async (_, _) => await _mainForm.CheckTunnelStatusAsync());
            contextMenu.Items.Add("Repair Tunnel",       null, async (_, _) => await _mainForm.RepairAsync());
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit",                null, (_, _) => ExitThread());

            _trayIcon = new NotifyIcon
            {
                Text             = "Oolio ZeroTrust Tunnel Monitor",
                Icon             = _trayIco,
                Visible          = true,
                ContextMenuStrip = contextMenu
            };
            _trayIcon.DoubleClick += (_, _) => ShowMainForm();

            var evt = new EventWaitHandle(false, EventResetMode.AutoReset, "TunnelMonitor_ShowWindow");
            var t   = new Thread(() => { while (true) { evt.WaitOne(); ShowMainForm(); } })
            { IsBackground = true, Name = "ShowWindowListener" };
            t.Start();

            if (!startMinimised) ShowMainForm();
            else _ = _mainForm.CheckTunnelStatusAsync();
        }

        private void ShowMainForm()
        {
            if (_mainForm.InvokeRequired) { _mainForm.BeginInvoke(ShowMainForm); return; }
            if (_mainForm.Visible)
            {
                _mainForm.WindowState = FormWindowState.Normal;
                _mainForm.Activate();
            }
            else
            {
                _mainForm.Show();
                _mainForm.WindowState = FormWindowState.Normal;
                ApplyCaptionColour(_mainForm.Handle);
            }
        }

        protected override void ExitThreadCore()
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIco.Dispose();
            _taskbarIco.Dispose();
            _mainForm.Dispose();
            base.ExitThreadCore();
        }
    }
}
