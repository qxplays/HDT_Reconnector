using System;
using System.Text.RegularExpressions;

namespace HDT_Reconnector.GameLog
{
    internal class ConnectionLogHandler
    {
        private static readonly Regex GotoGameServer = new Regex(
            @"Network\.GotoGameServer.*address=[ ]*(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}):(\d{1,5})",
            RegexOptions.Compiled);

        public void Handle(string line, ReconnectOverlay overlay)
        {
            var match = GotoGameServer.Match(line);
            if (!match.Success)
                return;

            lock (overlay)
            {
                overlay.RemoteAddr = match.Groups[1].Value.Trim();
                overlay.RemotePort = ushort.Parse(match.Groups[2].Value);
            }
        }
    }
}
