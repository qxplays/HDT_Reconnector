using System;
using System.IO;
using System.Linq;
using HDT_BgPickAdvisor.Meta;
using HDT_BgPickAdvisor.Models;
using Xunit;

namespace HDT_BgPickAdvisor.Tests
{
    public class MetaJsonParserTests
    {
        [Fact]
        public void ParseHeroes_tier_v2_and_avg_final_placement()
        {
            var json = ReadFixture("heroes-external.sample.json");
            var heroes = MetaJsonParser.ParseHeroes(json);

            Assert.True(heroes.Count >= 2);
            Assert.Equal("S", heroes[0].Tier);
            Assert.Equal(57946, heroes[0].DbfId);
            Assert.True(heroes[0].AvgPlacement < 4);
        }

        [Fact]
        public void ParseTrinkets_splits_by_group()
        {
            var json = ReadFixture("trinkets-external.sample.json");
            MetaJsonParser.ParseTrinketsSplit(json, out var lesser, out var greater);

            Assert.True(lesser.Count >= 1);
            Assert.True(greater.Count >= 1);
            Assert.All(lesser, t => Assert.Equal(TrinketPool.Lesser, t.Pool));
            Assert.All(greater, t => Assert.Equal(TrinketPool.Greater, t.Pool));
        }

        [Fact]
        public void ParseHeroes_includes_pick_rate_only_rows()
        {
            var json = @"[{ ""hero_dbf_id"": 74646, ""pick_rate"": 6.19, ""tier_v2"": """" }]";
            var heroes = MetaJsonParser.ParseHeroes(json);

            Assert.Single(heroes);
            Assert.Equal(74646, heroes[0].DbfId);
            Assert.Null(heroes[0].AvgPlacement);
            Assert.Equal(6.19, heroes[0].PickRate);
            Assert.Equal("D", heroes[0].Tier);
        }

        [Fact]
        public void ParseRows_rejects_html_and_invalid()
        {
            Assert.Empty(MetaJsonParser.ParseHeroes("<!DOCTYPE html>"));
            Assert.Empty(MetaJsonParser.ParseHeroes("[\"Invalid member\"]"));
        }

        [Fact]
        public void ParseDesktopDumps_if_present()
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var heroesPath = Path.Combine(desktop, "heroes.json");
            var trinketsPath = Path.Combine(desktop, "trinkets.json");
            if (!File.Exists(heroesPath) || !File.Exists(trinketsPath))
                return;

            var heroes = MetaJsonParser.ParseHeroes(File.ReadAllText(heroesPath));
            MetaJsonParser.ParseTrinketsSplit(File.ReadAllText(trinketsPath), out var lesser, out var greater);

            Assert.True(heroes.Count >= 50);
            Assert.True(lesser.Count >= 20);
            Assert.True(greater.Count >= 10);
            Assert.True(heroes.All(h => !string.IsNullOrWhiteSpace(h.Tier)));
        }

        private static string ReadFixture(string name) =>
            File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", name));
    }
}
