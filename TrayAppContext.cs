using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace CloudflaredMonitor
{
    /// <summary>
    ///  Provides a system tray icon and context menu.  Also applies a DWM
    ///  caption colour to tint the title bar on Windows 11.
    /// </summary>
    internal sealed class TrayAppContext : ApplicationContext
    {
        // ── DWM title-bar colouring (Windows 11 build 22000+) ────────────────
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
        private const int DWMWA_CAPTION_COLOR = 35;

        // Oolio sidebar dark colour: #272e3f
        private static readonly int _captionColour = ColorToAbgr(Color.FromArgb(39, 46, 63));

        // DWM uses 0x00BBGGRR (ABGR without alpha)
        private static int ColorToAbgr(Color c) => c.R | (c.G << 8) | (c.B << 16);

        private static void ApplyCaptionColour(IntPtr hwnd)
        {
            try
            {
                int col = _captionColour;
                DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref col, sizeof(int));
            }
            catch { /* DWM not available on older Windows */ }
        }

        // ── Icon generation ───────────────────────────────────────────────────
        /// <summary>
        ///  Build a 32×32 icon from the embedded Oolio PNG.  Falls back to a
        ///  simple purple circle with "O" if the resource is missing.
        /// </summary>
        private static Icon BuildOolioIcon()
        {
            try
            {
                // Try to load embedded Oolio PNG and render into a 32x32 icon
                var asm    = System.Reflection.Assembly.GetExecutingAssembly();
                var stream = asm.GetManifestResourceStream("CloudflaredMonitor.Resources.Oolio.png");
                if (stream != null)
                {
                    using var src = Image.FromStream(stream);
                    using var bmp = new Bitmap(32, 32);
                    using var g   = Graphics.FromImage(bmp);
                    g.SmoothingMode      = SmoothingMode.AntiAlias;
                    g.InterpolationMode  = InterpolationMode.HighQualityBicubic;
                    // Purple rounded background
                    using var bgBrush = new SolidBrush(Color.FromArgb(103, 58, 182));
                    using var bgPath  = RoundedRect(new Rectangle(0, 0, 31, 31), 6);
                    g.FillPath(bgBrush, bgPath);
                    // Draw Oolio logo centred, white-ish (just draw as-is — it has colour)
                    var logoRect = new Rectangle(2, 2, 28, 28);
                    g.DrawImage(src, logoRect);
                    return Icon.FromHandle(bmp.GetHicon());
                }
            }
            catch { }

            // Fallback: purple circle with white "O"
            using var fb  = new Bitmap(32, 32);
            using var fg  = Graphics.FromImage(fb);
            fg.SmoothingMode = SmoothingMode.AntiAlias;
            using var fb2 = new SolidBrush(Color.FromArgb(103, 58, 182));
            fg.FillEllipse(fb2, 1, 1, 30, 30);
            using var font = new Font("Segoe UI", 14f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var wb   = new SolidBrush(Color.White);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            fg.DrawString("O", font, wb, new RectangleF(0, 0, 32, 32), sf);
            return Icon.FromHandle(fb.GetHicon());
        }

        private static GraphicsPath RoundedRect(Rectangle r, int rad)
        {
            int d = rad * 2; var p = new GraphicsPath();
            p.AddArc(r.X,         r.Y,          d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
            p.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
            p.CloseFigure(); return p;
        }

        // ── Fields ────────────────────────────────────────────────────────────
        private readonly NotifyIcon _trayIcon;
        private readonly MainForm   _mainForm;
        private readonly Icon       _appIcon;

        public TrayAppContext(bool startMinimised = false)
        {
            _appIcon  = BuildOolioIcon();
            _mainForm = new MainForm();

            // Apply the icon to the main form
            _mainForm.Icon = _appIcon;

            // Apply DWM caption colour once the handle is created
            _mainForm.HandleCreated += (_, _) => ApplyCaptionColour(_mainForm.Handle);

            // Build tray context menu
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Open",                    null, (_, _) => ShowMainForm());
            contextMenu.Items.Add("Check Tunnel Status",     null, async (_, _) => await _mainForm.CheckTunnelStatusAsync());
            contextMenu.Items.Add("Repair Tunnel",           null, async (_, _) => await _mainForm.RepairAsync());
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit",                    null, (_, _) => ExitThread());

            _trayIcon = new NotifyIcon
            {
                Text             = "Oolio ZeroTrust Tunnel Monitor",
                Icon             = _appIcon,
                Visible          = true,
                ContextMenuStrip = contextMenu
            };

            _trayIcon.DoubleClick += (_, _) => ShowMainForm();

            // Listen for show-window signal from second instance
            var evt = new EventWaitHandle(false, EventResetMode.AutoReset, "CloudflaredMonitor_ShowWindow");
            var t   = new Thread(() => { while (true) { evt.WaitOne(); ShowMainForm(); } })
            { IsBackground = true, Name = "ShowWindowListener" };
            t.Start();

            if (!startMinimised)
                ShowMainForm();
            else
                _ = _mainForm.CheckTunnelStatusAsync();
        }

        private void ShowMainForm()
        {
            if (_mainForm.InvokeRequired)
            { _mainForm.BeginInvoke(ShowMainForm); return; }

            if (_mainForm.Visible)
            {
                _mainForm.WindowState = FormWindowState.Normal;
                _mainForm.Activate();
            }
            else
            {
                _mainForm.Show();
                _mainForm.WindowState = FormWindowState.Normal;
                // Re-apply caption colour each time window is shown
                ApplyCaptionColour(_mainForm.Handle);
            }
        }

        protected override void ExitThreadCore()
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _appIcon.Dispose();
            _mainForm.Dispose();
            base.ExitThreadCore();
        }
    }
}
