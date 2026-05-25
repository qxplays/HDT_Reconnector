using System;
using System.IO;
using System.Linq;
using BgPowerLog.Parsing;
using Xunit;

namespace BgPowerLog.Tests
{
    public class RealLogBoardSplitTests
    {
        [Fact]
        public void Combat_turn_friendly_and_opponent_boards_differ_on_real_log()
        {
            var path = Environment.GetEnvironmentVariable("HS_POWER_LOG");
            if (string.IsNullOrEmpty(path))
            {
                var dir = Path.Combine(
                    @"C:\Program Files (x86)\Hearthstone\Logs",
                    "Hearthstone_2026_05_21_22_54_22");
                path = Path.Combine(dir, "Power_old.log");
            }

            if (!File.Exists(path))
                return;

            var result = new PowerLogParser().ParseFile(path, maxBytes: 64 * 1024 * 1024);
            Assert.True(result.Matches.Count > 0, result.Error);

            var match = result.Matches.OrderByDescending(m => m.Turns.Count).First();
            var combat = match.Turns
                .Where(t => t.Phase == "Combat" && t.TurnNumber >= 4)
                .OrderByDescending(t => t.TurnNumber)
                .FirstOrDefault(t =>
                    t.Friendly.Minions.Count > 0 && t.Opponent.Minions.Count > 0);

            Assert.NotNull(combat);

            var friendlyIds = combat.Friendly.Minions.Select(m => m.CardId).ToList();
            var opponentIds = combat.Opponent.Minions.Select(m => m.CardId).ToList();

            Assert.All(friendlyIds, id => Assert.StartsWith("BG", id, StringComparison.OrdinalIgnoreCase));
            Assert.All(opponentIds, id => Assert.StartsWith("BG", id, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(opponentIds, id => id.StartsWith("TB_Bacon", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(friendlyIds, id => id.StartsWith("TB_Bacon", StringComparison.OrdinalIgnoreCase));

            Assert.NotEqual(friendlyIds.OrderBy(x => x), opponentIds.OrderBy(x => x));

            var recruit = match.Turns
                .Where(t => t.Phase == "Recruit" && t.TurnNumber == combat.TurnNumber - 1)
                .FirstOrDefault();
            if (recruit != null)
            {
                Assert.True(recruit.Friendly.Minions.Count > 0);
                Assert.Empty(recruit.Opponent.Minions);
            }
        }
    }
}
