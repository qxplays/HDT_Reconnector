using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HDT_BgPickAdvisor.Meta;
using HDT_BgPickAdvisor.Models;
using Xunit;
using Xunit.Abstractions;

namespace HDT_BgPickAdvisor.Tests
{
    /// <summary>
    /// Live checks against BgMetaApi (default http://hsbg.qxplays.ru).
    /// Run: dotnet test --filter "Category=Integration"
    /// Override URL: BGMETA_API_URL=http://127.0.0.1:5080
    /// Skip live calls: BGPICKADVISOR_SKIP_LIVE_API=1
    /// </summary>
    [Trait("Category", "Integration")]
    public sealed class MetaApiIntegrationTests
    {
        private readonly ITestOutputHelper _output;

        public MetaApiIntegrationTests(ITestOutputHelper output) => _output = output;

        private static bool SkipLiveApi =>
            string.Equals(Environment.GetEnvironmentVariable("BGPICKADVISOR_SKIP_LIVE_API"), "1",
                StringComparison.OrdinalIgnoreCase);

        [SkippableFact]
        public async Task MetaStore_refresh_loads_heroes_and_trinkets_from_api()
        {
            Skip.If(SkipLiveApi, "BGPICKADVISOR_SKIP_LIVE_API=1");

            var store = new MetaStore();
            await store.RefreshAllAsync();

            _output.WriteLine($"API: {MetaCatalog.ApiBaseUrl}");
            _output.WriteLine($"Heroes: {store.HeroCount}, lesser: {store.TrinketLesserCount}, greater: {store.TrinketGreaterCount}");

            Assert.True(store.HeroCount >= 50, $"Expected >= 50 heroes, got {store.HeroCount}");
            Assert.True(store.TrinketLesserCount >= 20, $"Expected >= 20 lesser trinkets, got {store.TrinketLesserCount}");
            Assert.True(store.TrinketGreaterCount >= 10, $"Expected >= 10 greater trinkets, got {store.TrinketGreaterCount}");

            var sampleHero = store.LookupHero(57946, null);
            Assert.NotNull(sampleHero);
            Assert.False(string.IsNullOrWhiteSpace(sampleHero.Tier));
            Assert.True(sampleHero.Rank > 0);
            Assert.True(sampleHero.AvgPlacement > 0);
        }

        [SkippableFact]
        public void Downloaded_heroes_json_parses_production_shape()
        {
            Skip.If(SkipLiveApi, "BGPICKADVISOR_SKIP_LIVE_API=1");

            var json = DownloadMetaJson(MetaCatalog.HeroesUrl);
            var heroes = MetaJsonParser.ParseHeroes(json);

            _output.WriteLine($"Heroes URL: {MetaCatalog.HeroesUrl}");
            _output.WriteLine($"Parsed: {heroes.Count}, json length: {json.Length}");

            Assert.True(heroes.Count >= 50);
            Assert.True(heroes.All(h => h.DbfId > 0));
            Assert.True(heroes.Count(h => !string.IsNullOrWhiteSpace(h.Tier)) >= heroes.Count * 0.8,
                "Most heroes should have tier_v2 mapped to Tier");
            Assert.Contains(heroes, h => h.Tier == "S" || h.Tier == "A");
            Assert.Equal(1, heroes[0].Rank);
            Assert.True(heroes[0].AvgPlacement > 0);
        }

        [SkippableFact]
        public void Downloaded_trinkets_json_splits_lesser_and_greater()
        {
            Skip.If(SkipLiveApi, "BGPICKADVISOR_SKIP_LIVE_API=1");

            var json = DownloadMetaJson(MetaCatalog.TrinketsUrl);
            MetaJsonParser.ParseTrinketsSplit(json, out var lesser, out var greater);

            _output.WriteLine($"Trinkets URL: {MetaCatalog.TrinketsUrl}");
            _output.WriteLine($"Lesser: {lesser.Count}, greater: {greater.Count}");

            Assert.True(lesser.Count >= 20);
            Assert.True(greater.Count >= 10);
            Assert.All(lesser, t => Assert.Equal(TrinketPool.Lesser, t.Pool));
            Assert.All(greater, t => Assert.Equal(TrinketPool.Greater, t.Pool));
            Assert.All(lesser.Concat(greater), t =>
            {
                Assert.True(t.DbfId > 0);
                Assert.True(t.Rank > 0);
                Assert.False(string.IsNullOrWhiteSpace(t.Tier));
            });
        }

        [SkippableFact]
        public void MetaCatalogLoader_apply_matches_parser_for_live_heroes()
        {
            Skip.If(SkipLiveApi, "BGPICKADVISOR_SKIP_LIVE_API=1");

            var json = DownloadMetaJson(MetaCatalog.HeroesUrl);
            var parsed = MetaJsonParser.ParseHeroes(json);

            var store = new MetaStore();
            Assert.True(MetaCatalogLoader.ApplyHeroesJson(store, json, "integration"));

            Assert.Equal(parsed.Count, store.HeroCount);
            Assert.Equal(parsed[0].DbfId, store.LookupHero(parsed[0].DbfId, null).DbfId);
            Assert.Equal(parsed[0].Tier, store.LookupHero(parsed[0].DbfId, null).Tier);
        }

        private static string DownloadMetaJson(string url)
        {
            using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(45) })
            {
                var body = http.GetStringAsync(url).GetAwaiter().GetResult();
                if (string.IsNullOrWhiteSpace(body))
                    throw new InvalidOperationException($"Empty response from {url}");
                if (body.TrimStart().StartsWith("<", StringComparison.Ordinal))
                    throw new InvalidOperationException($"HTML instead of JSON from {url} (check URL / proxy)");
                return body;
            }
        }
    }
}
