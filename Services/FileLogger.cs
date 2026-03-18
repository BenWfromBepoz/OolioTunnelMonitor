using System;
using System.IO;
using System.Text;

namespace CloudflaredMonitor.Services
{
    /// <summary>
    ///  Rolling daily log file stored under ProgramData.
    ///  One file per day named tool-yy-mm-dd.log.
    ///  Timestamp format on each line: yy-mm-dd:hh-mm-ss (24-hr local time).
    ///  All activity is automatically written here - no manual export needed.
    /// </summary>
    internal sealed class FileLogger
    {
        private readonly string _logDir;
        private readonly object _lock = new object();

        public FileLogger()
        {
            _logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Bepoz", "CloudflaredMonitor", "logs");
            Directory.CreateDirectory(_logDir);
        }

        public string LogDirectory => _logDir;

        // Current log file path - recalculated each call so it rolls over at midnight
        public string LogFilePath =>
            Path.Combine(_logDir, $"tool-{DateTime.Now:yy-MM-dd}.log");

        public void Info(string message)  => Write("INFO",  message);
        public void Warn(string message)  => Write("WARN",  message);
        public void Error(string message) => Write("ERROR", message);
        public void Error(string message, Exception ex) => Write("ERROR", message + Environment.NewLine + ex.ToString());

        private void Write(string level, string message)
        {
            // Format: yy-mm-dd:hh-mm-ss [LEVEL] message
            var ts   = DateTime.Now.ToString("yy-MM-dd:HH-mm-ss");
            var line = $"{ts} [{level}] {message}{Environment.NewLine}";
            lock (_lock)
            {
                File.AppendAllText(LogFilePath, line, Encoding.UTF8);
            }
        }
    }
}
