using System;
using System.Threading;
using System.Windows.Forms;

namespace CloudflaredMonitor
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Top-level exception handler - shows message box instead of silent crash
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) =>
                MessageBox.Show("Unhandled error: " + e.Exception.Message + "\n\n" + e.Exception.StackTrace,
                    "TunnelMonitor Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                MessageBox.Show("Fatal error: " + (e.ExceptionObject?.ToString() ?? "unknown"),
                    "TunnelMonitor Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            bool createdNew;
            using var mutex = new Mutex(true, "TunnelMonitor_SingleInstance", out createdNew);
            if (!createdNew)
            {
                try
                {
                    using var evt = EventWaitHandle.OpenExisting("TunnelMonitor_ShowWindow");
                    evt.Set();
                }
                catch { }
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new TrayAppContext(startMinimised: true));
        }
    }
}
