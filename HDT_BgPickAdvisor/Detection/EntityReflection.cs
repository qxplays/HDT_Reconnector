using System;
using System.Collections;
using System.Collections.Generic;
using HDT_BgPickAdvisor.Models;

namespace HDT_BgPickAdvisor.Detection
{
    internal static class EntityReflection
    {
        public static bool IsHero(object entity) => Utils.GetPropertyValue<bool>(entity, "IsHero");

        public static bool IsInSetAside(object entity) =>
            Utils.GetPropertyValue<bool>(entity, "IsInSetAside") ||
            string.Equals(GetZone(entity), "SETASIDE", StringComparison.OrdinalIgnoreCase);

        public static bool IsBattlegroundsTrinket(object entity) =>
            Utils.GetPropertyValue<bool>(entity, "IsBattlegroundsTrinket");

        public static bool HasTag(object entity, string tagName)
        {
            if (entity == null)
                return false;

            try
            {
                var tagType = Type.GetType("Hearthstone_Deck_Tracker.Hearthstone.GAME_TAG, HearthstoneDeckTracker");
                if (tagType == null)
                    return HasTagInDictionary(entity, tagName);

                object enumValue;
                try
                {
                    enumValue = Enum.Parse(tagType, tagName, true);
                }
                catch
                {
                    return HasTagInDictionary(entity, tagName);
                }

                var method = entity.GetType().GetMethod("HasTag", new[] { tagType });
                if (method == null)
                    return HasTagInDictionary(entity, tagName);

                return (bool)method.Invoke(entity, new[] { enumValue });
            }
            catch
            {
                return HasTagInDictionary(entity, tagName);
            }
        }

        public static int GetGameTag(object entity, string tagName)
        {
            if (entity == null)
                return 0;

            try
            {
                var tagType = Type.GetType("Hearthstone_Deck_Tracker.Hearthstone.GAME_TAG, HearthstoneDeckTracker");
                if (tagType == null)
                    return 0;

                var enumValue = Enum.Parse(tagType, tagName, true);
                var method = entity.GetType().GetMethod("GetTag", new[] { tagType });
                if (method == null)
                    return 0;

                return Convert.ToInt32(method.Invoke(entity, new[] { enumValue }));
            }
            catch
            {
                return 0;
            }
        }

        public static int GetZonePosition(object entity) => Utils.GetPropertyValue<int>(entity, "ZonePosition");

        public static string GetZone(object entity)
        {
            var zone = Utils.GetPropertyValue(entity, "Zone");
            return zone?.ToString();
        }

        public static string GetCardId(object entity) => Utils.GetPropertyValue<string>(entity, "CardId");

        public static int GetDbfId(object entity)
        {
            var card = Utils.GetPropertyValue(entity, "Card");
            if (card != null)
            {
                var dbf = Utils.GetPropertyValue<int>(card, "DbfId");
                if (dbf > 0)
                    return dbf;
            }

            return Utils.GetPropertyValue<int>(entity, "DbfId");
        }

        public static EntityDebugInfo ToDebugInfo(object entity)
        {
            var info = new EntityDebugInfo
            {
                Id = Utils.GetPropertyValue<int>(entity, "Id"),
                CardId = GetCardId(entity),
                DbfId = GetDbfId(entity),
                Name = Utils.GetPropertyValue<string>(entity, "Name"),
                ZonePosition = GetZonePosition(entity),
                IsHero = IsHero(entity)
            };

            var tags = Utils.GetPropertyValue(entity, "Tags") as IDictionary;
            if (tags != null)
            {
                foreach (DictionaryEntry entry in tags)
                    info.Tags[entry.Key?.ToString() ?? "?"] = Convert.ToInt32(entry.Value);
            }

            foreach (var tagName in new[]
                     {
                         "BACON_HERO_CAN_BE_DRAFTED", "BACON_SKIN", "BACON_LOCKED_MULLIGAN_HERO",
                         "BACON_TRINKET", "BACON_IS_MAGIC_ITEM_DISCOVER"
                     })
            {
                if (HasTag(entity, tagName))
                    info.Tags[tagName] = 1;
            }

            return info;
        }

        private static bool HasTagInDictionary(object entity, string tagName)
        {
            var tags = Utils.GetPropertyValue(entity, "Tags") as IDictionary;
            if (tags == null)
                return false;

            foreach (var key in tags.Keys)
            {
                if (string.Equals(key?.ToString(), tagName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
