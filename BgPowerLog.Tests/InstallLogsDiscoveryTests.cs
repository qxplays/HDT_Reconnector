using System;
using System.IO;
using System.Linq;
using BgPowerLog;
using BgPowerLog.Models;
using BgPowerLog.Parsing;
using Xunit;

namespace BgPowerLog.Tests
{
    public class InstallLogsDiscoveryTests
    {
        [Fact]
        public void Discover_install_logs_folder_finds_power_old_in_sessions()
        {
            var root = Environment.GetEnvironmentVariable("HS_LOG_ROOT")
                       ?? ReplayLogSettings.DefaultInstallLogsRoot;
            if (!Directory.Exists(root))
                return;

            var logs = PowerLogPaths.DiscoverLogFiles(root);
            Assert.True(logs.Count > 0);
            Assert.Contains(logs, x => x.Name.IndexOf("Power", StringComparison.OrdinalIgnoreCase) >= 0);

            var powerLogs = logs.Where(x => x.Name.StartsWith("Power", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.SizeBytes)
                .ToList();
            Assert.True(powerLogs.Count > 0);

            ReplayParseResult parsed = null;
            foreach (var log in powerLogs)
            {
                parsed = new PowerLogParser().ParseTail(log.Path, 15000);
                if (parsed.GameStateLines > 0)
                    break;
            }

            Assert.NotNull(parsed);
            Assert.True(parsed.GameStateLines > 0, "No GameState lines in any Power*.log tail");
        }
    }
}
