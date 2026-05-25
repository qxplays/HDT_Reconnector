using System;
using System.IO;
using System.Linq;

namespace BgPowerLog
{
    public static class HearthstoneInstallLocator
    {
        public static string TryAutoDetectInstallDirectory()
        {
            foreach (var candidate in new[]
                     {
                         @"C:\Program Files (x86)\Hearthstone",
                         @"C:\Program Files\Hearthstone",
                         Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Hearthstone"),
                         Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Hearthstone")
                     })
            {
                if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "Hearthstone.exe")))
                    return candidate;
            }

            return null;
        }

        /// <summary>
        /// User may pick install root (with Hearthstone.exe), Logs folder, or a session subfolder.
        /// </summary>
        public static bool TryResolveLogsDirectory(string selectedPath, out string logsDirectory, out string message)
        {
            logsDirectory = null;
            message = null;

            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                message = "Путь не выбран.";
                return false;
            }

            selectedPath = selectedPath.Trim().TrimEnd('\\', '/');

            if (!Directory.Exists(selectedPath))
            {
                message = "Папка не существует: " + selectedPath;
                return false;
            }

            var name = Path.GetFileName(selectedPath);
            if (name != null && name.StartsWith("Hearthstone_20", StringComparison.OrdinalIgnoreCase))
            {
                var parent = Path.GetDirectoryName(selectedPath);
                if (parent != null && Directory.Exists(parent))
                {
                    logsDirectory = parent;
                    message = "Выбрана сессия — используем папку Logs: " + logsDirectory;
                    return true;
                }
            }

            if (File.Exists(Path.Combine(selectedPath, "Hearthstone.exe")))
            {
                logsDirectory = Path.Combine(selectedPath, "Logs");
                message = "Найден Hearthstone.exe → " + logsDirectory;
                return Directory.Exists(logsDirectory);
            }

            if (string.Equals(name, "Logs", StringComparison.OrdinalIgnoreCase))
            {
                logsDirectory = selectedPath;
                message = "Папка Logs принята.";
                return true;
            }

            var logsUnder = Path.Combine(selectedPath, "Logs");
            if (Directory.Exists(logsUnder))
            {
                logsDirectory = logsUnder;
                message = "Найдена подпапка Logs.";
                return true;
            }

            if (PowerLogPaths.DiscoverLogFiles(selectedPath).Count > 0)
            {
                logsDirectory = selectedPath;
                message = "В папке есть файлы логов.";
                return true;
            }

            message = "Не найдены Hearthstone.exe и папка Logs. Выберите папку установки (где лежит Hearthstone.exe).";
            return false;
        }
    }
}
