using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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
        ///  Loads an embedded PNG resource, scales to the target size, and
        ///  returns an Icon.  No transparency manipulation - images are used as-is.
        /// </summary>
        private static Icon LoadIconFromResource(string resourceName, int size)
        {
            try
            {
                var asm    = System.Reflection.Assembly.GetExecutingAssembly();
                var stream = asm.GetManifestResourceStream(resourceName);
                if (stream == null) return FallbackIcon(size);

                using var src    = new Bitmap(stream);
                var       scaled = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(scaled))
                {
                    g.SmoothingMode     = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode   = PixelOffsetMode.HighQuality;
                    g.Clear(Color.Transparent);
                    g.DrawImage(src, new Rectangle(0, 0, size, size));
                }
                return Icon.FromHandle(scaled.GetHicon());
            }
            catch
            {
                return FallbackIcon(size);
            }
        }

        private static Icon FallbackIcon(int size)
        {
            using var bmp   = new Bitmap(size, size);
            using var g     = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(Color.FromArgb(103, 58, 182));
            g.FillEllipse(brush, 1, 1, size - 2, size - 2);
            using var font = new Font("Segoe UI", size * 0.45f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var wb   = new SolidBrush(Color.White);
            g.DrawString("O", font, wb, new RectangleF(0, 0, size, size),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            return Icon.FromHandle(bmp.GetHicon());
        }

        // ── Fields ────────────────────────────────────────────────────────────
        private readonly NotifyIcon _trayIcon;
        private readonly MainForm   _mainForm;
        private readonly Icon       _trayAppIcon;  // system tray (16px)
        private readonly Icon       _taskbarIcon;  // taskbar + title bar (32px)

        public TrayAppContext(bool startMinimised = false)
        {
            // SystemTray image: dark, high-contrast, reads well at 16px
            _trayAppIcon = LoadIconFromResource("CloudflaredMonitor.Resources.IconSystemTray.png", 16);
            // Taskbar image: richer detail, reads well at 32px
            _taskbarIcon = LoadIconFromResource("CloudflaredMonitor.Resources.IconTaskbar.png",   32);

            _mainForm      = new MainForm();
            _mainForm.Icon = _taskbarIcon;

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
                Icon             = _trayAppIcon,
                Visible          = true,
                ContextMenuStrip = contextMenu
            };
            _trayIcon.DoubleClick += (_, _) => ShowMainForm();

            var evt = new EventWaitHandle(false, EventResetMode.AutoReset, "CloudflaredMonitor_ShowWindow");
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
            _trayAppIcon.Dispose();
            _taskbarIcon.Dispose();
            _mainForm.Dispose();
            base.ExitThreadCore();
        }
    }
}
