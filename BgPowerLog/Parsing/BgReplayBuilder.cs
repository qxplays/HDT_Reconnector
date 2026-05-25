using System.Linq;
using BgPowerLog.Models;

namespace BgPowerLog.Parsing
{
    public static class BgReplayBuilder
    {
        public static ReplayParseResult BuildFromDefaultLog(bool tailOnly = true, int tailLines = 20000)
        {
            var path = PowerLogPaths.DefaultPowerLogPath;
            var parser = new PowerLogParser();
            return string.IsNullOrEmpty(path)
                ? new ReplayParseResult { Error = "Логи не найдены. Укажите папку установки Hearthstone." }
                : tailOnly ? parser.ParseTail(path, tailLines) : parser.ParseFile(path);
        }

        public static ReplayParseResult BuildFromPath(string path, bool tailOnly = false, int tailLines = 50000)
        {
            var parser = new PowerLogParser();
            return tailOnly ? parser.ParseTail(path, tailLines) : parser.ParseFile(path);
        }

        /// <summary>Parse Power logs from each session folder for the last N days.</summary>
        public static ReplayParseResult BuildFromRecentSessions(string logsRoot, int daysBack = 14)
        {
            var combined = new ReplayParseResult { SourcePath = logsRoot };
            var sessions = SessionLogCatalog.ListRecentSessions(logsRoot, daysBack);
            if (sessions.Count == 0)
            {
                combined.Error = $"За последние {daysBack} дн. нет папок Hearthstone_… с Power.log в {logsRoot}";
                return combined;
            }

            var parser = new PowerLogParser();
            var matchIndex = 1;

            foreach (var session in sessions)
            {
                var part = parser.ParseFile(session.BestPowerLogPath);
                combined.LinesRead += part.LinesRead;
                combined.GameStateLines += part.GameStateLines;
                if (part.IsBattlegrounds)
                    combined.IsBattlegrounds = true;

                foreach (var m in part.Matches)
                {
                    m.SessionLabel = session.FolderName;
                    m.SourcePath = session.BestPowerLogPath;
                    m.Index = matchIndex++;
                    combined.Matches.Add(m);
                }

                if (part.Matches.Count == 0 && part.Success)
                {
                    combined.Matches.Add(new ReplayMatch
                    {
                        Index = matchIndex++,
                        SessionLabel = session.FolderName,
                        SourcePath = session.BestPowerLogPath,
                        IsBattlegrounds = part.IsBattlegrounds
                    });
                }
            }

            combined.MatchCount = combined.Matches.Count;
            combined.Match = combined.Matches.LastOrDefault(m => m.Turns.Count > 0)
                             ?? combined.Matches.LastOrDefault()
                             ?? new ReplayMatch();
            combined.Success = combined.Matches.Count > 0;

            if (!combined.Success)
                combined.Error = "Сессии найдены, но игры (CREATE_GAME) в логах не распознаны. Попробуйте Parse full file на один файл.";

            return combined;
        }
    }
}
