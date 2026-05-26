using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HearthDb;
using HDT_BgPickAdvisor.Logging;
using HDT_BgPickAdvisor.Models;
using Core = Hearthstone_Deck_Tracker.API.Core;

namespace HDT_BgPickAdvisor.Detection
{
    internal sealed class BgTrinketOfferDetector
    {
        public bool IsActive => BgPickPhaseHelper.IsTrinketPickPhase();

        public List<TrinketOffer> GetOffers()
        {
            if (!BgPickPhaseHelper.IsTrinketPickPhase())
                return new List<TrinketOffer>();

            return ScanOffersRaw();
        }

        public List<EntityDebugInfo> DumpCandidateEntities()
        {
            var list = new List<EntityDebugInfo>();
            var game = Core.Game;
            var player = game?.Player;
            if (player == null)
                return list;

            // OfferedEntityIds: strongest signal for "currently shown" discover offers
            var offeredIds = Utils.GetPropertyValue(player, "OfferedEntityIds") as IEnumerable;
            var offeredIdSet = new HashSet<int>();
            if (offeredIds != null)
            {
                foreach (var item in offeredIds)
                {
                    if (TryGetEntityId(item, out var entityId))
                        offeredIdSet.Add(entityId);
                }
            }

            var entities = Utils.GetPropertyValue(game, "Entities") as IDictionary;
            if (entities != null && offeredIdSet.Count > 0)
            {
                foreach (var entityId in offeredIdSet)
                {
                    if (!TryResolveEntity(entities, entityId, out var entity))
                        continue;

                    if (!IsTrinketOfferEntity(entity))
                        continue;

                    list.Add(EntityReflection.ToDebugInfo(entity));
                }
            }

            // Fallback: also dump any discover-zone trinkets from PlayerEntities
            if (player.PlayerEntities != null)
            {
                foreach (var entity in player.PlayerEntities)
                {
                    if (!IsTrinketOfferEntity(entity))
                        continue;
                    if (!IsDiscoverZone(entity))
                        continue;
                    list.Add(EntityReflection.ToDebugInfo(entity));
                }
            }

            return list
                .GroupBy(x => x.Id > 0 ? x.Id : x.DbfId)
                .Select(g => g.First())
                .OrderBy(x => x.ZonePosition)
                .ToList();
        }

        internal static int CountVisibleOffers() => ScanOffersRaw().Count;

        internal static List<TrinketOffer> ScanOffersRaw()
        {
            BgTrinketPickTracker.OnGameContextChanged();
            var offeredIdCount = BgTrinketPickTracker.GetOfferedEntityIdCount();

            // No open discover choice → do not scan hand/entities/log (avoids random mid-game overlays).
            if (offeredIdCount < 2)
                return new List<TrinketOffer>();

            var fromChoice = ScanFromPlayerChoice();
            if (fromChoice.Count >= 2)
            {
                LogOffers("OfferedEntityIds", fromChoice, offeredIdCount);
                return fromChoice;
            }

            var fromHand = ScanPlayerHandTrinkets(filterByOfferedIds: true);
            if (fromHand.Count >= 2)
            {
                LogOffers("PlayerEntities+OfferedIds", fromHand, offeredIdCount);
                return fromHand;
            }

            var fromLog = ScanPowerLog();
            if (fromLog.Count >= 2)
            {
                LogOffers("Power.log-active-discover", fromLog, offeredIdCount);
                return fromLog;
            }

            FileLogger.Warn(
                $"Trinket choice open (offeredIds={offeredIdCount}) but resolved 0 offers (choice={fromChoice.Count}, hand={fromHand.Count}, log={fromLog.Count})");
            return new List<TrinketOffer>();
        }

        private static void LogOffers(string source, List<TrinketOffer> offers, int offeredIds)
        {
            BgTrinketPickTracker.LogPhaseSnapshot(true, offers.Count, source, offeredIds);
            FileLogger.Info(
                $"Trinket offers [{source}]: {offers.Count} — {string.Join(", ", offers.Select(o => $"{o.Name}(dbf={o.DbfId})"))}");
        }

        private static List<TrinketOffer> ScanFromPlayerChoice()
        {
            var offers = new List<TrinketOffer>();
            var game = Core.Game;
            var player = game?.Player;
            if (player == null)
                return offers;

            var offeredIds = Utils.GetPropertyValue(player, "OfferedEntityIds") as IEnumerable;
            if (offeredIds == null)
                return offers;

            var offeredIdSet = new HashSet<int>();
            foreach (var item in offeredIds)
            {
                if (TryGetEntityId(item, out var entityId))
                    offeredIdSet.Add(entityId);
            }

            if (offeredIdSet.Count < 2)
                return offers;

            var entities = Utils.GetPropertyValue(game, "Entities") as IDictionary;
            if (entities == null)
                return offers;

            foreach (var entityId in offeredIdSet)
            {
                if (!TryResolveEntity(entities, entityId, out var entity))
                    continue;

                if (!IsTrinketOfferEntity(entity))
                    continue;

                offers.Add(CreateOffer(
                    EntityReflection.GetCardId(entity),
                    EntityReflection.GetDbfId(entity),
                    EntityReflection.GetZonePosition(entity),
                    "OfferedEntityIds"));
            }

            return offers.OrderBy(o => o.ZonePosition).ToList();
        }

        private static List<TrinketOffer> ScanPlayerHandTrinkets(bool filterByOfferedIds)
        {
            var offers = new List<TrinketOffer>();
            var game = Core.Game;
            var player = game?.Player;
            if (player?.PlayerEntities == null)
                return offers;

            HashSet<int> offeredIdSet = null;
            if (filterByOfferedIds)
            {
                offeredIdSet = new HashSet<int>();
                var offeredIds = Utils.GetPropertyValue(player, "OfferedEntityIds") as IEnumerable;
                if (offeredIds != null)
                {
                    foreach (var item in offeredIds)
                    {
                        if (TryGetEntityId(item, out var entityId))
                            offeredIdSet.Add(entityId);
                    }
                }
            }

            foreach (var entity in player.PlayerEntities)
            {
                if (!IsTrinketOfferEntity(entity))
                    continue;

                if (filterByOfferedIds)
                {
                    var id = Utils.GetPropertyValue<int>(entity, "Id");
                    if (offeredIdSet != null && offeredIdSet.Count >= 2 && !offeredIdSet.Contains(id))
                        continue;
                }

                if (!IsDiscoverZone(entity))
                    continue;

                offers.Add(CreateOffer(
                    EntityReflection.GetCardId(entity),
                    EntityReflection.GetDbfId(entity),
                    EntityReflection.GetZonePosition(entity),
                    "PlayerEntities"));
            }

            return DedupeOffers(offers);
        }

        private static List<TrinketOffer> ScanPowerLog() =>
            PowerLogHelper.ReadActiveTrinketDiscoverOffers()
                .Select((t, i) => CreateOffer(t.CardId, t.DbfId, i, "Power.log"))
                .ToList();

        internal static bool IsTrinketOfferEntity(object entity)
        {
            if (entity == null)
                return false;

            if (!EntityReflection.IsBattlegroundsTrinket(entity)
                && !EntityReflection.HasTag(entity, "BACON_TRINKET"))
                return false;

            return !IsEquippedOrBoardTrinket(entity);
        }

        private static bool IsDiscoverZone(object entity)
        {
            var zone = EntityReflection.GetZone(entity);
            return zone == "HAND" || zone == "SETASIDE" || EntityReflection.IsInSetAside(entity);
        }

        private static bool IsEquippedOrBoardTrinket(object entity)
        {
            var zone = EntityReflection.GetZone(entity);
            return zone == "PLAY" || zone == "BOARD" || zone == "HERO";
        }

        private static List<TrinketOffer> DedupeOffers(List<TrinketOffer> offers) =>
            offers
                .GroupBy(o => o.DbfId > 0 ? o.DbfId.ToString() : o.CardId ?? o.Name)
                .Select(g => g.First())
                .OrderBy(o => o.ZonePosition)
                .ToList();

        private static TrinketOffer CreateOffer(string cardId, int dbfId, int zonePosition, string source)
        {
            var name = cardId;
            if (!string.IsNullOrEmpty(cardId) && Cards.All.TryGetValue(cardId, out var card))
                name = card.Name ?? cardId;

            return new TrinketOffer
            {
                CardId = cardId,
                DbfId = dbfId,
                Name = name,
                ZonePosition = zonePosition,
                Pool = GuessPool(cardId),
                Source = source
            };
        }

        private static TrinketPool GuessPool(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
                return TrinketPool.Unknown;

            var upper = cardId.ToUpperInvariant();
            if (upper.Contains("GREATER") || upper.Contains("BGT_") && upper.Contains("24"))
                return TrinketPool.Greater;
            if (upper.Contains("LESSER") || upper.Contains("SMALL"))
                return TrinketPool.Lesser;

            return TrinketPool.Unknown;
        }

        private static bool TryGetEntityId(object item, out int entityId)
        {
            entityId = 0;
            if (item is int i)
            {
                entityId = i;
                return true;
            }

            return int.TryParse(item?.ToString(), out entityId);
        }

        private static bool TryResolveEntity(IDictionary entities, int entityId, out object entity)
        {
            entity = null;
            if (entities.Contains(entityId))
            {
                entity = entities[entityId];
                return entity != null;
            }

            foreach (DictionaryEntry entry in entities)
            {
                if (entry.Key is int key && key == entityId)
                {
                    entity = entry.Value;
                    return entity != null;
                }
            }

            return false;
        }
    }
}
