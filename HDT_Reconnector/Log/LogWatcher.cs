using System.Collections.Generic;
using Hearthstone_Deck_Tracker.Utility.Logging;

namespace HDT_Reconnector.GameLog
{
    internal class LogWatcher
    {
        internal const int UpdateDelayMs = 200;

        private readonly ConnectionLogHandler _connectionLogHandler = new ConnectionLogHandler();
        private readonly ReconnectOverlay _overlay;
        private readonly List<LogReader> _logReaders = new List<LogReader>();

        public LogWatcher(ReconnectOverlay overlay, string logPath)
        {
            _overlay = overlay;
            Log.Info("Adding LogReader for file: " + logPath);
            var reader = new LogReader(logPath);
            reader.OnNewLine += OnNewLine;
            _logReaders.Add(reader);
        }

        public void Start()
        {
            foreach (var reader in _logReaders)
                reader.Start();
        }

        public void Stop()
        {
            foreach (var reader in _logReaders)
                reader.Stop();
            _logReaders.Clear();
        }

        private void OnNewLine(string line)
        {
            _connectionLogHandler.Handle(line, _overlay);
        }
    }
}
