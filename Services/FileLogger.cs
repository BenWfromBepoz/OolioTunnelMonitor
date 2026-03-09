using System;
using System.IO;
using System.Text;

namespace CloudflaredMonitor.Services
{
    /// <summary>
    ///  Simple file-based logger.  Logs are written to a directory under
    ///  ProgramData so they persist across sessions and can be collected for
    ///  support.  The log file name includes the date and a standard prefix.
    /// </summary>
    internal sealed class FileLogger
    {
        private readonly string _logDir;
        private readonly string _logFile;
        private readonly object _lock = new object();

        public FileLogger()
        {
            // Use ProgramData\Bepoz\CloudflaredMonitor\logs
            var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Bepoz", "CloudflaredMonitor", "logs");
            Directory.CreateDirectory(baseDir);
            _logDir = baseDir;
            _logFile = Path.Combine(_logDir, $"tool-{DateTime.Now:yyyy-MM-dd}.log");
        }

        public string LogDirectory => _logDir;
        public string LogFilePath => _logFile;

        public void Info(string message) => Write("INFO", message);
        public void Warn(string message) => Write("WARN", message);
        public void Error(string message) => Write("ERROR", message);
        public void Error(string message, Exception ex) => Write("ERROR", message + Environment.NewLine + ex.ToString());

        private void Write(string level, string message)
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
            lock (_lock)
            {
                File.AppendAllText(_logFile, line, Encoding.UTF8);
            }
        }
    }
}