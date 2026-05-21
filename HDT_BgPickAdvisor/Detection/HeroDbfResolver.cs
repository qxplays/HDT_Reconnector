using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HearthDb;
using HDT_BgPickAdvisor.Logging;

namespace HDT_BgPickAdvisor.Detection
{
    /// <summary>
    /// Maps BG hero skin card dbfIds to base hero dbfIds used in meta JSON (hero_dbf_id).
    /// </summary>
    internal static class HeroDbfResolver
    {
        private static readonly Lazy<Dictionary<int, int>> SkinToHeroDbfByCardDbf =
            new Lazy<Dictionary<int, int>>(BuildSkinToHeroDbfMap);

        private static readonly Regex SkinSuffixRegex =
            new Regex(@"_SKIN_[A-Z0-9_]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>Resolve meta lookup dbfId (base hero) from offer entity / card.</summary>
        public static int ResolveMetaDbfId(int cardDbfId, string cardId, object entity = null, int skinParentFromLog = 0)
        {
            if (entity != null && EntityReflection.HasTag(entity, "BACON_SKIN"))
            {
                var parent = EntityReflection.GetGameTag(entity, "BACON_SKIN_PARENT_ID");
                if (parent > 0)
                {
                    if (parent != cardDbfId)
                        FileLogger.Info($"Hero skin resolved via entity tag: cardDbf={cardDbfId} -> metaDbf={parent}");
                    return parent;
                }
            }

            if (skinParentFromLog > 0)
            {
                if (skinParentFromLog != cardDbfId)
                    FileLogger.Info($"Hero skin resolved via Power.log: cardDbf={cardDbfId} -> metaDbf={skinParentFromLog}");
                return skinParentFromLog;
            }

            if (cardDbfId > 0 && SkinToHeroDbfByCardDbf.Value.TryGetValue(cardDbfId, out var mapped))
            {
                FileLogger.Info($"Hero skin resolved via card map: {cardId} dbf={cardDbfId} -> metaDbf={mapped}");
                return mapped;
            }

            return cardDbfId;
        }

        internal static string GetBaseHeroCardId(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
                return cardId;

            return SkinSuffixRegex.Replace(cardId, "");
        }

        private static Dictionary<int, int> BuildSkinToHeroDbfMap()
        {
            var map = new Dictionary<int, int>();
            try
            {
                foreach (var card in Cards.All.Values)
                {
                    if (card.DbfId <= 0 || string.IsNullOrEmpty(card.Id))
                        continue;

                    if (card.Id.IndexOf("_SKIN_", StringComparison.Ordinal) < 0)
                        continue;

                    var baseId = GetBaseHeroCardId(card.Id);
                    if (string.IsNullOrEmpty(baseId) || baseId == card.Id)
                        continue;

                    if (!Cards.All.TryGetValue(baseId, out var baseCard) || baseCard.DbfId <= 0)
                        continue;

                    map[card.DbfId] = baseCard.DbfId;
                }

                FileLogger.Info($"Hero skin map built: {map.Count} skins");
            }
            catch (Exception ex)
            {
                FileLogger.Warn($"Hero skin map build failed: {ex.Message}");
            }

            return map;
        }
    }
}
