using System;
using System.Windows.Forms;

namespace CloudflaredMonitor
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Configure the application to use Windows visual styles and high DPI
            ApplicationConfiguration.Initialize();
            // Run the tray application context rather than a single form.  This
            // keeps the app running even when its window is closed and exposes
            // a system tray icon for quick access.
            Application.Run(new TrayAppContext());
        }
    }
}