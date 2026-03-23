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
        ///  Loads an embedded PNG resource, makes outer white/near-white pixels
        ///  transparent, scales to the target size, and returns an Icon.
        ///  The tolerance parameter controls how aggressively near-white fringe
        ///  pixels are removed (0 = exact white only, 30 = generous anti-alias removal).
        /// </summary>
        private static Icon LoadIconFromResource(string resourceName, int size, int tolerance = 30)
        {
            try
            {
                var asm    = System.Reflection.Assembly.GetExecutingAssembly();
                var stream = asm.GetManifestResourceStream(resourceName);
                if (stream == null) return FallbackIcon(size);

                using var src = new Bitmap(stream);

                // Make white/near-white pixels transparent using tolerance
                MakeNearWhiteTransparent(src, tolerance);

                // Scale to target size with high quality
                var scaled = new Bitmap(size, size, PixelFormat.Format32bppArgb);
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

        /// <summary>
        ///  Iterates every pixel in the bitmap and sets near-white pixels to transparent.
        ///  Uses unsafe pointer access via LockBits for performance.
        /// </summary>
        private static void MakeNearWhiteTransparent(Bitmap bmp, int tolerance)
        {
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                    ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                int bytes  = Math.Abs(data.Stride) * bmp.Height;
                var pixels = new byte[bytes];
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, pixels, 0, bytes);

                int threshold = 255 - tolerance;
                for (int i = 0; i < bytes; i += 4)
                {
                    byte b = pixels[i];     // blue
                    byte g = pixels[i + 1]; // green
                    byte r = pixels[i + 2]; // red
                    // byte a = pixels[i + 3]; // alpha (unused here)

                    // If pixel is near-white on all channels, make transparent
                    if (r >= threshold && g >= threshold && b >= threshold)
                    {
                        pixels[i]     = 0; // B
                        pixels[i + 1] = 0; // G
                        pixels[i + 2] = 0; // R
                        pixels[i + 3] = 0; // A = fully transparent
                    }
                }

                System.Runtime.InteropServices.Marshal.Copy(pixels, 0, data.Scan0, bytes);
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        private static Icon FallbackIcon(int size)
        {
            using var bmp  = new Bitmap(size, size);
            using var g    = Graphics.FromImage(bmp);
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
        private readonly Icon       _trayAppIcon;   // system tray + title bar
        private readonly Icon       _taskbarIcon;   // taskbar / form icon

        public TrayAppContext(bool startMinimised = false)
        {
            // System tray icon: small 16px version of SystemTray image
            _trayAppIcon  = LoadIconFromResource("CloudflaredMonitor.Resources.IconSystemTray.png", 16, tolerance: 30);
            // Taskbar icon: larger 32px version of Taskbar image
            _taskbarIcon  = LoadIconFromResource("CloudflaredMonitor.Resources.IconTaskbar.png",   32, tolerance: 30);

            _mainForm = new MainForm();
            _mainForm.Icon = _taskbarIcon;

            // Apply DWM caption colour once handle is created
            _mainForm.HandleCreated += (_, _) => ApplyCaptionColour(_mainForm.Handle);

            // Build tray context menu
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

            // Listen for show-window signal from second instance
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
