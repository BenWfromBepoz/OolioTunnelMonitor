using System;
using System.IO;
using System.Text;

namespace OolioTunnelMonitor.Services
{
    /// <summary>
    /// Rolling daily log. One file per day named yymmdd_tunnelmonitor.log
    /// stored under ProgramData\Bepoz\OolioTunnelMonitor\logs.
    /// Timestamp format: yy-MM-dd | HH:mm:ss — matches the UI live log format.
    /// </summary>
    internal sealed class FileLogger
    {
        private readonly string _logDir;
        private readonly object _lock = new object();

        public FileLogger()
        {
            _logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Bepoz", "OolioTunnelMonitor", "logs");
            Directory.CreateDirectory(_logDir);
        }

        public string LogDirectory => _logDir;

        // Filename: yymmdd_tunnelmonitor.log - recalculated each write so it rolls at midnight
        public string LogFilePath =>
            Path.Combine(_logDir, $"{DateTime.Now:yyMMdd}_tunnelmonitor.log");

        public void Info(string message)  => Write("INFO",  message);
        public void Warn(string message)  => Write("WARN",  message);
        public void Error(string message) => Write("ERROR", message);
        public void Error(string message, Exception ex) => Write("ERROR", message + Environment.NewLine + ex.ToString());

        private void Write(string level, string message)
        {
            // Format matches Ts() in MainForm: "yy-MM-dd | HH:mm:ss"
            var ts   = DateTime.Now.ToString("yy-MM-dd | HH:mm:ss");
            var line = $"{ts} [{level}] {message}{Environment.NewLine}";
            lock (_lock)
            {
                File.AppendAllText(LogFilePath, line, Encoding.UTF8);
            }
        }
    }
}
