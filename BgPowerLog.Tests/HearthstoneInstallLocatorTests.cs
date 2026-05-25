using System.IO;
using BgPowerLog;
using Xunit;

namespace BgPowerLog.Tests
{
    public class HearthstoneInstallLocatorTests
    {
        [Fact]
        public void Resolve_logs_from_default_install_path()
        {
            var install = @"C:\Program Files (x86)\Hearthstone";
            if (!Directory.Exists(install))
                return;

            Assert.True(
                HearthstoneInstallLocator.TryResolveLogsDirectory(install, out var logs, out var msg));
            Assert.EndsWith("Logs", logs);
            Assert.True(Directory.Exists(logs), msg);
        }

        [Fact]
        public void List_recent_sessions_when_logs_exist()
        {
            var root = ReplayLogSettings.CustomLogRoot ?? ReplayLogSettings.DefaultInstallLogsRoot;
            if (!Directory.Exists(root))
                return;

            var sessions = SessionLogCatalog.ListRecentSessions(root, 30);
            Assert.NotNull(sessions);
        }
    }
}
