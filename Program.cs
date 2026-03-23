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
            // Single-instance guard: if already running, bring that window to front and exit
            using var mutex = new Mutex(true, "CloudflaredMonitor_SingleInstance", out bool createdNew);
            if (!createdNew)
            {
                // Signal the existing instance to show itself via a named event
                using var evt = new EventWaitHandle(false, EventResetMode.AutoReset, "CloudflaredMonitor_ShowWindow");
                evt.Set();
                return;
            }

            ApplicationConfiguration.Initialize();

            // Start minimised to system tray - don't show the main window on launch
            var ctx = new TrayAppContext(startMinimised: true);
            Application.Run(ctx);
        }
    }
}
