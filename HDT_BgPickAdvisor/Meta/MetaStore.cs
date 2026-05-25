using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HearthDb;
using HDT_BgPickAdvisor.Detection;
using HDT_BgPickAdvisor.Logging;
using HDT_BgPickAdvisor.Models;

namespace HDT_BgPickAdvisor.Meta
{
    /// <summary>In-memory meta loaded from plugin JSON files or BgMetaApi.</summary>
    internal sealed class MetaStore
    {
        private readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private Dictionary<int, HeroMeta> _heroesByDbf = new Dictionary<int, HeroMeta>();
        private Dictionary<int, TrinketMeta> _trinketsLesserByDbf = new Dictionary<int, TrinketMeta>();
        private Dictionary<int, TrinketMeta> _trinketsGreaterByDbf = new Dictionary<int, TrinketMeta>();
        private bool _loaded;

        public string LastLoadError { get; private set; }
        public int HeroCount => _heroesByDbf.Count;
        public int TrinketLesserCount => _trinketsLesserByDbf.Count;
        public int TrinketGreaterCount => _trinketsGreaterByDbf.Count;

        public void EnsureLoaded(bool forceRefresh = false)
        {
            if (forceRefresh || HeroCount == 0 || TrinketLesserCount == 0)
                MetaCatalogLoader.TryRefresh(this, _http, forceRefresh || HeroCount == 0);

            _loaded = true;
            FileLogger.Info($"Meta loaded: heroes={HeroCount}, lesser={TrinketLesserCount}, greater={TrinketGreaterCount}, api={MetaCatalog.ApiBaseUrl}");
        }

        public async Task RefreshAllAsync()
        {
            await Task.Run(() => MetaCatalogLoader.TryRefresh(this, _http, force: true));
            _loaded = true;
        }

        internal void ApplyHeroesInMemory(List<HeroMeta> heroes)
        {
            _heroesByDbf = heroes.Where(h => h.DbfId > 0).GroupBy(h => h.DbfId).ToDictionary(g => g.Key, g => g.First());
            LastLoadError = null;
        }

        internal void ApplyTrinketsInMemory(List<TrinketMeta> trinkets, TrinketPool pool)
        {
            var dict = trinkets.Where(t => t.DbfId > 0).GroupBy(t => t.DbfId).ToDictionary(g => g.Key, g => g.First());
            if (pool == TrinketPool.Greater)
                _trinketsGreaterByDbf = dict;
            else
                _trinketsLesserByDbf = dict;
            LastLoadError = null;
        }

        public HeroMeta LookupHero(int dbfId, string cardId)
        {
            if (!_loaded)
                EnsureLoaded();

            if (dbfId > 0 && _heroesByDbf.TryGetValue(dbfId, out var byDbf))
                return byDbf;

            var resolved = HeroDbfResolver.ResolveMetaDbfId(dbfId, cardId);
            if (resolved > 0 && resolved != dbfId && _heroesByDbf.TryGetValue(resolved, out byDbf))
                return byDbf;

            if (!string.IsNullOrEmpty(cardId))
            {
                var baseId = HeroDbfResolver.GetBaseHeroCardId(cardId);
                if (!string.IsNullOrEmpty(baseId) && !string.Equals(baseId, cardId, StringComparison.Ordinal))
                {
                    try
                    {
                        if (Cards.All.TryGetValue(baseId, out var baseCard) && baseCard.DbfId > 0 &&
                            _heroesByDbf.TryGetValue(baseCard.DbfId, out byDbf))
                            return byDbf;
                    }
                    catch
                    {
                        // HearthDb not available
                    }
                }
            }

            return null;
        }

        public TrinketMeta LookupTrinket(int dbfId, string cardId, TrinketPool pool)
        {
            if (!_loaded)
                EnsureLoaded();

            var dict = pool == TrinketPool.Greater ? _trinketsGreaterByDbf : _trinketsLesserByDbf;
            if (pool == TrinketPool.Unknown)
                return LookupIn(dict, dbfId) ?? LookupIn(_trinketsGreaterByDbf, dbfId) ?? LookupIn(_trinketsLesserByDbf, dbfId);

            return LookupIn(dict, dbfId);
        }

        public void RankHeroOffers(IList<HeroOffer> offers)
        {
            if (!_loaded)
                EnsureLoaded();

            foreach (var offer in offers)
            {
                offer.Meta = LookupHero(offer.DbfId, offer.CardId);
                offer.IsBestPick = false;
            }

            var best = offers
                .OrderBy(o => o.Meta == null ? 1 : 0)
                .ThenBy(o => o.Meta?.Rank ?? int.MaxValue)
                .ThenBy(o => o.Meta?.AvgPlacement ?? double.MaxValue)
                .ThenByDescending(o => o.Meta?.PickRate ?? 0)
                .FirstOrDefault();
            if (best != null)
                best.IsBestPick = true;
        }

        public void RankTrinketOffers(IList<TrinketOffer> offers)
        {
            if (!_loaded)
                EnsureLoaded();

            foreach (var offer in offers)
            {
                offer.Meta = LookupTrinket(offer.DbfId, offer.CardId, offer.Pool);
                if (offer.Meta == null && offer.Pool != TrinketPool.Lesser)
                    offer.Meta = LookupTrinket(offer.DbfId, offer.CardId, TrinketPool.Lesser);
                if (offer.Meta == null && offer.Pool != TrinketPool.Greater)
                    offer.Meta = LookupTrinket(offer.DbfId, offer.CardId, TrinketPool.Greater);

                if (offer.Meta?.Pool != TrinketPool.Unknown)
                    offer.Pool = offer.Meta.Pool;
                offer.IsBestPick = false;
            }

            var best = offers
                .OrderBy(o => o.Meta?.Rank ?? int.MaxValue)
                .ThenBy(o => o.Meta?.AvgPlacement ?? double.MaxValue)
                .FirstOrDefault();
            if (best != null)
                best.IsBestPick = true;
        }

        private static TrinketMeta LookupIn(Dictionary<int, TrinketMeta> dict, int dbfId)
        {
            if (dbfId > 0 && dict.TryGetValue(dbfId, out var meta))
                return meta;
            return null;
        }
    }
}
