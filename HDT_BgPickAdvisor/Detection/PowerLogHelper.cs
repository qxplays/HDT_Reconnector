using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HearthDb;
using HearthDb.Enums;

namespace HDT_BgPickAdvisor.Detection
{
    internal static class PowerLogHelper
    {
        private static readonly Regex CardIdRegex = new Regex(@"cardId=([A-Z0-9_]+)", RegexOptions.Compiled);
        private static readonly Regex DbfIdRegex = new Regex(@"dbfId=(\d+)", RegexOptions.Compiled);
        private static readonly Regex BaconDraftedRegex = new Regex(@"BACON_HERO_CAN_BE_DRAFTED", RegexOptions.Compiled);
        private static readonly Regex SkinParentTagRegex = new Regex(
            @"(?:BACON_SKIN_PARENT_ID|tag=2039)[^\d]*(\d+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static string LogPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Blizzard", "Hearthstone", "Logs", "Power.log");

        public static List<HeroPowerLogOffer> ReadRecentHeroOffers(int maxLines = 400, bool freshTail = false)
        {
            var results = new List<HeroPowerLogOffer>();
            if (!File.Exists(LogPath))
                return results;

            try
            {
                var lines = ReadTailLines(LogPath, maxLines);
                foreach (var line in lines)
                {
                    if (!BaconDraftedRegex.IsMatch(line) && !line.Contains("BACON_SKIN"))
                        continue;
                    if (line.Contains("BACON_LOCKED_MULLIGAN_HERO"))
                        continue;

                    var cardMatch = CardIdRegex.Match(line);
                    if (!cardMatch.Success)
                        continue;

                    var cardId = cardMatch.Groups[1].Value;
                    var dbfId = 0;
                    var dbfMatch = DbfIdRegex.Match(line);
                    if (dbfMatch.Success)
                        int.TryParse(dbfMatch.Groups[1].Value, out dbfId);

                    if (!IsLikelyHeroCard(cardId))
                        continue;

                    if (results.Any(r => r.CardId == cardId))
                        continue;

                    var skinParent = 0;
                    var parentMatch = SkinParentTagRegex.Match(line);
                    if (parentMatch.Success)
                        int.TryParse(parentMatch.Groups[1].Value, out skinParent);

                    results.Add(new HeroPowerLogOffer
                    {
                        CardId = cardId,
                        DbfId = dbfId,
                        SkinParentDbfId = skinParent
                    });
                }
            }
            catch
            {
                // ignored
            }

            return results;
        }

        /// <summary>Only the latest open magic-item discover (not older picks in the same log tail).</summary>
        public static List<(string CardId, int DbfId)> ReadActiveTrinketDiscoverOffers(int maxLines = 280)
        {
            var results = new List<(string, int)>();
            if (!File.Exists(LogPath))
                return results;

            try
            {
                var lines = ReadTailLines(LogPath, maxLines).ToList();
                var seenDiscover = false;

                for (var i = lines.Count - 1; i >= 0; i--)
                {
                    var line = lines[i];

                    if (line.Contains("CHOSEN") && seenDiscover && results.Count >= 2)
                        break;

                    if (line.Contains("BACON_IS_MAGIC_ITEM_DISCOVER"))
                    {
                        if (results.Count >= 2)
                            break;

                        seenDiscover = true;
                        continue;
                    }

                    if (!seenDiscover)
                        continue;

                    if (!line.Contains("BACON_TRINKET") && results.Count == 0)
                        continue;

                    var cardMatch = CardIdRegex.Match(line);
                    if (!cardMatch.Success)
                        continue;

                    var cardId = cardMatch.Groups[1].Value;
                    if (!IsLikelyTrinketCard(cardId))
                        continue;

                    var dbfId = 0;
                    var dbfMatch = DbfIdRegex.Match(line);
                    if (dbfMatch.Success)
                        int.TryParse(dbfMatch.Groups[1].Value, out dbfId);

                    if (results.Any(r => r.Item1 == cardId))
                        continue;

                    results.Insert(0, (cardId, dbfId));
                }
            }
            catch
            {
                // ignored
            }

            return results.Count >= 2 ? results : new List<(string, int)>();
        }

        private static bool IsLikelyTrinketCard(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
                return false;

            if (Cards.All.TryGetValue(cardId, out var card))
            {
                if (card.Type == CardType.MINION || card.Type == CardType.HERO)
                    return false;
            }

            var upper = cardId.ToUpperInvariant();
            return upper.Contains("BGT") || upper.Contains("TRINKET") || upper.Contains("BACON");
        }

        private static bool IsLikelyHeroCard(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
                return false;

            if (Cards.All.TryGetValue(cardId, out var card))
                return card.Type == CardType.HERO || card.Id.Contains("HERO");

            return cardId.Contains("HERO") || cardId.StartsWith("BG", StringComparison.Ordinal);
        }

        private static IEnumerable<string> ReadTailLines(string path, int maxLines)
        {
            var queue = new Queue<string>();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    queue.Enqueue(line);
                    if (queue.Count > maxLines)
                        queue.Dequeue();
                }
            }

            return queue.ToArray();
        }
    }

    internal sealed class HeroPowerLogOffer
    {
        public string CardId { get; set; }
        public int DbfId { get; set; }
        public int SkinParentDbfId { get; set; }
    }
}
