using System;
using System.Collections;
using System.Linq;
using HDT_BgPickAdvisor.Logging;
using Core = Hearthstone_Deck_Tracker.API.Core;

namespace HDT_BgPickAdvisor.Detection
{
    /// <summary>Tracks trinket discover sessions and logs phase transitions for debugging.</summary>
    internal static class BgTrinketPickTracker
    {
        private static string _lastLogKey = "";
        private static int _lastMatchToken;

        public static int GetOfferedEntityIdCount()
        {
            var offeredIds = Utils.GetPropertyValue(Core.Game?.Player, "OfferedEntityIds") as IEnumerable;
            return offeredIds?.Cast<object>().Count() ?? 0;
        }

        /// <summary>True when HDT has an open GENERAL choice (any type).</summary>
        public static bool HasOpenChoice => GetOfferedEntityIdCount() >= 2;

        public static void OnGameContextChanged()
        {
            var game = Core.Game;
            if (game == null || !game.IsBattlegroundsMatch || game.IsInMenu)
            {
                _lastMatchToken = 0;
                return;
            }

            var token = game.GetHashCode();
            if (token != _lastMatchToken)
            {
                _lastMatchToken = token;
                _lastLogKey = "";
                FileLogger.Info("Trinket tracker: new BG game context");
            }
        }

        public static void LogPhaseSnapshot(bool phaseActive, int offerCount, string source, int offeredIds)
        {
            var key = $"{phaseActive}|{offerCount}|{source}|{offeredIds}";
            if (key == _lastLogKey)
                return;

            _lastLogKey = key;
            FileLogger.Info(
                $"Trinket phase: active={phaseActive}, offers={offerCount}, offeredEntityIds={offeredIds}, source={source ?? "none"}");
        }
    }
}
