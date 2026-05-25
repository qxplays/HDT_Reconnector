using System;
using System.IO;

namespace BgPowerLog
{
    /// <summary>Persisted custom HS logs root (e.g. Program Files install Logs).</summary>
    public static class ReplayLogSettings
    {
        public const string DefaultInstallLogsRoot = @"C:\Program Files (x86)\Hearthstone\Logs";

        private static string SettingsDir =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HearthstoneDeckTracker", "Plugins", "BgReplay");

        private static string LogRootFilePath => Path.Combine(SettingsDir, "log-root.txt");
        private static string InstallFilePath => Path.Combine(SettingsDir, "install-path.txt");

        public static string CustomLogRoot
        {
            get
            {
                try
                {
                    if (File.Exists(LogRootFilePath))
                    {
                        var line = File.ReadAllText(LogRootFilePath).Trim();
                        if (!string.IsNullOrWhiteSpace(line))
                            return line;
                    }

                    var install = InstallPath;
                    if (!string.IsNullOrWhiteSpace(install))
                    {
                        var logs = Path.Combine(install.TrimEnd('\\', '/'), "Logs");
                        if (Directory.Exists(logs))
                            return logs;
                    }
                }
                catch
                {
                    // ignored
                }

                return Directory.Exists(DefaultInstallLogsRoot) ? DefaultInstallLogsRoot : null;
            }
            set => WriteFile(LogRootFilePath, value);
        }

        public static string InstallPath
        {
            get => ReadFile(InstallFilePath);
            set
            {
                WriteFile(InstallFilePath, value);
                if (!string.IsNullOrWhiteSpace(value) &&
                    HearthstoneInstallLocator.TryResolveLogsDirectory(value, out var logs, out _))
                    CustomLogRoot = logs;
            }
        }

        private static string ReadFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    return File.ReadAllText(path).Trim();
            }
            catch
            {
                // ignored
            }

            return null;
        }

        private static void WriteFile(string path, string value)
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                if (string.IsNullOrWhiteSpace(value))
                {
                    if (File.Exists(path))
                        File.Delete(path);
                }
                else
                    File.WriteAllText(path, value.Trim());
            }
            catch
            {
                // ignored
            }
        }
    }
}
