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
