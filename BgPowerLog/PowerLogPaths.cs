using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BgPowerLog
{
    public sealed class LogFileInfo
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public string SessionFolder { get; set; }
        public long SizeBytes { get; set; }
        public DateTime LastWriteUtc { get; set; }

        public string DisplayLabel
        {
            get
            {
                var mb = SizeBytes / (1024.0 * 1024.0);
                var session = string.IsNullOrEmpty(SessionFolder) ? "" : $" [{SessionFolder}]";
                return $"{Name} ({mb:F1} MB){session}";
            }
        }
    }

    public static class PowerLogPaths
    {
        private static readonly string[] LogFileNames =
        {
            "Power.log",
            "Power_old.log",
            "Zone.log",
            "LoadingScreen.log",
            "LoadingScreen_old.log",
            "Hearthstone.log"
        };

        public static string DefaultPowerLogPath =>
            DiscoverLogFiles()
                .Where(x => x.Name.StartsWith("Power", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.SizeBytes)
                .FirstOrDefault()?.Path
            ?? DiscoverLogFiles().FirstOrDefault()?.Path;

        public static bool Exists(string path = null)
        {
            if (!string.IsNullOrWhiteSpace(path))
                return File.Exists(path);
            return DiscoverLogFiles().Count > 0;
        }

        /// <summary>
        /// Scans AppData Logs, install Logs folder, and optional custom root (recursive session folders).
        /// </summary>
        public static IReadOnlyList<LogFileInfo> DiscoverLogFiles(string customRoot = null)
        {
            var results = new List<LogFileInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var roots = new List<string>();
            var saved = string.IsNullOrWhiteSpace(customRoot) ? ReplayLogSettings.CustomLogRoot : customRoot.Trim();
            if (!string.IsNullOrWhiteSpace(saved) && Directory.Exists(saved))
                roots.Add(saved);

            foreach (var dir in GetCandidateLogDirectories())
            {
                if (Directory.Exists(dir) && !roots.Contains(dir, StringComparer.OrdinalIgnoreCase))
                    roots.Add(dir);
            }

            foreach (var root in roots)
                ScanDirectory(root, root, results, seen, recursive: true);

            return results
                .OrderByDescending(x => x.Name.StartsWith("Power", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(x => x.LastWriteUtc)
                .ToList();
        }

        private static void ScanDirectory(
            string root,
            string currentDir,
            List<LogFileInfo> results,
            HashSet<string> seen,
            bool recursive)
        {
            if (!Directory.Exists(currentDir))
                return;

            foreach (var name in LogFileNames)
            {
                var path = Path.Combine(currentDir, name);
                if (!File.Exists(path) || !seen.Add(path))
                    continue;

                var info = new FileInfo(path);
                var session = GetSessionFolderLabel(root, currentDir);
                results.Add(new LogFileInfo
                {
                    Path = path,
                    Name = name,
                    SessionFolder = session,
                    SizeBytes = info.Length,
                    LastWriteUtc = info.LastWriteTimeUtc
                });
            }

            if (!recursive)
                return;

            try
            {
                foreach (var sub in Directory.GetDirectories(currentDir))
                    ScanDirectory(root, sub, results, seen, recursive: true);
            }
            catch
            {
                // access denied etc.
            }
        }

        private static string GetSessionFolderLabel(string root, string fileDir)
        {
            if (string.Equals(root, fileDir, StringComparison.OrdinalIgnoreCase))
                return "(root)";

            var rel = fileDir.Substring(root.Length).Trim('\\', '/');
            var first = rel.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return first ?? rel;
        }

        public static IEnumerable<string> GetCandidateLogDirectories()
        {
            yield return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Blizzard", "Hearthstone", "Logs");

            if (Directory.Exists(ReplayLogSettings.DefaultInstallLogsRoot))
                yield return ReplayLogSettings.DefaultInstallLogsRoot;

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!string.IsNullOrEmpty(programFiles))
            {
                var path = Path.Combine(programFiles, "Hearthstone", "Logs");
                if (Directory.Exists(path))
                    yield return path;
            }
        }
    }
}
