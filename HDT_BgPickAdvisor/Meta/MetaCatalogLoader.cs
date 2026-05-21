using System;
using System.Net.Http;
using HDT_BgPickAdvisor.Logging;
using HDT_BgPickAdvisor.Models;

namespace HDT_BgPickAdvisor.Meta
{
    internal sealed class MetaCatalogLoadResult
    {
        public bool HeroesLoaded { get; set; }
        public bool TrinketsLoaded { get; set; }
    }

    /// <summary>Loads meta from BgMetaApi only.</summary>
    internal static class MetaCatalogLoader
    {
        public static MetaCatalogLoadResult TryRefresh(MetaStore store, HttpClient http, bool force)
        {
            var result = new MetaCatalogLoadResult();

            if (TryLoadHeroes(store, http, force))
                result.HeroesLoaded = true;

            if (TryLoadTrinkets(store, http, force))
                result.TrinketsLoaded = true;

            return result;
        }

        private static bool TryLoadHeroes(MetaStore store, HttpClient http, bool force)
        {
            if (!force && store.HeroCount > 0)
                return true;

            if (!MetaCatalog.IsRemoteConfigured)
            {
                FileLogger.Warn("Meta API URL not configured");
                return false;
            }

            var json = DownloadJson(http, MetaCatalog.HeroesUrl, "heroes");
            return ApplyHeroesJson(store, json, "api");
        }

        private static bool TryLoadTrinkets(MetaStore store, HttpClient http, bool force)
        {
            if (!force && store.TrinketLesserCount > 0)
                return true;

            if (!MetaCatalog.IsRemoteConfigured)
            {
                FileLogger.Warn("Meta API URL not configured");
                return false;
            }

            var json = DownloadJson(http, MetaCatalog.TrinketsUrl, "trinkets");
            return ApplyTrinketsJson(store, json, "api");
        }

        internal static bool ApplyHeroesJson(MetaStore store, string json, string source)
        {
            if (string.IsNullOrEmpty(json))
                return false;

            var heroes = MetaJsonParser.ParseHeroes(json);
            if (heroes.Count == 0)
            {
                FileLogger.Warn($"Heroes {source}: 0 rows");
                return false;
            }

            store.ApplyHeroesInMemory(heroes);
            FileLogger.Info($"Heroes from {source}: {heroes.Count}");
            return true;
        }

        internal static bool ApplyTrinketsJson(MetaStore store, string json, string source)
        {
            if (string.IsNullOrEmpty(json))
                return false;

            MetaJsonParser.ParseTrinketsSplit(json, out var lesser, out var greater);
            if (lesser.Count == 0 && greater.Count == 0)
            {
                FileLogger.Warn($"Trinkets {source}: 0 rows");
                return false;
            }

            if (lesser.Count > 0)
                store.ApplyTrinketsInMemory(lesser, TrinketPool.Lesser);
            if (greater.Count > 0)
                store.ApplyTrinketsInMemory(greater, TrinketPool.Greater);

            FileLogger.Info($"Trinkets from {source}: lesser={lesser.Count}, greater={greater.Count}");
            return lesser.Count > 0 || greater.Count > 0;
        }

        private static string DownloadJson(HttpClient http, string url, string label)
        {
            try
            {
                FileLogger.Info($"GET {label}: {url}");
                var body = http.GetStringAsync(url).GetAwaiter().GetResult();
                if (string.IsNullOrWhiteSpace(body) || body.TrimStart().StartsWith("<", StringComparison.Ordinal))
                {
                    FileLogger.Warn($"{label}: not JSON (check API URL / TLS)");
                    return null;
                }

                FileLogger.Info($"{label}: {body.Length} chars");
                return body;
            }
            catch (Exception ex)
            {
                FileLogger.Warn($"{label} download failed: {ex.Message}");
                return null;
            }
        }
    }
}
