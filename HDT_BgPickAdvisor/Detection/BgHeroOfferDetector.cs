using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HearthDb;
using HDT_BgPickAdvisor.Models;
using Core = Hearthstone_Deck_Tracker.API.Core;

namespace HDT_BgPickAdvisor.Detection
{
    internal sealed class BgHeroOfferDetector
    {
        public bool IsActive => BgPickPhaseHelper.IsHeroPickPhase();

        public List<HeroOffer> GetOffers()
        {
            var fromState = ScanHeroPickState();
            if (fromState.Count >= 2)
                return fromState;

            var fromEntities = ScanPlayerEntities();
            if (fromEntities.Count >= 2)
            {
                SnapshotOffers(fromEntities);
                return fromEntities;
            }

            return ScanPowerLog();
        }

        public List<EntityDebugInfo> DumpPlayerHeroEntities()
        {
            return BgPickPhaseHelper.GetDraftableHeroEntities()
                .Select(EntityReflection.ToDebugInfo)
                .ToList();
        }

        private static void SnapshotOffers(List<HeroOffer> offers)
        {
            var game = Core.Game;
            if (game == null)
                return;

            var heroes = BgPickPhaseHelper.GetDraftableHeroEntities();
            if (heroes.Count < 2)
                return;

            Utils.TryInvoke(game, "SnapshotBattlegroundsOfferedHeroes", new object[] { heroes }, out _);
        }

        private static List<HeroOffer> ScanPlayerEntities()
        {
            return BgPickPhaseHelper.GetDraftableHeroEntities()
                .Select(e => CreateOffer(
                    EntityReflection.GetCardId(e),
                    EntityReflection.GetDbfId(e),
                    EntityReflection.GetZonePosition(e),
                    "entities",
                    e))
                .OrderBy(o => o.ZonePosition)
                .ToList();
        }

        private static List<HeroOffer> ScanHeroPickState()
        {
            var offers = new List<HeroOffer>();
            var game = Core.Game;
            if (game == null)
                return offers;

            var state = Utils.GetPropertyValue(game, "BattlegroundsHeroPickState");
            if (state == null)
                return offers;

            var dbfIds = Utils.GetPropertyValue(state, "OfferedHeroDbfIds") as IEnumerable;
            if (dbfIds == null)
                return offers;

            foreach (var item in dbfIds)
            {
                int dbfId;
                if (item is int i)
                    dbfId = i;
                else if (!int.TryParse(item?.ToString(), out dbfId))
                    continue;

                offers.Add(CreateOffer(ResolveCardId(dbfId), dbfId, offers.Count, "BattlegroundsHeroPickState", null));
            }

            return offers;
        }

        private static List<HeroOffer> ScanPowerLog()
        {
            return PowerLogHelper.ReadRecentHeroOffers(freshTail: true)
                .Select((h, i) => CreateOffer(h.CardId, h.DbfId, i, "Power.log", null, h.SkinParentDbfId))
                .ToList();
        }

        private static HeroOffer CreateOffer(
            string cardId,
            int cardDbfId,
            int zonePosition,
            string source,
            object entity = null,
            int skinParentFromLog = 0)
        {
            var name = cardId;
            if (!string.IsNullOrEmpty(cardId) && Cards.All.TryGetValue(cardId, out var card))
                name = card.Name ?? cardId;
            else if (cardDbfId > 0)
            {
                var byDbf = Cards.All.Values.FirstOrDefault(c => c.DbfId == cardDbfId);
                if (byDbf != null)
                {
                    name = byDbf.Name ?? byDbf.Id;
                    if (string.IsNullOrEmpty(cardId))
                        cardId = byDbf.Id;
                }
            }

            var metaDbfId = HeroDbfResolver.ResolveMetaDbfId(cardDbfId, cardId, entity, skinParentFromLog);

            return new HeroOffer
            {
                CardId = cardId,
                DbfId = metaDbfId,
                Name = name,
                ZonePosition = zonePosition,
                Source = source
            };
        }

        private static string ResolveCardId(int dbfId)
        {
            if (dbfId <= 0)
                return null;
            return Cards.All.Values.FirstOrDefault(c => c.DbfId == dbfId)?.Id;
        }
    }
}
