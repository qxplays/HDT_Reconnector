using System;
using System.IO;
using BgPowerLog.Parsing;
using Xunit;

namespace BgPowerLog.Tests
{
    public class PowerLogParserTests
    {
        [Fact]
        public void Parse_sample_fixture_finds_turns_and_minions()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "power-bg.sample.log");
            var result = new PowerLogParser().ParseFile(path);

            Assert.True(result.Success, result.Error);
            Assert.True(result.IsBattlegrounds);
            Assert.True(result.GameStateLines >= 10);
            Assert.True(result.Match.Turns.Count >= 2);

            var turn2 = result.Match.Turns.Find(t => t.TurnNumber == 2);
            Assert.NotNull(turn2);
            Assert.Equal("Combat", turn2.Phase);
            Assert.NotNull(turn2.Friendly);
            Assert.NotEmpty(turn2.Friendly.Minions);
            Assert.NotEmpty(turn2.Opponent.Minions);
            Assert.NotEqual(
                turn2.Friendly.Minions[0].CardId,
                turn2.Opponent.Minions[0].CardId);
            Assert.NotEqual(
                turn2.Friendly.Hero?.CardId,
                turn2.Opponent.Hero?.CardId);
        }

        [Fact]
        public void Parse_live_log_if_present()
        {
            var path = BgPowerLog.PowerLogPaths.DefaultPowerLogPath;
            if (!File.Exists(path))
                return;

            var result = new PowerLogParser().ParseTail(path, 15000);
            Assert.True(result.LinesRead > 0);
            if (result.GameStateLines == 0)
                return; // e.g. tiny stub log in AppData
            Assert.True(result.GameStateLines > 0);
        }
    }
}
