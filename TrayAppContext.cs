using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
        // Loads .ico without forcing a size — Windows picks the best frame.
        // Falls back to rendering from the 256x256 PNG if the .ico stream fails.
        // Uses a stream-roundtrip (Bitmap -> MemoryStream -> Icon) to create a
        // fully managed Icon that won't be GC'd (avoids broken GetHicon handles).
        private static Icon LoadIcon(string icoResourceName, int fallbackSize)
        {
            try
            {
                var asm    = System.Reflection.Assembly.GetExecutingAssembly();
                var stream = asm.GetManifestResourceStream(icoResourceName);
                if (stream != null && stream.Length > 0)
                    return new Icon(stream);
            }
            catch { }
            return GenerateIconFromPng(fallbackSize);
        }

        private static Icon GenerateIconFromPng(int size)
        {
            try
            {
                var asm    = System.Reflection.Assembly.GetExecutingAssembly();
                var stream = asm.GetManifestResourceStream("CloudflaredMonitor.Resources.OolioTaskbar256.png")
                          ?? asm.GetManifestResourceStream("CloudflaredMonitor.Resources.Oolio.png");
                if (stream != null)
                {
                    using var src = Image.FromStream(stream);
                    using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                    using var g   = Graphics.FromImage(bmp);
                    g.SmoothingMode     = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.Clear(Color.Transparent);
                    g.DrawImage(src, new Rectangle(0, 0, size, size));
                    // Stream roundtrip creates a fully managed Icon — avoids GC issues with GetHicon
                    using var ms = new MemoryStream();
                    bmp.Save(ms, ImageFormat.Png);
                    ms.Position = 0;
                    return MakeIconFromBitmap(bmp, size);
                }
            }
            catch { }
            return FallbackIcon(size);
        }

        // Wraps a bitmap as an Icon via a properly structured ICO stream
        private static Icon MakeIconFromBitmap(Bitmap bmp, int size)
        {
            using var ms = new MemoryStream();
            // ICO header
            ms.Write(new byte[] { 0, 0, 1, 0, 1, 0 }, 0, 6); // reserved, type=1, count=1
            int w = Math.Min(size, 255), h = Math.Min(size, 255);
            ms.WriteByte((byte)w);   // width
            ms.WriteByte((byte)h);   // height
            ms.WriteByte(0);         // color count (0 = >256)
            ms.WriteByte(0);         // reserved
            ms.Write(new byte[] { 1, 0 }, 0, 2); // color planes
            ms.Write(new byte[] { 32, 0 }, 0, 2); // bits per pixel
            // PNG image data
            using var imgStream = new MemoryStream();
            bmp.Save(imgStream, ImageFormat.Png);
            byte[] imgData = imgStream.ToArray();
            // Data size (4 bytes LE)
            ms.Write(BitConverter.GetBytes(imgData.Length), 0, 4);
            // Data offset = 6 (header) + 16 (dir entry) = 22
            ms.Write(BitConverter.GetBytes(22), 0, 4);
            ms.Write(imgData, 0, imgData.Length);
            ms.Position = 0;
            return new Icon(ms);
        }

        private static Icon FallbackIcon(int size)
        {
            using var bmp   = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using var g     = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var brush = new SolidBrush(Color.FromArgb(103, 58, 182));
            g.FillEllipse(brush, 1, 1, size - 2, size - 2);
            using var font = new Font("Segoe UI", size * 0.4f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var wb   = new SolidBrush(Color.White);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("O", font, wb, new RectangleF(0, 0, size, size), sf);
            return MakeIconFromBitmap(bmp, size);
        }

        // ── Fields ────────────────────────────────────────────────────────────
        private readonly NotifyIcon _trayIcon;
        private readonly MainForm   _mainForm;
        private readonly Icon       _trayIco;
        private readonly Icon       _taskbarIco;

        public TrayAppContext(bool startMinimised = false)
        {
            _trayIco    = LoadIcon("CloudflaredMonitor.Resources.IconTray.ico",    16);
            _taskbarIco = LoadIcon("CloudflaredMonitor.Resources.IconTaskbar.ico", 48);

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
            { _mainForm.WindowState = FormWindowState.Normal; _mainForm.Activate(); }
            else
            { _mainForm.Show(); _mainForm.WindowState = FormWindowState.Normal; ApplyCaptionColour(_mainForm.Handle); }
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
