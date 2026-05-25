using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BgPowerLog
{
    public sealed class SessionFolderInfo
    {
        public string FolderName { get; set; }
        public string FolderPath { get; set; }
        public DateTime? SessionStart { get; set; }
        public string BestPowerLogPath { get; set; }
        public long BestPowerLogBytes { get; set; }
    }

    /// <summary>Lists Hearthstone_YYYY_MM_DD_HH_MM_SS session folders under install Logs.</summary>
    public static class SessionLogCatalog
    {
        private static readonly Regex SessionDirRegex =
            new Regex(@"^Hearthstone_(\d{4})_(\d{2})_(\d{2})_(\d{2})_(\d{2})_(\d{2})$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static IReadOnlyList<SessionFolderInfo> ListRecentSessions(string logsRoot, int daysBack = 14)
        {
            var list = new List<SessionFolderInfo>();
            if (string.IsNullOrWhiteSpace(logsRoot) || !Directory.Exists(logsRoot))
                return list;

            var cutoff = DateTime.Now.AddDays(-daysBack);

            try
            {
                foreach (var dir in Directory.GetDirectories(logsRoot))
                {
                    var name = Path.GetFileName(dir);
                    if (string.IsNullOrEmpty(name))
                        continue;

                    DateTime? started = null;
                    var m = SessionDirRegex.Match(name);
                    if (m.Success)
                    {
                        started = new DateTime(
                            int.Parse(m.Groups[1].Value),
                            int.Parse(m.Groups[2].Value),
                            int.Parse(m.Groups[3].Value),
                            int.Parse(m.Groups[4].Value),
                            int.Parse(m.Groups[5].Value),
                            int.Parse(m.Groups[6].Value),
                            DateTimeKind.Local);
                    }

                    if (started.HasValue && started.Value < cutoff)
                        continue;

                    var powerPath = FindBestPowerLog(dir);
                    if (powerPath == null)
                        continue;

                    var size = new FileInfo(powerPath).Length;
                    list.Add(new SessionFolderInfo
                    {
                        FolderName = name,
                        FolderPath = dir,
                        SessionStart = started ?? new FileInfo(powerPath).LastWriteTime,
                        BestPowerLogPath = powerPath,
                        BestPowerLogBytes = size
                    });
                }
            }
            catch
            {
                // ignored
            }

            return list
                .OrderByDescending(s => s.SessionStart ?? DateTime.MinValue)
                .ToList();
        }

        private static string FindBestPowerLog(string sessionDir)
        {
            string best = null;
            long bestSize = 0;

            foreach (var name in new[] { "Power.log", "Power_old.log" })
            {
                var path = Path.Combine(sessionDir, name);
                if (!File.Exists(path))
                    continue;

                var size = new FileInfo(path).Length;
                if (size > bestSize)
                {
                    bestSize = size;
                    best = path;
                }
            }

            return best;
        }
    }
}
