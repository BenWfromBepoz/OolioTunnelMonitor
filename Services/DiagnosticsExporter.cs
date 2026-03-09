using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace CloudflaredMonitor.Services
{
    /// <summary>
    ///  Produces a zip file containing diagnostic artefacts such as the
    ///  current status, ingress mappings, UI log lines and the persistent
    ///  log file.  The exported zip is stored in ProgramData so it can be
    ///  easily shared with support.
    /// </summary>
    internal sealed class DiagnosticsExporter
    {
        private readonly FileLogger _logger;

        public DiagnosticsExporter(FileLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        ///  Create a diagnostics bundle and return the path to the zip file.
        /// </summary>
        /// <param name="status">The current tunnel/service status model.</param>
        /// <param name="uiLogLines">The list of lines written to the UI log.</param>
        /// <param name="ingressLines">The ingress mapping lines displayed in the UI.</param>
        public string Export(TunnelServiceStatus status, IList<string> uiLogLines, IList<string> ingressLines)
        {
            // Create output directories under ProgramData
            var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Bepoz", "CloudflaredMonitor", "diagnostics");
            Directory.CreateDirectory(baseDir);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
            var workDir = Path.Combine(baseDir, $"diag-{timestamp}");
            Directory.CreateDirectory(workDir);

            // Write status file
            var statusPath = Path.Combine(workDir, "service-status.txt");
            var sb = new StringBuilder();
            sb.AppendLine($"ServiceState: {status.ServiceState}");
            sb.AppendLine($"TunnelId: {status.TunnelId}");
            sb.AppendLine($"TunnelName: {status.TunnelName}");
            sb.AppendLine($"RemoteStatus: {status.RemoteStatus}");
            sb.AppendLine($"DiagnosticsNote: {status.DiagnosticsNote}");
            File.WriteAllText(statusPath, sb.ToString(), Encoding.UTF8);

            // Write ingress file
            File.WriteAllLines(Path.Combine(workDir, "ingress.txt"), ingressLines, Encoding.UTF8);

            // Write UI log file
            File.WriteAllLines(Path.Combine(workDir, "ui-log.txt"), uiLogLines, Encoding.UTF8);

            // Copy persistent log file if it exists
            var logFile = _logger.LogFilePath;
            if (File.Exists(logFile))
            {
                var destLog = Path.Combine(workDir, Path.GetFileName(logFile));
                File.Copy(logFile, destLog, true);
            }

            // Zip the diagnostics directory
            var zipPath = Path.Combine(baseDir, $"diag-{timestamp}.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(workDir, zipPath);

            return zipPath;
        }
    }
}