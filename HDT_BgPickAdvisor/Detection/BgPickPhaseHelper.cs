using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core = Hearthstone_Deck_Tracker.API.Core;

namespace HDT_BgPickAdvisor.Detection
{
    internal static class BgPickPhaseHelper
    {
        /// <summary>Offered BG heroes live in SETASIDE during pick (see HDT GameV2.BattlegroundsHeroCount).</summary>
        public static List<object> GetDraftableHeroEntities()
        {
            var game = Core.Game;
            if (game?.Player?.PlayerEntities == null)
                return new List<object>();

            return game.Player.PlayerEntities
                .Where(e => EntityReflection.IsHero(e))
                .Where(e => EntityReflection.IsInSetAside(e))
                .Where(e => EntityReflection.HasTag(e, "BACON_HERO_CAN_BE_DRAFTED") ||
                            EntityReflection.HasTag(e, "BACON_SKIN"))
                .Where(e => !EntityReflection.HasTag(e, "BACON_LOCKED_MULLIGAN_HERO"))
                .Cast<object>()
                .ToList();
        }

        /// <summary>Hero pick while draftable heroes are on screen (includes premium reroll).</summary>
        public static bool IsHeroPickPhase()
        {
            var game = Core.Game;
            if (game == null || !game.IsBattlegroundsMatch || game.IsInMenu)
                return false;

            if (IsBattlegroundsHeroPickingDone(game))
                return false;

            if (GetOfferedHeroDbfIdCount(game) >= 2)
                return true;

            return GetDraftableHeroEntities().Count >= 2;
        }

        /// <summary>BG accessory (trinket) discover — turn varies by hero; detect offers, not turn.</summary>
        public static bool IsTrinketPickPhase()
        {
            var game = Core.Game;
            if (game == null)
                return false;

            BgTrinketPickTracker.OnGameContextChanged();

            var offeredIds = BgTrinketPickTracker.GetOfferedEntityIdCount();
            var offerCount = offeredIds >= 2 ? BgTrinketOfferDetector.CountVisibleOffers() : 0;
            var active = BgPickPhaseRules.ShouldShowTrinketOverlay(
                game.IsBattlegroundsMatch,
                game.IsInMenu,
                IsHeroPickPhase(),
                IsBattlegroundsHeroPickingDone(game),
                offerCount);

            if (!active && offeredIds < 2)
                BgTrinketPickTracker.LogPhaseSnapshot(false, 0, null, offeredIds);
            else if (!active && offeredIds >= 2)
                BgTrinketPickTracker.LogPhaseSnapshot(false, offerCount, "non-trinket-choice", offeredIds);

            return active;
        }

        /// <summary>Same signal HDT uses to close the hero pick overlay (player mulligan finished).</summary>
        public static bool IsBattlegroundsHeroPickingDone(object game)
        {
            if (game == null)
                return true;

            if (!Utils.GetPropertyValue<bool>(game, "IsBattlegroundsMatch"))
                return true;

            var prop = game.GetType().GetProperty("IsBattlegroundsHeroPickingDone");
            if (prop != null && prop.PropertyType == typeof(bool))
                return Utils.GetPropertyValue<bool>(game, "IsBattlegroundsHeroPickingDone");

            return IsPlayerMulliganDone(game);
        }

        private static bool IsPlayerMulliganDone(object game)
        {
            var player = Utils.GetPropertyValue(game, "Player");
            var hero = Utils.GetPropertyValue(player, "Hero");
            if (hero == null)
                return false;

            var doneValue = GetMulliganDoneValue();
            if (doneValue < 0)
                return false;

            return EntityReflection.GetGameTag(hero, "MULLIGAN_STATE") == doneValue;
        }

        private static int GetMulliganDoneValue()
        {
            try
            {
                var mulliganType = Type.GetType(
                    "Hearthstone_Deck_Tracker.Hearthstone.Mulligan, HearthstoneDeckTracker");
                if (mulliganType == null)
                    return -1;

                return Convert.ToInt32(Enum.Parse(mulliganType, "DONE"));
            }
            catch
            {
                return -1;
            }
        }

        private static int GetOfferedHeroDbfIdCount(object game)
        {
            var state = Utils.GetPropertyValue(game, "BattlegroundsHeroPickState");
            if (state == null)
                return 0;

            var dbfIds = Utils.GetPropertyValue(state, "OfferedHeroDbfIds") as IEnumerable;
            return dbfIds?.Cast<object>().Count() ?? 0;
        }
    }
}
