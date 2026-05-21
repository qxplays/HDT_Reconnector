using System;
using System.IO;
using System.Text;

namespace HDT_BgPickAdvisor.Logging
{
    internal static class FileLogger
    {
        private static readonly object Gate = new object();
        private static string _logPath;

        private static string PluginDataDirectory =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HearthstoneDeckTracker", "Plugins", "BgPickAdvisor");

        public static string LogPath
        {
            get
            {
                if (_logPath != null)
                    return _logPath;

                Directory.CreateDirectory(PluginDataDirectory);
                _logPath = Path.Combine(PluginDataDirectory, "bgpickadvisor.log");
                return _logPath;
            }
        }

        public static string DebugLastPath => Path.Combine(PluginDataDirectory, "debug-last.json");

        public static void Info(string message) => Write("INFO", message);
        public static void Warn(string message) => Write("WARN", message);
        public static void Error(string message) => Write("ERROR", message);
        public static void Error(string message, Exception ex) =>
            Write("ERROR", message + Environment.NewLine + ex);

        public static void Write(string level, string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
            try
            {
                lock (Gate)
                {
                    Directory.CreateDirectory(PluginDataDirectory);
                    File.AppendAllText(LogPath, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // ignored
            }
        }

        public static void ResetSession()
        {
            try
            {
                lock (Gate)
                {
                    Directory.CreateDirectory(PluginDataDirectory);
                    File.WriteAllText(LogPath,
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [INFO] --- BgPickAdvisor session ---{Environment.NewLine}",
                        Encoding.UTF8);
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}
